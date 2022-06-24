using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace SimpleR.Protocol.Internal;

internal class CompositeMessageProtocol<TMessageIn, TMessageOut> : IMessageProtocol<TMessageIn, TMessageOut>
{
    private readonly IMessageReader<TMessageIn> _reader;
    private readonly IMessageWriter<TMessageOut> _writer;

    internal CompositeMessageProtocol(IMessageReader<TMessageIn> reader, IMessageWriter<TMessageOut> writer)
    {
        _reader = reader;
        _writer = writer;
    }
    
    public bool TryParseMessage(ref ReadOnlySequence<byte> input, [NotNullWhen(true)]out TMessageIn message)
    {
        return _reader.TryParseMessage(ref input, out message);
    }

    public void WriteMessage(TMessageOut message, IBufferWriter<byte> output)
    {
        _writer.WriteMessage(message, output);
    }
}