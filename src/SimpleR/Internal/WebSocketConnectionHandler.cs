using System.Buffers;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using SimpleR.Protocol;

namespace SimpleR.Internal;

internal partial class WebSocketConnectionHandler<TMessageIn, TMessageOut> : ConnectionHandler
{
    private readonly IMessageProtocol<TMessageIn, TMessageOut> _messageProtocol;
    private readonly IWebSocketMessageDispatcher<TMessageIn, TMessageOut> _dispatcher;
    private readonly ILogger _logger;

    public WebSocketConnectionHandler(IMessageProtocol<TMessageIn, TMessageOut> messageProtocol,
        IWebSocketMessageDispatcher<TMessageIn, TMessageOut> messageDispatcher,
        ILogger<WebSocketConnectionHandler<TMessageIn, TMessageOut>> logger)
    {
        _messageProtocol = messageProtocol;
        _dispatcher = messageDispatcher;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        Log.ConnectedStarting(_logger);

        var appConnectionContext = new ApplicationConnectionContext<TMessageOut>(connection, _messageProtocol);
        
        try
        {
            // TODO: add lifetime manager
            await RunApplicationAsync(appConnectionContext);
        }
        finally
        {
            appConnectionContext.Cleanup();

            Log.ConnectedEnding(_logger);
        }
    }
    private async Task RunApplicationAsync(ApplicationConnectionContext<TMessageOut> connection)
    {
        try
        {
            await _dispatcher.OnConnectedAsync(connection);
        }
        catch (Exception ex)
        {
            Log.ErrorDispatchingEvent(_logger, "OnConnectedAsync", ex);

            // return instead of throw to let close message send successfully
            return;
        }

        try
        {
            await DispatchMessagesAsync(connection);
        }
        catch (OperationCanceledException)
        {
            // Don't treat OperationCanceledException as an error, it's basically a "control flow"
            // exception to stop things from running
        }
        catch (Exception ex)
        {
            Log.ErrorProcessingRequest(_logger, ex);

            await OnDisconnectedAsync(connection, ex);

            return;
        }

        await OnDisconnectedAsync(connection, connection.CloseException);
    }

    private async Task OnDisconnectedAsync(ApplicationConnectionContext<TMessageOut> connection, Exception? exception)
    {
        // We wait on abort to complete, this is so that we can guarantee that all callbacks have fired
        // before OnDisconnectedAsync

        try
        {
            // Ensure the connection is aborted before firing disconnect
            await connection.AbortAsync();
        }
        finally
        {
            await _dispatcher.OnDisconnectedAsync(connection, exception);
        }
    }
    
    private async Task DispatchMessagesAsync(ApplicationConnectionContext<TMessageOut> connection)
    {
        var input = connection.Input;

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

                while (!buffer.IsEmpty && TryParseMessageImpl(ref buffer, out var message, out var exception))
                {
                    if (exception == null)
                    {
                        await _dispatcher.DispatchMessageAsync(connection, message);
                    }
                    else
                    {
                        await _dispatcher.OnParsingIssueAsync(connection, exception);
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

    private bool TryParseMessageImpl(ref ReadOnlySequence<byte> buffer, out TMessageIn message, out Exception? exception)
    {
        try
        {
            var result =  _messageProtocol.TryParseMessage(ref buffer, out message);
            exception = null;
            return result;
        }
        catch (Exception ex)
        {
            exception = ex;
            message = default!;
            return true;
        }
    }
}