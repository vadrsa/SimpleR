using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using SimpleR.Protocol;

namespace PingPongNet6Server.Binary;

public class CustomBinaryPingProtocol : IMessageProtocol<PingPongBinary>
{
    public void WriteMessage(PingPongBinary message, IBufferWriter<byte> output)
    {
        var memory = output.GetMemory(message.Payload.Length);
        
        message.Payload.CopyTo(memory);
        
        output.Advance(message.Payload.Length);
    }

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, out PingPongBinary message)
    {
        var reader = new SequenceReader<byte>(input);

        if (reader.TryReadTo(out ReadOnlySequence<byte> payload, delimiter: 0, advancePastDelimiter: true))
        {
            message = new PingPongBinary { Payload = payload.ToArray() };
            input = reader.UnreadSequence;
            return true;
        }
        
        message = default;
        return false;
    }
}