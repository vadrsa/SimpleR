using System.IO.Pipelines;

namespace SimpleR;

public abstract class WebsocketHandler : IDisposable
{
    private readonly SemaphoreSlim _writeLock = new(1);
    private bool _isConnected;

    internal WebSocketConnectionContext? _context;
    private bool _isDisposed;

    internal PipeWriter? TransportWriter { get; set; }

    protected WebSocketConnectionContext Context
        => _context ?? throw new InvalidOperationException("Context is not available before OnCreatedAsync is called.");

    public async Task SendMessageAsync(OutboundSocketData data, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Socket connection is not active.");
        }

        if (TransportWriter != null)
        {
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                data.Write(TransportWriter);

                await TransportWriter.FlushAsync(cancellationToken);

            }
            finally { _writeLock.Release(); }
        }
    }

    public virtual Task OnConnectedAsync() => Task.CompletedTask;

    public virtual Task OnDisconnectedAsync() => Task.CompletedTask;

    internal Task HandleConnectionAsync()
    {
        _isConnected = true;
        return OnConnectedAsync();
    }

    internal async Task HandleDisconnectionAsync()
    {
        if (_isConnected)
        {
            _isConnected = false;
            await OnDisconnectedAsync();
        }
    }

    public abstract Task OnMessageAsync(IncomingSocketData data);

    public virtual void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            Context.Abort();
            // don't wait for disposal
            ThreadPool.UnsafeQueueUserWorkItem(ctx => _ = ((WebSocketConnectionContext)ctx!).DisposeAsync(), Context);
            GC.SuppressFinalize(this);
        }
    }

    public async Task DisconnectAndDisposeAsync()
    {
        await HandleDisconnectionAsync();
        Dispose();
    }
}
