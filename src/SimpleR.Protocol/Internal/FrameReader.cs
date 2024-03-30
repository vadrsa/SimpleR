using System;
using System.Buffers;
using System.Buffers.Binary;

namespace SimpleR.Protocol.Internal
{
    public class FrameReader
    {
        /// <summary>
        /// If a frame exists in the input,
        /// reads the first one and puts it into the <see cref="frame"/>,
        /// <see cref="isEndOfMessage"/> flag is set for the frame
        /// <see cref="input"/> is set to the unread tail
        /// returns true
        /// If a frame doesn't exist,
        /// returns false
        /// </summary>
        public bool ReadFrame(ref ReadOnlySequence<byte> input, out ReadOnlySequence<byte> frame, out bool isEndOfMessage)
        {
            // Check if the input length is less than or equal to the length of an integer
            if (input.Length <= FrameHelpers.IntegerLengthEncodedByteCount)
            {
                frame = default;
                isEndOfMessage = false;
                return false;
            }

            // Get the length of the frame
            var length = GetLength(input);

            // Check if the input length is less than the length of the frame plus 1
            if (input.Length < length + 1)
            {
                frame = default;
                isEndOfMessage = false;
                return false;
            }

            // Create a ReadOnlySequenceBuilder to build the frame
            var sequenceBuilder = new ReadOnlySequenceBuilder<byte>();

            // Slice the input to get the packet
            var packet = input.Slice(FrameHelpers.IntegerLengthEncodedByteCount, length);

            // Append the packet to the sequence builder
            sequenceBuilder.Append(packet);

            // Read the byte that indicates if the frame is the end of a message
            var endOfMessageByte = input.Slice(FrameHelpers.IntegerLengthEncodedByteCount + length, 1).FirstSpan[0];
            isEndOfMessage = endOfMessageByte == FrameHelpers.IsEndOfMessageByte;

            // Build the frame
            frame = sequenceBuilder.Build();

            // Slice the input to remove the frame and the end of message byte
            input = input.Slice(input.GetPosition(FrameHelpers.IntegerLengthEncodedByteCount + length + 1));

            return true;
        }

        /// <summary>
        /// Reads an integer from the start of a ReadOnlySequence<byte>.
        /// If the sequence is a single segment, it reads the integer directly from the first span.
        /// If the sequence is multi-segment, it copies the sequence to a span and reads the integer from the span.
        /// </summary>
        private int GetLength(ReadOnlySequence<byte> sequence)
        {
            var lengthSlice = sequence.Slice(0, FrameHelpers.IntegerLengthEncodedByteCount);
            if (lengthSlice.IsSingleSegment)
            {
                return BinaryPrimitives.ReadInt32BigEndian(lengthSlice.FirstSpan);
            }

            // Allocate a span on the stack and copy the sequence to it
            Span<byte> buffer = stackalloc byte[FrameHelpers.IntegerLengthEncodedByteCount];
            lengthSlice.CopyTo(buffer);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }
    }
}