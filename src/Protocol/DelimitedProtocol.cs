using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace SimpleR.Protocol;

/// <summary>
/// Protocol that splits messages by a given delimiter
/// </summary>
public abstract class DelimitedProtocol<TMessage> : IMessageProtocol<TMessage>
{
    private byte[]? _delimiter;
    
    protected abstract byte[] Delimiter { get; }

    internal byte[] CachedDelimiter => _delimiter ??= Delimiter;

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, [NotNullWhen(true)]out TMessage? message)
    {
        var reader = new SequenceReader<byte>(input);
        if (reader.TryReadTo(out ReadOnlySpan<byte> cut, new ReadOnlySpan<byte>(CachedDelimiter), true))
        {
            message = ParseMessage(cut)!;
            input = reader.UnreadSequence;
            return true;
        }

        message = default;
        return false;
    }

    public void WriteMessage(TMessage message, IBufferWriter<byte> output)
    {
        var wrappedOutput = new BufferWriterWithNoOpAdvance(output);
        WriteMessageCore(message, wrappedOutput);
        _delimiter.CopyTo(output.GetSpan(CachedDelimiter.Length).Slice(wrappedOutput.Advanced));
        output.Advance(wrappedOutput.Advanced + CachedDelimiter.Length);
    }

    protected abstract TMessage ParseMessage(ReadOnlySpan<byte> span);
    protected abstract void WriteMessageCore(TMessage message, IBufferWriter<byte> output);

    private class BufferWriterWithNoOpAdvance : IBufferWriter<byte>
    {
        private readonly IBufferWriter<byte> _internalWriter;
        private int _advanced;

        public int Advanced => _advanced;

        public BufferWriterWithNoOpAdvance(IBufferWriter<byte> internalWriter)
        {
            _internalWriter = internalWriter;
        }

        public void Advance(int count)
        {
            _advanced += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => _internalWriter.GetMemory(sizeHint);

        public Span<byte> GetSpan(int sizeHint = 0) => _internalWriter.GetSpan(sizeHint);
    }
}