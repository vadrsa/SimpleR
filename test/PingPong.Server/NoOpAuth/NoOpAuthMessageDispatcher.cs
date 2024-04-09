using Microsoft.AspNetCore.Authorization;
using SimpleR;

namespace PingPong.Server.NoOpAuth;

public class NoOpAuthMessageDispatcher : IWebSocketMessageDispatcher<EmptyMessage>
{

    public NoOpAuthMessageDispatcher(ConnectionManager connectionManager)
    {
    }

    public Task OnConnectedAsync(IWebsocketConnectionContext<EmptyMessage> connection)
    {
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<EmptyMessage> connection, Exception? exception)
    {
        return Task.CompletedTask;
    }

    public Task DispatchMessageAsync(IWebsocketConnectionContext<EmptyMessage> connection, EmptyMessage message)
    {
        return Task.CompletedTask;
    }
    
}