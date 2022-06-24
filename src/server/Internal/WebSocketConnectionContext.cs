using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Security.Claims;

namespace SimpleR.Internal;

internal partial class WebSocketConnectionContext : ConnectionContext,
    IConnectionIdFeature,
    IConnectionItemsFeature,
    IConnectionTransportFeature,
    IConnectionUserFeature,
    ITransferFormatFeature,
    IHttpTransportFeature,
    IConnectionLifetimeFeature
{
    private readonly ILogger _logger;
    private readonly object _itemsLock = new();
    private readonly object _stateLock = new();
    private bool _disposed;
    private IDictionary<object, object?>? _items;
    private readonly TaskCompletionSource _disposeTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _connectionClosedTokenSource;

    internal WebSocketConnectionContext(string id, ILogger logger, IDuplexPipe transport, IDuplexPipe application, WebSocketConnectionDispatcherOptions options)
    {
        ConnectionId = id;
        Application = application;
        Transport = transport;
        Options = options;

        // The default behavior is that both formats are supported.
        SupportedFormats = TransferFormat.Text | TransferFormat.Binary;
        ActiveFormat = options.WebSockets.TransferFormat == Protocol.TransferFormat.Binary? TransferFormat.Binary : TransferFormat.Text;

        _connectionClosedTokenSource = new CancellationTokenSource();
        ConnectionClosed = _connectionClosedTokenSource.Token;

        _logger = logger;
        
        Features = new FeatureCollection();
        Features.Set<IConnectionUserFeature>(this);
        Features.Set<IConnectionItemsFeature>(this);
        Features.Set<IConnectionIdFeature>(this);
        Features.Set<IConnectionTransportFeature>(this);
        Features.Set<ITransferFormatFeature>(this);
        Features.Set<IHttpTransportFeature>(this);
        Features.Set<IConnectionLifetimeFeature>(this);
    }
    public override string ConnectionId { get; set; }
    public IDuplexPipe Application { get; }
    public WebSocketConnectionDispatcherOptions Options { get; }
    public override IFeatureCollection Features { get; }
    internal Task? TransportTask { get; set; }
    internal Task? ApplicationTask { get; set; }
    public override IDictionary<object, object?> Items
    {
        get
        {
            if (_items == null)
            {
                lock (_itemsLock)
                {
                    if (_items == null)
                    {
                        _items = new ConnectionItems(new ConcurrentDictionary<object, object?>());
                    }
                }
            }
            return _items;
        }
        set => _items = value ?? throw new ArgumentNullException(nameof(value));
    }
    public ClaimsPrincipal? User { get; set; }
    public TransferFormat SupportedFormats { get; set; }
    public TransferFormat ActiveFormat { get; set; }
    public HttpTransportType TransportType => HttpTransportType.WebSockets;
    public override IDuplexPipe Transport { get; set; }
    public CancellationTokenSource? Cancellation { get; set; }

    internal bool TryActivateConnection(
        ConnectionDelegate connectionDelegate,
        IHttpTransport transport,
        HttpContext context)
    {
        // Call into the end point passing the connection
        ApplicationTask = ExecuteApplication(connectionDelegate);

        // Start the transport
        TransportTask = transport.ProcessRequestAsync(context, context.RequestAborted);

        return true;
    }

    private async Task ExecuteApplication(ConnectionDelegate connectionDelegate)
    {
        // Jump onto the thread pool thread so blocking user code doesn't block the setup of the
        // connection and transport
        await Task.Yield();

        // Running this in an async method turns sync exceptions into async ones
        await connectionDelegate(this);
    }

    public override void Abort(ConnectionAbortedException abortReason)
        => ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts!).Cancel(), _connectionClosedTokenSource);

    public async Task DisposeAsync(bool closeGracefully = false)
    {
        Task disposeTask;

        try
        {
            lock (_stateLock)
            {
                if (_disposed)
                {
                    disposeTask = _disposeTcs.Task;
                }
                else
                {
                    _disposed = true;

                    Log.DisposingConnection(_logger, ConnectionId);

                    var applicationTask = ApplicationTask ?? Task.CompletedTask;
                    var transportTask = TransportTask ?? Task.CompletedTask;

                    disposeTask = WaitOnTasks(applicationTask, transportTask, closeGracefully);
                }
            }
        }
        finally
        {
            Cancellation?.Dispose();
            Cancellation = null;
        }

        await disposeTask;
    }

    private async Task WaitOnTasks(Task applicationTask, Task transportTask, bool closeGracefully)
    {
        try
        {
            // Closing gracefully means we're only going to close the finished sides of the pipe
            // If the application finishes, that means it's done with the transport pipe
            // If the transport finishes, that means it's done with the application pipe
            if (!closeGracefully)
            {
                Application?.Output.CancelPendingFlush();

                // The websocket transport will close the application output automatically when reading is canceled
                Cancellation?.Cancel();
            }

            // Wait for either to finish
            var result = await Task.WhenAny(applicationTask, transportTask);

            // If the application is complete, complete the transport pipe (it's the pipe to the transport)
            if (result == applicationTask)
            {
                Transport?.Output.Complete(applicationTask.Exception?.InnerException);
                Transport?.Input.Complete();

                try
                {
                    Log.WaitingForTransport(_logger, TransportType);

                    // Transports are written by us and are well behaved, wait for them to drain
                    await transportTask;
                }
                finally
                {
                    Log.TransportComplete(_logger, TransportType);

                    // Now complete the application
                    Application?.Output.Complete();
                    Application?.Input.Complete();

                    // Trigger ConnectionClosed
                    ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts!).Cancel(), _connectionClosedTokenSource);
                }
            }
            else
            {
                // If the transport is complete, complete the application pipes
                Application?.Output.Complete(transportTask.Exception?.InnerException);
                Application?.Input.Complete();

                // Trigger ConnectionClosed
                ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts!).Cancel(), _connectionClosedTokenSource);

                try
                {
                    // A poorly written application *could* in theory get stuck forever and it'll show up as a memory leak
                    Log.WaitingForApplication(_logger);

                    await applicationTask;
                }
                finally
                {
                    Log.ApplicationComplete(_logger);

                    Transport?.Output.Complete();
                    Transport?.Input.Complete();
                }
            }

            // Notify all waiters that we're done disposing
            _disposeTcs.TrySetResult();
        }
        catch (OperationCanceledException)
        {
            _disposeTcs.TrySetCanceled();

            throw;
        }
        catch (Exception ex)
        {
            _disposeTcs.TrySetException(ex);

            throw;
        }
    }
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Trace, "Disposing connection {TransportConnectionId}.", EventName = "DisposingConnection")]
        public static partial void DisposingConnection(ILogger logger, string transportConnectionId);

        [LoggerMessage(2, LogLevel.Trace, "Waiting for application to complete.", EventName = "WaitingForApplication")]
        public static partial void WaitingForApplication(ILogger logger);

        [LoggerMessage(3, LogLevel.Trace, "Application complete.", EventName = "ApplicationComplete")]
        public static partial void ApplicationComplete(ILogger logger);

        [LoggerMessage(4, LogLevel.Trace, "Waiting for {TransportType} transport to complete.", EventName = "WaitingForTransport")]
        public static partial void WaitingForTransport(ILogger logger, HttpTransportType transportType);

        [LoggerMessage(5, LogLevel.Trace, "{TransportType} transport complete.", EventName = "TransportComplete")]
        public static partial void TransportComplete(ILogger logger, HttpTransportType transportType);

        [LoggerMessage(6, LogLevel.Trace, "Shutting down both the application and the {TransportType} transport.", EventName = "ShuttingDownTransportAndApplication")]
        public static partial void ShuttingDownTransportAndApplication(ILogger logger, HttpTransportType transportType);

        [LoggerMessage(7, LogLevel.Trace, "Waiting for both the application and {TransportType} transport to complete.", EventName = "WaitingForTransportAndApplication")]
        public static partial void WaitingForTransportAndApplication(ILogger logger, HttpTransportType transportType);

        [LoggerMessage(8, LogLevel.Trace, "The application and {TransportType} transport are both complete.", EventName = "TransportAndApplicationComplete")]
        public static partial void TransportAndApplicationComplete(ILogger logger, HttpTransportType transportType);

        [LoggerMessage(9, LogLevel.Trace, "{Timeout}ms elapsed attempting to send a message to the transport. Closing connection {TransportConnectionId}.", EventName = "TransportSendTimeout")]
        public static partial void TransportSendTimeout(ILogger logger, TimeSpan timeout, string transportConnectionId);
    }
}

