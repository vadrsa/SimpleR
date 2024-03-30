using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace SimpleR.Protocol.Internal
{
    public class EndOfMessageDelimitedProtocol<TMessage> : IMessageProtocol<TMessage>
    {
        private readonly IDelimitedMessageProtocol<TMessage> _innerProtocol;
        private readonly FrameReader _frameReader;

        public EndOfMessageDelimitedProtocol(IDelimitedMessageProtocol<TMessage> innerProtocol)
        {
            _innerProtocol = innerProtocol;
            _frameReader = new FrameReader();
        }
        
        public bool TryParseMessage(ref ReadOnlySequence<byte> input, [NotNullWhen(true)]out TMessage message)
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
                    return true;
                }
            }

            message = default;
            return false;
        }

        public void WriteMessage(TMessage message, IBufferWriter<byte> output)
        {
            var frameWriter = new FrameBufferWriter(output);
            _innerProtocol.WriteMessage(message, frameWriter);
            frameWriter.FinishLastFrame(true);
        }
    }
}