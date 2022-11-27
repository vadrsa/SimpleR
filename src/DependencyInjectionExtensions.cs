using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SimpleR;
using SimpleR.Internal;
using SimpleR.Protocol;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddWebsocketServer<TDispatcher, TMessage, TProtocol>(this IServiceCollection services)
        where TDispatcher : class, IWebSocketMessageDispatcher<TMessage>
        where TProtocol : class, IMessageProtocol<TMessage>
    {
        services.AddRouting();
        services.AddAuthorization();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ConnectionOptions>, ConnectionOptionsSetup>());
        services.TryAddSingleton<WebSocketConnectionDispatcher>();
        services.TryAddSingleton<WebSocketConnectionManager>();
        
        return services.AddSingleton<IWebSocketMessageDispatcher<TMessage>, TDispatcher>()
            .AddSingleton<IMessageProtocol<TMessage>, TProtocol>()
            .AddSingleton<IMessageReader<TMessage>>(sp => sp.GetRequiredService<IMessageProtocol<TMessage>>())
            .AddSingleton<IMessageWriter<TMessage>>(sp => sp.GetRequiredService<IMessageProtocol<TMessage>>());
    }
}
