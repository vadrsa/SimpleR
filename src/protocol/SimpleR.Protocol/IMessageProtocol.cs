namespace SimpleR.Protocol
{
    public interface IMessageProtocol<TMessage> : IMessageReader<TMessage>, IMessageWriter<TMessage>
    {
        TransferFormat Format { get; }
    }
}
