using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SimpleR.Internal;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to add SimpleR services.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds SimpleR services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddSimpleR(this IServiceCollection services)
    {
        services.AddConnections();
        services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IConfigureOptions<ConnectionOptions>, ConnectionOptionsSetup>());
        services.TryAddSingleton<SimpleRMarkerService>();
        services.TryAddSingleton<WebSocketConnectionDispatcher>();
        services.TryAddSingleton<WebSocketConnectionManager>();

        return services;
    }
}