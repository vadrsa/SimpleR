using SampleProtocols;
using SimpleR;

namespace Sample1;

public class LengthPrefixedProtocolDispatcher : IWebSocketMessageDispatcher<LengthPrefixedProtocolMessage>
{
    public Task OnConnectedAsync(IWebsocketConnectionContext<LengthPrefixedProtocolMessage> connection)
    {
        Console.WriteLine($"Connection {connection.ConnectionId} connected to {nameof(LengthPrefixedProtocolDispatcher)}");
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<LengthPrefixedProtocolMessage> connection, Exception? exception)
    {
        Console.WriteLine($"Connection {connection.ConnectionId} disconnected from {nameof(LengthPrefixedProtocolDispatcher)}. Exception: {exception}");
        return Task.CompletedTask;
    }

    public async Task DispatchMessageAsync(IWebsocketConnectionContext<LengthPrefixedProtocolMessage> connection, LengthPrefixedProtocolMessage message)
    {
        Console.WriteLine($"Connection {connection.ConnectionId} received message '{message.Content}' in {nameof(LengthPrefixedProtocolDispatcher)}");
        await connection.WriteAsync(message);
    }
}