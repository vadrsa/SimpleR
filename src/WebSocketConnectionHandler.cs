using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimpleR;

internal partial class WebSocketConnectionHandler<THandler> : ConnectionHandler
    where THandler : WebsocketHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public WebSocketConnectionHandler(IServiceProvider serviceProvider,
        ILogger<WebSocketConnectionHandler<THandler>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        using var handler = _serviceProvider.GetRequiredService<THandler>();
        var wsConnection = (WebSocketConnectionContext)connection;

        try
        {
            handler.TransportWriter = connection.Transport.Output;
            handler._context = wsConnection;

            wsConnection.ConnectionClosed.Register(async () => await handler.HandleDisconnectionAsync());

            // this is causing issues with TestHost, removing for now since we don't need it,
            // but when it is uncommented this part should be investigated
            //wsConnection.User = wsConnection.GetHttpContext()?.User;

            await handler.HandleConnectionAsync();
        }
        catch (Exception ex)
        {
            Log.UnableToProcessConnection(_logger, ex);
            connection.Abort(new ConnectionAbortedException("Unable to handle connection."));
            return;
        }

        var input = connection.Transport.Input;

        while (true)
        {
            var result = await input.ReadAsync();
            var buffer = result.Buffer;
            var processed = false;
            try
            {
                if (result.IsCanceled)
                {
                    break;
                }

                if (!buffer.IsEmpty && wsConnection.IsEndOfMessage)
                {
                    processed = true;
                    try
                    {
                        await handler.OnMessageAsync(new IncomingSocketData(buffer));
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
                // We mark examined as `buffer.End` so that if we didn't receive a full frame, we'll wait for more data
                // before yielding the read again.
                if (processed)
                {
                    input.AdvanceTo(buffer.End);
                }
                else
                {
                    input.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }
    }
}
