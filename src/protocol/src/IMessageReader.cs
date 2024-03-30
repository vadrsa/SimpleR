using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace SimpleR.Protocol
{
    public interface IMessageReader<TMessage>
    {
        bool TryParseMessage(ref ReadOnlySequence<byte> input, [NotNullWhen(true)]out TMessage message);
    }
}