using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SimpleR;
using SimpleR.Internal;
using SimpleR.Protocol;

namespace Microsoft.AspNetCore.Builder;

public static class HubEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapWebsocket<TDispatcher, TMessage>(this IEndpointRouteBuilder endpoints, string pattern)
        where TDispatcher : IWebSocketMessageDispatcher<TMessage>
            => endpoints.MapWebsocket<TDispatcher, TMessage>(pattern, null);

    public static IEndpointConventionBuilder MapWebsocket<TDispatcher, TMessage>(this IEndpointRouteBuilder endpoints, string pattern, Action<WebSocketConnectionDispatcherOptions>? optionsConfigurator)
        where TDispatcher : IWebSocketMessageDispatcher<TMessage>
    {
        var options = new WebSocketConnectionDispatcherOptions();
        optionsConfigurator?.Invoke(options);

        var messageProtocol = endpoints.ServiceProvider.GetRequiredService<IMessageProtocol<TMessage>>();
        if (messageProtocol is EndOfMessageDelimitedProtocol<TMessage> endOfMessageDelimitedProtocol)
        {
            options.WebSockets.EndOfMessageBytes = endOfMessageDelimitedProtocol.CachedDelimiter;
        }
        var conventionBuilder = endpoints.MapWebsocketConnection(pattern, options, b => b.UseWebsocketHandler<TDispatcher, TMessage>());
        
        var attributes = typeof(TDispatcher).GetCustomAttributes(inherit: true);
        conventionBuilder.Add(e =>
        {
            // Add all attributes on the Hub as metadata (this will allow for things like)
            // auth attributes and cors attributes to work seamlessly
            foreach (var item in attributes)
            {
                e.Metadata.Add(item);
            }
            // TODO: add
            // Add metadata that captures the hub type this endpoint is associated with
            //e.Metadata.Add(new HubMetadata(typeof(THandler)));
        });

        return conventionBuilder;
    }

    public static IConnectionBuilder UseWebsocketHandler<TDispatcher, TMessage>(this IConnectionBuilder connectionBuilder)
        where TDispatcher : IWebSocketMessageDispatcher<TMessage>
        => connectionBuilder.UseConnectionHandler<WebSocketConnectionHandler<TDispatcher, TMessage>>();
}
