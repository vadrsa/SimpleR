using SimpleR;
using SimpleR.Ocpp;
using System.Security.Claims;

namespace SimpleOcpp.Server;

public class SimpleOcppDispatcher : IWebSocketMessageDispatcher<IOcppMessage>
{
    private readonly ILogger<SimpleOcppDispatcher> _logger;

    public SimpleOcppDispatcher(ILogger<SimpleOcppDispatcher> logger)
    {
        _logger = logger;
    }

    public Task OnConnectedAsync(IWebsocketConnectionContext<IOcppMessage> connection)
    {
        _logger.LogInformation("Charger with id {ChargerId} connected", connection.User.FindFirstValue(ClaimTypes.NameIdentifier));
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<IOcppMessage> connection, Exception? exception)
    {
        _logger.LogInformation("Charger with id {ChargerId} disconnected", connection.User.FindFirstValue(ClaimTypes.NameIdentifier));
        return Task.CompletedTask;
    }

    public Task DispatchMessageAsync(IWebsocketConnectionContext<IOcppMessage> connection, IOcppMessage message)
    {
        switch (message)
        {
            case OcppCall call:
                // process the call
                break;
            case OcppCallResult callResult:
                // process the call result
                break;
            case OcppCallError callError:
                // process the call error
                break;
        }

        return Task.CompletedTask;
    }
}