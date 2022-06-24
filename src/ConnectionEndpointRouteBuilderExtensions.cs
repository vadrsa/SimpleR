using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using SimpleR;
using Microsoft.AspNetCore.Routing;
using System;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods on <see cref="IEndpointRouteBuilder"/> that add routes for <see cref="ConnectionHandler"/>s.
/// </summary>
public static class ConnectionEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapWebsocketConnection(this IEndpointRouteBuilder endpoints, string pattern, Action<IConnectionBuilder> configure) =>
            endpoints.MapWebsocketConnection(pattern, new HttpConnectionDispatcherOptions(), configure);

    public static IEndpointConventionBuilder MapWebsocketConnection(this IEndpointRouteBuilder endpoints, string pattern, HttpConnectionDispatcherOptions options, Action<IConnectionBuilder> configure)
    {
        var dispatcher = endpoints.ServiceProvider.GetRequiredService<WebSocketConnectionDispatcher>();

        var connectionBuilder = new ConnectionBuilder(endpoints.ServiceProvider);
        configure(connectionBuilder);
        var connectionDelegate = connectionBuilder.Build();

        // build the execute handler part of the protocol
        var app = endpoints.CreateApplicationBuilder();
        app.UseWebSockets();
        app.Run(c => dispatcher.ExecuteAsync(c, options, connectionDelegate));
        var executeHandler = app.Build();

        return endpoints.Map(pattern, executeHandler);
    }
}
