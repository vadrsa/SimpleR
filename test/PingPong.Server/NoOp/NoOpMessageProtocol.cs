using System.Buffers;
using System.Text;
using SimpleR.Protocol;

namespace PingPong.Server.NoOp;

public class NoOpMessageProtocol : IDelimitedMessageProtocol<EmptyMessage>
{
    public EmptyMessage ParseMessage(ref ReadOnlySequence<byte> span)
    {
        return new EmptyMessage();
    }

    public void WriteMessage(EmptyMessage message, IBufferWriter<byte> output)
    {
    }
}