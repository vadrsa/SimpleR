using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing;
using SimpleR;
using SimpleR.Ocpp;

namespace Microsoft.AspNetCore.Builder;

public static class OcppEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps incoming requests with the specified path to the specified OCPP handler
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="build">The action that builds the SimpleR endpoint.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with the connections.</returns>
    public static IEndpointConventionBuilder MapSimpleRToOcpp(this IEndpointRouteBuilder endpoints,
#if NET7_0_OR_GREATER
        [StringSyntax("Route")]
#endif
        string pattern, Action<MessageDispatcherBuilder<IOcppMessage>> build)
        => endpoints.MapSimpleR(pattern, build);

    /// <summary>
    /// Maps incoming requests with the specified path to the specified OCPP handler
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="build">The action that builds the SimpleR endpoint.</param>
    /// <param name="configureOptions">A callback to configure dispatcher options.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for endpoints associated with the connections.</returns>
    public static IEndpointConventionBuilder MapSimpleRToOcpp(this IEndpointRouteBuilder endpoints, 
#if NET7_0_OR_GREATER
        [StringSyntax("Route")]
#endif
        string pattern, Action<MessageDispatcherBuilder<IOcppMessage>> build, Action<WebSocketConnectionDispatcherOptions>? configureOptions)
        => endpoints.MapSimpleR(pattern, build, configureOptions);
}