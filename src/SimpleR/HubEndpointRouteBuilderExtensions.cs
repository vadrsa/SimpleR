using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Routing;
using SimpleR;
using SimpleR.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

public static class HubEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps incoming requests with the specified path to the specified handler
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="build">The action that builds the SimpleR endpoint.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with the connections.</returns>
    public static IEndpointConventionBuilder MapSimpleR<TMessage>(this IEndpointRouteBuilder endpoints, string pattern, Action<MessageDispatcherBuilder<TMessage>> build)
        => endpoints.MapSimpleR<TMessage>(pattern, build, null);

    /// <summary>
    /// Maps incoming requests with the specified path to the specified handler
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="build">The action that builds the SimpleR endpoint.</param>
    /// <param name="configureOptions">A callback to configure dispatcher options.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with the connections.</returns>
    public static IEndpointConventionBuilder MapSimpleR<TMessage>(this IEndpointRouteBuilder endpoints, string pattern, Action<MessageDispatcherBuilder<TMessage>> build, Action<WebSocketConnectionDispatcherOptions>? configureOptions)
    {
        var marker = endpoints.ServiceProvider.GetService<SimpleRMarkerService>();

        if (marker == null)
        {
            throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
                                                "'IServiceCollection.AddSimpleR' inside the call to 'ConfigureServices(...)' in the application startup code.");
        }
        
        var builder = new MessageDispatcherBuilder<TMessage>();
        build.Invoke(builder);
        builder.Validate();
        var dispatcher = builder.DispatcherFactory!(endpoints.ServiceProvider);
        
        var options = new WebSocketConnectionDispatcherOptions();
        configureOptions?.Invoke(options);
        options.WebSockets.FramePackets = builder.IsEndOfMessageDelimited;

        var conventionBuilder = endpoints.MapWebsocketConnection(pattern, options,
            b =>
            {
                var sp = b.ApplicationServices;
                var handler = ActivatorUtilities.CreateInstance<WebSocketConnectionHandler<TMessage>>(sp,
                    dispatcher, builder.Protocol!);
                b.UseWebsocketHandler(handler);
            });
        
        var attributes = dispatcher.GetType().GetCustomAttributes(inherit: true);
        conventionBuilder.Add(e =>
        {
            // Add all attributes on the Hub as metadata (this will allow for things like)
            // auth attributes and cors attributes to work seamlessly
            foreach (var item in attributes)
            {
                e.Metadata.Add(item);
            }
        });

        return conventionBuilder;
    }

    private static IConnectionBuilder UseWebsocketHandler<TMessage>(this IConnectionBuilder connectionBuilder, WebSocketConnectionHandler<TMessage> handler)
        => connectionBuilder.Run(handler.OnConnectedAsync);
}