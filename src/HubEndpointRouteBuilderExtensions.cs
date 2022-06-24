using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using SimpleR;

namespace Microsoft.AspNetCore.Builder;

public static class HubEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapWebsocket<THandler>(this IEndpointRouteBuilder endpoints, string pattern)
        where THandler : WebsocketHandler
            => endpoints.MapWebsocket<THandler>(pattern, null);

    public static IEndpointConventionBuilder MapWebsocket<THandler>(this IEndpointRouteBuilder endpoints, string pattern, Action<HttpConnectionDispatcherOptions>? optionsConfigurator)
        where THandler : WebsocketHandler
    {
        var options = new HttpConnectionDispatcherOptions();
        optionsConfigurator?.Invoke(options);

        return endpoints.MapWebsocketConnection(pattern, options, b => b.UseWebsocketHandler<THandler>());
    }

    public static IConnectionBuilder UseWebsocketHandler<THandler>(this IConnectionBuilder connectionBuilder)
        where THandler : WebsocketHandler
        => connectionBuilder.UseConnectionHandler<WebSocketConnectionHandler<THandler>>();
}
