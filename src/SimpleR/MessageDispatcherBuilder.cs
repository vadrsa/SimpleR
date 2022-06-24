using Microsoft.Extensions.DependencyInjection;
using SimpleR.Protocol;
using SimpleR.Protocol.Internal;

namespace SimpleR;

public class MessageDispatcherBuilder<TMessage> : MessageDispatcherBuilder<TMessage, TMessage>;

public class MessageDispatcherBuilder<TMessageIn, TMessageOut>
{
    internal bool IsEndOfMessageDelimited { get; private set; }
    internal IMessageProtocol<TMessageIn, TMessageOut>? Protocol { get; private set; }
    internal Func<IServiceProvider, IWebSocketMessageDispatcher<TMessageIn, TMessageOut>>? DispatcherFactory { get; private set; }

    internal void Validate()
    {
        if (Protocol == null)
        {
            throw new InvalidOperationException("Protocol must be set using either of the .UseEndOfMessageDelimitedProtocol or .UseCustomProtocol methods.");
        }
        if (DispatcherFactory == null)
        {
            throw new InvalidOperationException("Dispatcher must be set using the .UseDispatcher method.");
        }
    }
    
    /// <summary>
    /// Use a protocol that delimits messages using the EndOfMessage delimiter.
    /// </summary>
    /// <param name="protocol">The delimited message protocol.</param>
    /// <returns>The builder.</returns>
    public MessageDispatcherBuilder<TMessageIn, TMessageOut> UseEndOfMessageDelimitedProtocol(IDelimitedMessageProtocol<TMessageIn, TMessageOut> protocol)
    {
        IsEndOfMessageDelimited = true;
        Protocol = new EndOfMessageDelimitedProtocol<TMessageIn, TMessageOut>(protocol);
        return this;
    }
    
    /// <summary>
    /// Use a custom message protocol.
    /// </summary>
    /// <param name="protocol">The message protocol.</param>
    /// <returns>The builder.</returns>
    public MessageDispatcherBuilder<TMessageIn, TMessageOut> UseCustomProtocol(IMessageProtocol<TMessageIn, TMessageOut> protocol)
    {
        IsEndOfMessageDelimited = false;
        Protocol = protocol;
        return this;
    }
    /// <summary>
    /// Use a dispatcher for handling messages.
    /// </summary>
    /// <param name="dispatcher">The WebSocket message dispatcher.</param>
    /// <returns>The builder.</returns>

    public MessageDispatcherBuilder<TMessageIn, TMessageOut> UseDispatcher(IWebSocketMessageDispatcher<TMessageIn, TMessageOut> dispatcher)
    {
        DispatcherFactory = _ => dispatcher;
        return this;
    }
    
    /// <summary>
    /// Use a dispatcher for handling messages.
    /// </summary>
    /// <typeparam name="TDispatcher">The type of the dispatcher.</typeparam>
    /// <returns>The builder.</returns>
    public MessageDispatcherBuilder<TMessageIn, TMessageOut> UseDispatcher<TDispatcher>()
        where TDispatcher : IWebSocketMessageDispatcher<TMessageIn, TMessageOut>
    {
        DispatcherFactory = sp => (IWebSocketMessageDispatcher<TMessageIn, TMessageOut>)ActivatorUtilities.CreateInstance(sp, typeof(TDispatcher));
        return this;
    }
}