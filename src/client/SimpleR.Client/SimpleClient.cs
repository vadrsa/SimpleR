using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleR.Client.Internal;
using SimpleR.Protocol;

namespace SimpleR.Client
{
    public class SimpleClient<TMessage> : IDisposable, IAsyncDisposable
    {
        private readonly WebSocket _webSocket;
        private readonly IMessageProtocol<TMessage> _protocol;
        private readonly WebSocketConnectionDispatcherOptions _options;
        private readonly CancellationTokenSource _connectionCts = new CancellationTokenSource();
        private readonly TaskCompletionSource<bool> _disposeTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);
        private bool _disposed;
        private readonly object _stateLock = new object();
        private IDuplexPipe Application { get; }
        private IDuplexPipe Transport { get; }

        public SimpleClient(WebSocket webSocket, IMessageProtocol<TMessage> protocol, WebSocketConnectionDispatcherOptions options)
        {
            if (protocol is IEndOfMessageDelimitedProtocol endOfMessageDelimitedProtocol)
            {
                options.WebSockets.EndOfMessageBytes = endOfMessageDelimitedProtocol.EndOfMessageBytes;
            }
            options.WebSockets.TransferFormat = protocol.Format;
            
            _webSocket = webSocket;
            _protocol = protocol;
            _options = options;
            
            var pair = DuplexPipe.CreateConnectionPair(options.TransportPipeOptions, options.AppPipeOptions);
            Application = pair.Application;
            Transport = pair.Transport;
            var transport = new WebSocketClientTransport(_options.WebSockets, Application);

            TransportTask = transport.ProcessSocketAsync(_webSocket, _connectionCts.Token);
            ApplicationTask = StartApplication();
        }

        public event Action<TMessage> OnMessage;

        public async Task SendAsync(TMessage message, CancellationToken cancellationToken = default)
        {
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                _protocol.WriteMessage(message, Transport.Output);

                await Transport.Output.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private async Task StartApplication()
        {
            var input = Transport.Input;

            while (true)
            {
                var result = await input.ReadAsync();
                var buffer = result.Buffer;
                try
                {
                    if (result.IsCanceled)
                    {
                        break;
                    }

                    while (!buffer.IsEmpty && _protocol.TryParseMessage(ref buffer, out var message))
                    {
                        try
                        {
                            OnMessage?.Invoke(message);
                            // await _dispatcher.DispatchMessageAsync(connection, message);
                        }
                        catch (Exception ex)
                        {
                            // Log.UnableToProcessMessage(_logger, ex);
                        }
                    }

                    if (result.IsCompleted)
                    {
                        if (!buffer.IsEmpty)
                        {
                            throw new InvalidDataException("Connection terminated while reading a message.");
                        }
                        break;
                    }
                }
                finally
                {
                    // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                    // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
                    // before yielding the read again.
                    input.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }

        private Task ApplicationTask { get; set; }
        private Task TransportTask { get; set; }

        public ValueTask DisposeAsync() => DisposeAsync(false);

        public ValueTask DisposeAsync(bool closeGracefully)
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

                        // Log.DisposingConnection(_logger, ConnectionId);

                        var applicationTask = ApplicationTask ?? Task.CompletedTask;
                        var transportTask = TransportTask ?? Task.CompletedTask;

                        disposeTask = WaitOnTasks(applicationTask, transportTask, closeGracefully);
                    }
                }
            }
            finally
            {
                _connectionCts.Dispose();
            }

            if (disposeTask.IsCompleted)
            {
                return default;
            }

            return new ValueTask(disposeTask);
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
                    Application.Output.CancelPendingFlush();

                    // The websocket transport will close the application output automatically when reading is canceled
                    _connectionCts.Cancel();
                }

                // Wait for either to finish
                var result = await Task.WhenAny(applicationTask, transportTask);

                // If the application is complete, complete the transport pipe (it's the pipe to the transport)
                if (result == applicationTask)
                {
                    Transport.Output.Complete(applicationTask.Exception?.InnerException);
                    Transport.Input.Complete();

                    try
                    {
                        //Log.WaitingForTransport(_logger, TransportType);

                        // Transports are written by us and are well behaved, wait for them to drain
                        await transportTask;
                    }
                    finally
                    {
                        //Log.TransportComplete(_logger, TransportType);

                        // Now complete the application
                        Application.Output.Complete();
                        Application.Input.Complete();

                        // Trigger ConnectionClosed
                        // ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts!).Cancel(), _connectionClosedTokenSource);
                    }
                }
                else
                {
                    // If the transport is complete, complete the application pipes
                    Application.Output.Complete(transportTask.Exception?.InnerException);
                    Application.Input.Complete();

                    // Trigger ConnectionClosed
                    // ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts!).Cancel(), _connectionClosedTokenSource);

                    try
                    {
                        // A poorly written application *could* in theory get stuck forever and it'll show up as a memory leak
                        // Log.WaitingForApplication(_logger);

                        await applicationTask;
                    }
                    finally
                    {
                        // Log.ApplicationComplete(_logger);

                        Transport.Output.Complete();
                        Transport.Input.Complete();
                    }
                }

                // Notify all waiters that we're done disposing
                _disposeTcs.TrySetResult(true);
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
        
        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
            _webSocket.Dispose();
        }
    }
}
