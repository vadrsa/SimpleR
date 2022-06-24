using SimpleR.Protocol.Internal;

namespace SimpleR.Protocol;

public static class MessageProtocol
{
    public static IMessageProtocol<TMessageIn, TMessageOut> From<TMessageIn, TMessageOut>(
        IMessageReader<TMessageIn> reader, IMessageWriter<TMessageOut> writer)
        => new CompositeMessageProtocol<TMessageIn, TMessageOut>(reader, writer);
    
    public static IDelimitedMessageProtocol<TMessageIn, TMessageOut> From<TMessageIn, TMessageOut>(
        IDelimitedMessageReader<TMessageIn> reader, IMessageWriter<TMessageOut> writer)
        => new CompositeDelimitedMessageProtocol<TMessageIn, TMessageOut>(reader, writer);
}