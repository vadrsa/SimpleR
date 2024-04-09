using System.Net.WebSockets;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging.Abstractions;

namespace Websocket.Client;

public static class WebsocketClientHelpers
{
    public static async Task<(WebsocketClient client, WebSocket socket)> ConnectWebsocketAsync(this TestServer server, string route = "", string schema = "wss")
    {
        var client = server.CreateWebSocketClient();
            
        var wsUri = new UriBuilder(server.BaseAddress + route.TrimStart('/'))
        {
            Scheme = schema
        }.Uri;
        WebSocket? socket = null;
        var wsClient = new WebsocketClient(wsUri, new NullLogger<WebsocketClient>(), async (_, _) =>
        {
            socket = await client.ConnectAsync(wsUri, CancellationToken.None);
            return socket;
        });
        await wsClient.Start();
        return (wsClient, socket!);
    }
    
    public static (WebsocketClient client, WebSocket socket) CreateWebsocketAsync(this TestServer server, string route = "", string schema = "wss")
    {
        var client = server.CreateWebSocketClient();
            
        var wsUri = new UriBuilder(server.BaseAddress + route.TrimStart('/'))
        {
            Scheme = schema
        }.Uri;
        WebSocket? socket = null;
        var wsClient = new WebsocketClient(wsUri, new NullLogger<WebsocketClient>(), async (_, _) =>
        {
            socket = await client.ConnectAsync(wsUri, CancellationToken.None);
            return socket;
        });
        return (wsClient, socket!);
    }
}