using System.Buffers;
using System.Text;
using SimpleR.Protocol;

namespace SampleProtocols;

public class EndOfMessageDelimitedSampleProtocolMessage
{
    public EndOfMessageDelimitedSampleProtocolMessage(string content)
    {
        Content = content;
    }

    public string Content { get; }
}

public class EndOfMessageDelimitedSampleProtocol : EndOfMessageDelimitedProtocol<EndOfMessageDelimitedSampleProtocolMessage>
{
    // this sequence of bytes is guaranteed to not occur in the protocol bytes
    protected override byte[] Delimiter { get; } = { 0xFF, 0xFF };

    protected override EndOfMessageDelimitedSampleProtocolMessage ParseMessage(ReadOnlySpan<byte> span)
    {
        return new EndOfMessageDelimitedSampleProtocolMessage(Encoding.UTF8.GetString(span));
    }

    protected override void WriteMessageCore(EndOfMessageDelimitedSampleProtocolMessage message, IBufferWriter<byte> output)
    {
        var bytes = Encoding.UTF8.GetBytes(message.Content);
        var memory = output.GetMemory(bytes.Length);
        bytes.CopyTo(memory);
        output.Advance(bytes.Length);
    }
}