using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Logging;

namespace SimpleR.Internal;

internal class WebSocketConnectionDispatcher
{
    public WebSocketConnectionDispatcher(WebSocketConnectionManager connectionManager, ILoggerFactory loggerFactory)
    {
        ConnectionManager = connectionManager;
        LoggerFactory = loggerFactory;
    }

    private WebSocketConnectionManager ConnectionManager { get; }
    private ILoggerFactory LoggerFactory { get; }

    public async Task ExecuteAsync(HttpContext httpContext, WebSocketConnectionDispatcherOptions options, ConnectionDelegate connectionDelegate)
    {
        var connection = ConnectionManager.CreateConnection(options);

        var transport = new WebSocketsServerTransport(options.WebSockets, connection.Application, connection, LoggerFactory);

        if (connection.TryActivateConnection(connectionDelegate, transport, httpContext))
        {
            // Wait for any of them to end
            await Task.WhenAny(connection.ApplicationTask!, connection.TransportTask!);

            await ConnectionManager.DisposeAndRemoveAsync(connection, closeGracefully: true);
        }
    }
}
