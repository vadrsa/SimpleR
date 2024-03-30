using System.Buffers;
using System.Text;
using SimpleR.Protocol;

namespace PingPong.Server.Text;

public class CustomTextPingProtocol : IMessageProtocol<PingPongText>
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

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, out PingPongText message)
    {
        var reader = new SequenceReader<byte>(input);

        if (reader.TryReadTo(out ReadOnlySequence<byte> payload, delimiter: 0, advancePastDelimiter: true))
        {
            message = new PingPongText { Payload = Encoding.UTF8.GetString(payload) };
            input = reader.UnreadSequence;
            return true;
        }
        
        message = default;
        return false;
    }
}