using System.IO.Pipelines;
using SimpleR.Internal;

namespace SimpleR;

public interface IWebSocketMessageDispatcher<TMessage>
{
    Task OnConnectedAsync(IWebsocketConnectionContext<TMessage> connection);

    Task OnDisconnectedAsync(IWebsocketConnectionContext<TMessage> connection, Exception? exception);

    Task DispatchMessageAsync(IWebsocketConnectionContext<TMessage> connection, TMessage message);
}
