using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace SimpleR.Protocol.Internal
{
    public class EndOfMessageDelimitedProtocol<TMessageIn, TMessageOut> : IMessageProtocol<TMessageIn, TMessageOut>
    {
        private readonly IDelimitedMessageProtocol<TMessageIn, TMessageOut> _innerProtocol;
        private readonly FrameReader _frameReader;

        public EndOfMessageDelimitedProtocol(IDelimitedMessageProtocol<TMessageIn, TMessageOut> innerProtocol)
        {
            _innerProtocol = innerProtocol;
            _frameReader = new FrameReader();
        }
        
        public bool TryParseMessage(ref ReadOnlySequence<byte> input, [NotNullWhen(true)]out TMessageIn message)
        {
            var messageSequenceBuilder = new ReadOnlySequenceBuilder<byte>();
            var currentInput = input;
            while (_frameReader.ReadFrame(ref currentInput, out var packet, out var isEndOfMessage))
            {
                messageSequenceBuilder.Append(packet);
                if (isEndOfMessage)
                {
                    input = currentInput;
                    var messageSequence = messageSequenceBuilder.Build();
                    message = _innerProtocol.ParseMessage(ref messageSequence);
#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
                    return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
                }
            }

            message = default!;
            return false;
        }

        public void WriteMessage(TMessageOut message, IBufferWriter<byte> output)
        {
            var frameWriter = new FrameBufferWriter(output);
            _innerProtocol.WriteMessage(message, frameWriter);
            frameWriter.FinishLastFrame(true);
        }
    }
}