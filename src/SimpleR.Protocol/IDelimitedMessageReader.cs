using System.Buffers;

namespace SimpleR.Protocol;

public interface IDelimitedMessageReader<out TMessageIn>
{
    TMessageIn ParseMessage(ref ReadOnlySequence<byte> input);
}