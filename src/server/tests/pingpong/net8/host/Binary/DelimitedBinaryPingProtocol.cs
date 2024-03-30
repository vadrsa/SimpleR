using System.Buffers;
using SimpleR.Protocol;

namespace PingPongNet8Server.Binary;

public class DelimitedBinaryPingProtocol : IDelimitedMessageProtocol<PingPongBinary>
{
    public PingPongBinary ParseMessage(ref ReadOnlySequence<byte> span)
    {
        return new PingPongBinary { Payload = span.ToArray() };
    }

    public void WriteMessage(PingPongBinary message, IBufferWriter<byte> output)
    {
        var memory = output.GetMemory(message.Payload.Length);
        
        message.Payload.CopyTo(memory);
        
        output.Advance(message.Payload.Length);
    }
}