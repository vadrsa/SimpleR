using SimpleR.Protocol;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace SimpleR.Tests.Mocks;

internal class MessageProtocolMock<TMessageIn, TMessageOut> : IMessageProtocol<TMessageIn, TMessageOut>
{
    private readonly TMessageIn _mockMessage;
    private readonly Exception? _exception;

    public MessageProtocolMock(TMessageIn mockMessage, Exception? exception)
    {
        _mockMessage = mockMessage;
        _exception = exception;
    }

    public bool TryParseMessage(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out TMessageIn message)
    {
        if (buffer.IsEmpty)
        {
            message = default!;
            return false;
        }

        buffer = buffer.Slice(buffer.Length);

        if (_exception is not null)
        {
            throw _exception;
        }

        message = _mockMessage;
#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
        return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
    }

    public void WriteMessage(TMessageOut message, IBufferWriter<byte> output)
    {
        throw new NotImplementedException();
    }
}