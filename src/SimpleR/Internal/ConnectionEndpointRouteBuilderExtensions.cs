using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using SimpleR;
using Microsoft.AspNetCore.Routing;
using SimpleR.Internal;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods on <see cref="IEndpointRouteBuilder"/> that add routes for <see cref="ConnectionHandler"/>s.
/// </summary>
internal static class ConnectionEndpointRouteBuilderExtensions
{
    internal static IEndpointConventionBuilder MapWebsocketConnection(this IEndpointRouteBuilder endpoints, string pattern, Action<IConnectionBuilder> configure) =>
        endpoints.MapWebsocketConnection(pattern, new WebSocketConnectionDispatcherOptions(), configure);

    internal static IEndpointConventionBuilder MapWebsocketConnection(this IEndpointRouteBuilder endpoints, string pattern, WebSocketConnectionDispatcherOptions options, Action<IConnectionBuilder> configure)
    {
        var dispatcher = endpoints.ServiceProvider.GetRequiredService<WebSocketConnectionDispatcher>();

        var connectionBuilder = new ConnectionBuilder(endpoints.ServiceProvider);
        configure(connectionBuilder);
        var connectionDelegate = connectionBuilder.Build();

        var conventionBuilders = new List<IEndpointConventionBuilder>();
        
        // build the execute handler part of the protocol
        var app = endpoints.CreateApplicationBuilder();

        var webSocketOptions = new WebSocketOptions();
        if (options.WebSockets.KeepAliveInterval is not null)
            webSocketOptions.KeepAliveInterval = (TimeSpan)options.WebSockets.KeepAliveInterval;

        if (options.WebSockets.KeepAliveTimeout is not null)
            webSocketOptions.KeepAliveTimeout = (TimeSpan)options.WebSockets.KeepAliveTimeout;

        app.UseWebSockets(webSocketOptions);
        app.Run(c => dispatcher.ExecuteAsync(c, options, connectionDelegate));
        var executeHandler = app.Build();

        
        var executeBuilder = endpoints.Map(pattern, executeHandler);
        conventionBuilders.Add(executeBuilder);

        var compositeConventionBuilder = new CompositeEndpointConventionBuilder(conventionBuilders);

        // Add metadata to all of Endpoints
        compositeConventionBuilder.Add(e =>
        {
            // Add the authorization data as metadata
            foreach (var data in options.AuthorizationData)
            {
                e.Metadata.Add(data);
            }
        });


        return compositeConventionBuilder;
    }
    
#if NET7_0_OR_GREATER
    private sealed class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly List<IEndpointConventionBuilder> _endpointConventionBuilders;

        public CompositeEndpointConventionBuilder(List<IEndpointConventionBuilder> endpointConventionBuilders)
        {
            _endpointConventionBuilders = endpointConventionBuilders;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            foreach (var endpointConventionBuilder in _endpointConventionBuilders)
            {
                endpointConventionBuilder.Add(convention);
            }
        }

        public void Finally(Action<EndpointBuilder> finalConvention)
        {
            foreach (var endpointConventionBuilder in _endpointConventionBuilders)
            {
                endpointConventionBuilder.Finally(finalConvention);
            }
        }
    }
#else
    private class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly List<IEndpointConventionBuilder> _endpointConventionBuilders;

        public CompositeEndpointConventionBuilder(List<IEndpointConventionBuilder> endpointConventionBuilders)
        {
            _endpointConventionBuilders = endpointConventionBuilders;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            foreach (var endpointConventionBuilder in _endpointConventionBuilders)
            {
                endpointConventionBuilder.Add(convention);
            }
        }
    }
#endif
}