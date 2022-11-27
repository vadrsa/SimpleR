namespace SimpleR.Protocol;

public interface IMessageProtocol<TMessage> : IMessageReader<TMessage>, IMessageWriter<TMessage>
{
}