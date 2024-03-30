using System.Buffers;

namespace SimpleR.Protocol
{
    public interface IDelimitedMessageProtocol<TMessage> : IMessageWriter<TMessage>
    {
        TMessage ParseMessage(ref ReadOnlySequence<byte> input);
    }
}