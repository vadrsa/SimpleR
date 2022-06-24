using System.Buffers;

namespace SimpleR.Protocol
{
    public interface IMessageWriter<in TMessage>
    {
        void WriteMessage(TMessage message, IBufferWriter<byte> output);
    }
}