using System.Buffers;
using System.Text;
using SimpleR.Protocol;

namespace PingPongNet8Server.Text;

public class DelimitedTextPingProtocol : IDelimitedMessageProtocol<PingPongText>
{
    public PingPongText ParseMessage(ref ReadOnlySequence<byte> span)
    {
        return new PingPongText { Payload = Encoding.UTF8.GetString(span) };
    }

    public void WriteMessage(PingPongText message, IBufferWriter<byte> output)
    {
        var span = output.GetSpan(Encoding.UTF8.GetByteCount(message.Payload));

        var bytesWritten = Encoding.UTF8.GetBytes(message.Payload,span);

        output.Advance(bytesWritten);
    }
}