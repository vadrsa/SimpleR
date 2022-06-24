using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SimpleR;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddWebsocketServer(this IServiceCollection services)
    {
        services.AddRouting();
        services.AddAuthorization();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ConnectionOptions>, ConnectionOptionsSetup>());
        services.TryAddSingleton<WebSocketConnectionDispatcher>();
        services.TryAddSingleton<WebSocketConnectionManager>();
        return services;
    }

    public static IServiceCollection AddWebsocketHandler<THandler>(this IServiceCollection services)
        where THandler : WebsocketHandler
        => services.AddTransient<THandler>();
}
