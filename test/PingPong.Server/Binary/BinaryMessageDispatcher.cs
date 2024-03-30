using SimpleR;

namespace PingPong.Server.Binary;

public class BinaryMessageDispatcher : IWebSocketMessageDispatcher<PingPongBinary>
{
    private readonly ConnectionManager _connectionManager;

    public BinaryMessageDispatcher(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public Task OnConnectedAsync(IWebsocketConnectionContext<PingPongBinary> connection)
    {
        _connectionManager.Connections.Add(connection.ConnectionId);
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<PingPongBinary> connection, Exception? exception)
    {
        _connectionManager.Disconnections.Add(connection.ConnectionId);
        return Task.CompletedTask;
    }

    public async Task DispatchMessageAsync(IWebsocketConnectionContext<PingPongBinary> connection, PingPongBinary message)
    {
        await connection.WriteAsync(new PingPongBinary{ Payload = message.Payload.Reverse().ToArray() });
    }
}