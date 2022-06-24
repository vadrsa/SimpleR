using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SimpleR.Protocol;

namespace SampleProtocols;

public class LengthPrefixedProtocolMessage
{
    public LengthPrefixedProtocolMessage(string content)
    {
        Content = content;
    }

    public string Content { get; }
}

public class LengthPrefixedProtocol : IMessageProtocol<LengthPrefixedProtocolMessage>
{
    public TransferFormat Format => TransferFormat.Text;
    
    public bool TryParseMessage(ref ReadOnlySequence<byte> input, [NotNullWhen(true)]out LengthPrefixedProtocolMessage message)
    {
        var length = (int)input.FirstSpan[0];
        input = input.Slice(1, input.Length - 1);
        
        if (input.Length <= length)
        {
            message = new LengthPrefixedProtocolMessage(Encoding.UTF8.GetString(input.Slice(0, length)));
            input = input.Slice(length);
            return true;
        }
        
        message = null;
        return false;
    }

    public void WriteMessage(LengthPrefixedProtocolMessage message, IBufferWriter<byte> output)
    {
        var span = output.GetSpan(1);
        var bytes = Encoding.UTF8.GetBytes(message.Content);
        if (bytes.Length > byte.MaxValue)
        {
            throw new NotSupportedException();
        }
        var length = (byte)bytes.Length;
        span[0] = length;
        output.Advance(1);
        span = output.GetSpan(length);
        bytes.CopyTo(span);
        output.Advance(length);
    }

}