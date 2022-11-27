using SampleProtocols;
using SimpleR;

namespace Sample1;

public class EndOfMessageDelimitedProtocolDispatcher : IWebSocketMessageDispatcher<EndOfMessageDelimitedSampleProtocolMessage>
{
    public Task OnConnectedAsync(IWebsocketConnectionContext<EndOfMessageDelimitedSampleProtocolMessage> connection)
    {
        Console.WriteLine($"Connection {connection.ConnectionId} connected to {nameof(EndOfMessageDelimitedProtocolDispatcher)}");
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<EndOfMessageDelimitedSampleProtocolMessage> connection, Exception? exception)
    {
        Console.WriteLine($"Connection {connection.ConnectionId} disconnected from {nameof(EndOfMessageDelimitedProtocolDispatcher)}. Exception: {exception}");
        return Task.CompletedTask;
    }

    public async Task DispatchMessageAsync(IWebsocketConnectionContext<EndOfMessageDelimitedSampleProtocolMessage> connection, EndOfMessageDelimitedSampleProtocolMessage message)
    {
        Console.WriteLine($"Connection {connection.ConnectionId} received message '{message.Content}' in {nameof(EndOfMessageDelimitedProtocolDispatcher)}");
        await connection.WriteAsync(message);
    }
}