namespace SimpleR.Authobahn.Server;

public class EchoDispatcher : IWebSocketMessageDispatcher<byte[]>
{
    public Task OnConnectedAsync(IWebsocketConnectionContext<byte[]> connection)
    {
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<byte[]> connection, Exception? exception)
    {
        return Task.CompletedTask;
    }

    public async Task DispatchMessageAsync(IWebsocketConnectionContext<byte[]> connection, byte[] message)
    {
        await connection.WriteAsync(message);
    }
}