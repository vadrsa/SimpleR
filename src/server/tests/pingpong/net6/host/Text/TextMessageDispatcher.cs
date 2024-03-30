using System.Globalization;
using SimpleR;

namespace PingPongNet6Server.Text;

public class TextMessageDispatcher : IWebSocketMessageDispatcher<PingPongText>
{
    private readonly ConnectionManager _connectionManager;

    public TextMessageDispatcher(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public Task OnConnectedAsync(IWebsocketConnectionContext<PingPongText> connection)
    {
        _connectionManager.Connections.Add(connection.ConnectionId);
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<PingPongText> connection, Exception? exception)
    {
        _connectionManager.Disconnections.Add(connection.ConnectionId);
        return Task.CompletedTask;
    }

    public async Task DispatchMessageAsync(IWebsocketConnectionContext<PingPongText> connection, PingPongText message)
    {
        await connection.WriteAsync(new PingPongText{ Payload = string.Concat(ToTextElements(message.Payload).Reverse()) });
    }
    
    private static IEnumerable<string> ToTextElements(string source)
    {
        var e = StringInfo.GetTextElementEnumerator(source);
        while (e.MoveNext())
        {
            yield return e.GetTextElement();
        }
    }
}