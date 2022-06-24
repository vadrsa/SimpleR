using System.Globalization;
using SimpleR;

namespace PingPong.Server.NoOp;

public class NoOpMessageDispatcher : IWebSocketMessageDispatcher<EmptyMessage>
{
    private readonly ConnectionManager _connectionManager;

    public NoOpMessageDispatcher(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public Task OnConnectedAsync(IWebsocketConnectionContext<EmptyMessage> connection)
    {
        _connectionManager.OnConnected(connection.ConnectionId);
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<EmptyMessage> connection, Exception? exception)
    {
        _connectionManager.OnDisconnected(connection.ConnectionId);
        return Task.CompletedTask;
    }

    public Task DispatchMessageAsync(IWebsocketConnectionContext<EmptyMessage> connection, EmptyMessage message)
    {
        return Task.CompletedTask;
    }
    
}