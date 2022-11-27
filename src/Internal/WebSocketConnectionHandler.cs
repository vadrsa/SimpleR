using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using SimpleR.Protocol;

namespace SimpleR.Internal;

internal partial class WebSocketConnectionHandler<THandler, TMessage> : ConnectionHandler
    where THandler : IWebSocketMessageDispatcher<TMessage>
{
    private readonly IMessageReader<TMessage> _messageReader;
    private readonly IMessageWriter<TMessage> _messageWriter;
    private readonly IWebSocketMessageDispatcher<TMessage> _dispatcher;
    private readonly ILogger _logger;

    public WebSocketConnectionHandler(IMessageReader<TMessage> messageReader,
        IMessageWriter<TMessage> messageWriter,
        IWebSocketMessageDispatcher<TMessage> messageDispatcher,
        ILogger<WebSocketConnectionHandler<THandler, TMessage>> logger)
    {
        _messageReader = messageReader;
        _messageWriter = messageWriter;
        _dispatcher = messageDispatcher;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var appConnectionContext = new ApplicationConnectionContext<TMessage>(connection, _messageWriter);

        try
        {
            // TODO: add lifetime manager
            // await _lifetimeManager.OnConnectedAsync(connectionContext);
            await RunApplicationAsync(appConnectionContext);
        }
        finally
        {
            appConnectionContext.Cleanup();

            // Log.ConnectedEnding(_logger);
            // await _lifetimeManager.OnDisconnectedAsync(connectionContext);
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
            // TODO: log
            //Log.ErrorDispatchingHubEvent(_logger, "OnConnectedAsync", ex);

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
            // TODO: log
            //Log.ErrorProcessingRequest(_logger, ex);

            await HubOnDisconnectedAsync(connection, ex);

            return;
        }

        await HubOnDisconnectedAsync(connection, connection.CloseException);
    }

    private async Task HubOnDisconnectedAsync(ApplicationConnectionContext<TMessage> connection, Exception? exception)
    {
        // We wait on abort to complete, this is so that we can guarantee that all callbacks have fired
        // before OnDisconnectedAsync

        // Ensure the connection is aborted before firing disconnect
        await connection.AbortAsync();

        try
        {
            await _dispatcher.OnDisconnectedAsync(connection, exception);
        }
        catch (Exception ex)
        {
            // TODO: log
            //Log.ErrorDispatchingHubEvent(_logger, "OnDisconnectedAsync", ex);
            throw;
        }
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

                while (!buffer.IsEmpty && _messageReader.TryParseMessage(ref buffer, out var message))
                {
                    try
                    {
                        await _dispatcher.DispatchMessageAsync(connection, message);
                    }
                    catch (Exception ex)
                    {
                        Log.UnableToProcessMessage(_logger, ex);
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
}
