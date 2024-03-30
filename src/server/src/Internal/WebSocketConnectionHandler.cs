using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using SimpleR.Protocol;

namespace SimpleR.Internal;

internal partial class WebSocketConnectionHandler<TMessage> : ConnectionHandler
{
    private readonly IMessageProtocol<TMessage> _messageProtocol;
    private readonly IWebSocketMessageDispatcher<TMessage> _dispatcher;
    private readonly ILogger _logger;

    public WebSocketConnectionHandler(IMessageProtocol<TMessage> messageProtocol,
        IWebSocketMessageDispatcher<TMessage> messageDispatcher,
        ILogger<WebSocketConnectionHandler<TMessage>> logger)
    {
        _messageProtocol = messageProtocol;
        _dispatcher = messageDispatcher;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        Log.ConnectedStarting(_logger);

        var appConnectionContext = new ApplicationConnectionContext<TMessage>(connection, _messageProtocol);
        
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
    private async Task RunApplicationAsync(ApplicationConnectionContext<TMessage> connection)
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

    private async Task OnDisconnectedAsync(ApplicationConnectionContext<TMessage> connection, Exception? exception)
    {
        // We wait on abort to complete, this is so that we can guarantee that all callbacks have fired
        // before OnDisconnectedAsync

        // Ensure the connection is aborted before firing disconnect
        await connection.AbortAsync();

        await _dispatcher.OnDisconnectedAsync(connection, exception);
    }
    
    private async Task DispatchMessagesAsync(ApplicationConnectionContext<TMessage> connection)
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

                while (!buffer.IsEmpty && _messageProtocol.TryParseMessage(ref buffer, out var message))
                {
                    await _dispatcher.DispatchMessageAsync(connection, message);
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
}