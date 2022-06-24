using System.Buffers;

namespace SimpleR.Protocol.Internal;

internal class CompositeDelimitedMessageProtocol<TMessageIn, TMessageOut> : IDelimitedMessageProtocol<TMessageIn, TMessageOut>
{
    private readonly IDelimitedMessageReader<TMessageIn> _reader;
    private readonly IMessageWriter<TMessageOut> _writer;

    internal CompositeDelimitedMessageProtocol(IDelimitedMessageReader<TMessageIn> reader, IMessageWriter<TMessageOut> writer)
    {
        _reader = reader;
        _writer = writer;
    }

    public void WriteMessage(TMessageOut message, IBufferWriter<byte> output)
    {
        _writer.WriteMessage(message, output);
    }

    public TMessageIn ParseMessage(ref ReadOnlySequence<byte> input)
    {
        return _reader.ParseMessage(ref input);
    }
}