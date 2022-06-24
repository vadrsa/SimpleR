namespace SimpleR.Protocol
{
    public interface IMessageProtocol<TMessageIn, in TMessageOut> : IMessageReader<TMessageIn>, IMessageWriter<TMessageOut>;

    public interface IMessageProtocol<TMessage> : IMessageProtocol<TMessage, TMessage>;
}
