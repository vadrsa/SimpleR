using System.Buffers;
using SimpleR.Protocol;

namespace SimpleR.Authobahn.Server;

public class EchoProtocol : IDelimitedMessageProtocol<byte[]>
{
    public void WriteMessage(byte[] message, IBufferWriter<byte> output)
    {
        output.Write(message);
    }

    public byte[] ParseMessage(ref ReadOnlySequence<byte> input)
    {
        return input.ToArray();
    }
}