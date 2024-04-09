using System.Buffers;
using System.Text;
using SimpleR.Protocol;

namespace PingPong.Server.NoOpAuth;

public class NoOpAuthMessageProtocol : IDelimitedMessageProtocol<EmptyMessage>
{
    public EmptyMessage ParseMessage(ref ReadOnlySequence<byte> span)
    {
        return new EmptyMessage();
    }

    public void WriteMessage(EmptyMessage message, IBufferWriter<byte> output)
    {
    }
}