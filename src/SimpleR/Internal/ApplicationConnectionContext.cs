using System.IO.Pipelines;
using System.Security.Claims;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using SimpleR.Protocol;

namespace SimpleR.Internal;

internal class ApplicationConnectionContext<TMessageOut> : IWebsocketConnectionContext<TMessageOut>
{
    private readonly ConnectionContext _connectionContext;
    private readonly IMessageWriter<TMessageOut> _writer;
    private ClaimsPrincipal? _user;
    private readonly SemaphoreSlim _writeLock = new(1);
    private readonly CancellationTokenRegistration _closedRegistration;
    private readonly TaskCompletionSource _abortCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _connectionAbortedTokenSource = new();
    private static readonly WaitCallback _abortedCallback = AbortConnection;
    private volatile bool _connectionAborted;

    public ApplicationConnectionContext(ConnectionContext context, IMessageWriter<TMessageOut> writer)
    {
        _connectionContext = context;
        _writer = writer;
        ConnectionAborted = _connectionAbortedTokenSource.Token;
        
        _closedRegistration = _connectionContext.ConnectionClosed.Register(static (state) => ((ApplicationConnectionContext<TMessageOut>)state!).Abort(), this);
    }

    public string ConnectionId => _connectionContext.ConnectionId;

    public ClaimsPrincipal User
    {
        get
        {
            if (_user is null)
            {
                _user = Features.Get<IConnectionUserFeature>()?.User ?? new ClaimsPrincipal();
            }
            return _user;
        }
    }

    /// <summary>
    /// Gets the collection of features available on this connection.
    /// </summary>
    public IFeatureCollection Features => _connectionContext.Features;
    
    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that notifies when the connection is aborted.
    /// </summary>
    public virtual CancellationToken ConnectionAborted { get; }
    
    // Used by WebSocketConnectionHandler
    internal PipeReader Input => _connectionContext.Transport.Input;
    internal Exception? CloseException { get; private set; }

    public ValueTask WriteAsync(TMessageOut message, CancellationToken cancellationToken = default)
    {
        // Try to grab the lock synchronously, if we fail, go to the slower path
        if (!_writeLock.Wait(0, CancellationToken.None))
        {
            return new ValueTask(WriteSlowAsync(message, cancellationToken));
        }
        
        if (_connectionAborted)
        {
            _writeLock.Release();
            return default;
        }

        // This method should never throw synchronously
        var task = WriteCore(message, cancellationToken);

        // The write didn't complete synchronously so await completion
        if (!task.IsCompletedSuccessfully)
        {
            return new ValueTask(CompleteWriteAsync(task));
        }
        else
        {
            // If it's a IValueTaskSource backed ValueTask,
            // inform it its result has been read so it can reset
            task.GetAwaiter().GetResult();
        }

        // Otherwise, release the lock acquired when entering WriteAsync
        _writeLock.Release();

        return default;
    }

    private ValueTask<FlushResult> WriteCore(TMessageOut message, CancellationToken cancellationToken)
    {
        try
        {
            // We know that we are only writing this message to one receiver, so we can
            // write it without caching.
            _writer.WriteMessage(message, _connectionContext.Transport.Output);

            return _connectionContext.Transport.Output.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            CloseException = ex;
            // TODO: log
            Abort();
            return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
        }
    }

    private async Task CompleteWriteAsync(ValueTask<FlushResult> task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            CloseException = ex;
            // TODO: log
            Abort();
        }
        finally
        {
            // Release the lock acquired when entering WriteAsync
            _writeLock.Release();
        }
    }

    private async Task WriteSlowAsync(TMessageOut message, CancellationToken cancellationToken)
    {
        // Failed to get the lock immediately when entering WriteAsync so await until it is available
        await _writeLock.WaitAsync(cancellationToken);

        try
        {if (_connectionAborted)
            {
                return;
            }
            
            await WriteCore(message, cancellationToken);
        }
        catch (Exception ex)
        {
            CloseException = ex;
            // TODO: log
            Abort();
        }
        finally
        {
            _writeLock.Release();
        }
    }
    private static void AbortConnection(object? state)
    {
        var connection = (ApplicationConnectionContext<TMessageOut>)state!;

        try
        {
            connection._connectionAbortedTokenSource.Cancel();
        }
        catch// (Exception ex)
        {
            // TODO: log
            //Log.AbortFailed(connection._logger, ex);
        }
        finally
        {
            _ = InnerAbortConnection(connection);
        }

        static async Task InnerAbortConnection(ApplicationConnectionContext<TMessageOut> connection)
        {
            // We lock to make sure all writes are done before triggering the completion of the pipe
            await connection._writeLock.WaitAsync();
            try
            {
                // Communicate the fact that we're finished triggering abort callbacks
                // HubOnDisconnectedAsync is waiting on this to complete the Pipe
                connection._abortCompletedTcs.TrySetResult();
            }
            finally
            {
                connection._writeLock.Release();
            }
        }
    }
    
    // Used by the WebSocketConnectionHandler only
    internal Task AbortAsync()
    {
        Abort();

        // Acquire lock to make sure all writes are completed
        if (!_writeLock.Wait(0))
        {
            return AbortAsyncSlow();
        }
        _writeLock.Release();
        return _abortCompletedTcs.Task;
    }

    private async Task AbortAsyncSlow()
    {
        await _writeLock.WaitAsync();
        _writeLock.Release();
        await _abortCompletedTcs.Task;
    }
    
    public void Abort()
    {
        _connectionAborted = true;
        // Cancel any current writes or writes that are about to happen and have already gone past the _connectionAborted bool
        // We have to do this outside of the lock otherwise it could hang if the write is observing backpressure
        _connectionContext.Transport.Output.CancelPendingFlush();

        // If we already triggered the token then noop, this isn't thread safe but it's good enough
        // to avoid spawning a new task in the most common cases
        if (_connectionAbortedTokenSource.IsCancellationRequested)
        {
            return;
        }

        _connectionContext.Transport.Input.CancelPendingRead();

        // We fire and forget since this can trigger user code to run
        ThreadPool.QueueUserWorkItem(_abortedCallback, this);
    }

    internal void Cleanup()
    {
        _closedRegistration.Dispose();
    }
}