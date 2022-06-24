using System.Diagnostics.CodeAnalysis;
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
    public static IEndpointConventionBuilder MapSimpleR<TMessage>(this IEndpointRouteBuilder endpoints,
#if NET7_0_OR_GREATER
        [StringSyntax("Route")]
#endif
        string pattern, Action<MessageDispatcherBuilder<TMessage>> build)
        => endpoints.MapSimpleR<TMessage>(pattern, build, null);

    /// <summary>
    /// Maps incoming requests with the specified path to the specified handler
    /// </summary>
    /// <typeparam name="TMessageIn">The type of input message.</typeparam>
    /// <typeparam name="TMessageOut">The type of output message.</typeparam>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="build">The action that builds the SimpleR endpoint.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with the connections.</returns>
    public static IEndpointConventionBuilder MapSimpleR<TMessageIn, TMessageOut>(this IEndpointRouteBuilder endpoints, 
#if NET7_0_OR_GREATER
        [StringSyntax("Route")]
#endif
        string pattern, Action<MessageDispatcherBuilder<TMessageIn, TMessageOut>> build)
        => endpoints.MapSimpleR<TMessageIn, TMessageOut>(pattern, build, null);

    /// <summary>
    /// Maps incoming requests with the specified path to the specified handler
    /// </summary>
    /// <typeparam name="TMessageIn">The type of input message.</typeparam>
    /// <typeparam name="TMessageOut">The type of output message.</typeparam>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="build">The action that builds the SimpleR endpoint.</param>
    /// <param name="configureOptions">A callback to configure dispatcher options.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with the connections.</returns>
    public static IEndpointConventionBuilder MapSimpleR<TMessageIn, TMessageOut>(this IEndpointRouteBuilder endpoints, 
#if NET7_0_OR_GREATER
        [StringSyntax("Route")]
#endif
        string pattern, Action<MessageDispatcherBuilder<TMessageIn, TMessageOut>> build, Action<WebSocketConnectionDispatcherOptions>? configureOptions)
    {
        return endpoints.MapSimpleRInternal(pattern, () =>
        {
            var builder = new MessageDispatcherBuilder<TMessageIn, TMessageOut>();
            build(builder);
            return builder;
        }, configureOptions);
    }

    /// <summary>
    /// Maps incoming requests with the specified path to the specified handler
    /// </summary>
    private static IEndpointConventionBuilder MapSimpleRInternal<TMessageIn, TMessageOut>(this IEndpointRouteBuilder endpoints, string pattern, Func<MessageDispatcherBuilder<TMessageIn, TMessageOut>> build, Action<WebSocketConnectionDispatcherOptions>? configureOptions)
    {
        var marker = endpoints.ServiceProvider.GetService<SimpleRMarkerService>();

        if (marker == null)
        {
            throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
                                                "'IServiceCollection.AddSimpleR' inside the call to 'ConfigureServices(...)' in the application startup code.");
        }
        
        var builder = build.Invoke();
        builder.Validate();
        var dispatcher = builder.DispatcherFactory!(endpoints.ServiceProvider);
        
        var options = new WebSocketConnectionDispatcherOptions();
        configureOptions?.Invoke(options);
        options.WebSockets.FramePackets = builder.IsEndOfMessageDelimited;

        var conventionBuilder = endpoints.MapWebsocketConnection(pattern, options,
            b =>
            {
                var sp = b.ApplicationServices;
                var handler = ActivatorUtilities.CreateInstance<WebSocketConnectionHandler<TMessageIn, TMessageOut>>(sp,
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

    
    /// <summary>
    /// Maps incoming requests with the specified path to the specified handler
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="build">The action that builds the SimpleR endpoint.</param>
    /// <param name="configureOptions">A callback to configure dispatcher options.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with the connections.</returns>
    public static IEndpointConventionBuilder MapSimpleR<TMessage>(this IEndpointRouteBuilder endpoints, 
#if NET7_0_OR_GREATER
        [StringSyntax("Route")]
#endif
        string pattern, Action<MessageDispatcherBuilder<TMessage>> build, Action<WebSocketConnectionDispatcherOptions>? configureOptions)
    {
        return endpoints.MapSimpleRInternal(pattern, () =>
        {
            var builder = new MessageDispatcherBuilder<TMessage>();
            build(builder);
            return builder;
        }, configureOptions);
    }

    private static IConnectionBuilder UseWebsocketHandler<TMessageIn, TMessageOut>(this IConnectionBuilder connectionBuilder, WebSocketConnectionHandler<TMessageIn, TMessageOut> handler)
        => connectionBuilder.Run(handler.OnConnectedAsync);
}