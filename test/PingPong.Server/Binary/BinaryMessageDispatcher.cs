using SimpleR;

namespace PingPong.Server.Binary;

public class BinaryMessageDispatcher : IWebSocketMessageDispatcher<PingPongBinary>
{
    public Task OnConnectedAsync(IWebsocketConnectionContext<PingPongBinary> connection)
    {
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<PingPongBinary> connection, Exception? exception)
    {
        return Task.CompletedTask;
    }

    public async Task DispatchMessageAsync(IWebsocketConnectionContext<PingPongBinary> connection, PingPongBinary message)
    {
        await connection.WriteAsync(new PingPongBinary{ Payload = message.Payload.Reverse().ToArray() });
    }
}