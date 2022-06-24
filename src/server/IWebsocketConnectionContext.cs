using System.Security.Claims;

namespace SimpleR;

public interface IWebsocketConnectionContext<in TMessage>
{
    string ConnectionId { get; }
    ClaimsPrincipal User { get; }
    ValueTask WriteAsync(TMessage message, CancellationToken cancellationToken = default);
    void Abort();
}