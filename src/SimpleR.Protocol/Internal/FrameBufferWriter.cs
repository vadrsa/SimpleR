using System;
using System.Buffers;
using System.Buffers.Binary;

namespace SimpleR.Protocol.Internal
{
    /// <summary>
    /// FrameBufferWriter is an implementation of the IBufferWriter&lt;byte&gt; interface.
    /// This class is used to write data into a buffer in a frame-based format,
    /// where each frame has a length and an end-of-message marker.
    /// </summary>
    public class FrameBufferWriter : IBufferWriter<byte>
    {
        // Internal buffer writer
        private readonly IBufferWriter<byte> _internalWriter;
        // Flag to track if it's the first request to the buffer
        private bool _firstBufferRequest = true;
        // Memory slice to keep track of the length of the data being written
        private Memory<byte>? _lengthSlice;
        // Memory slice to keep track of the last end of message slice
        private Memory<byte>? _lastEndOfMessageSlice;

        /// <summary>
        /// Constructor that initializes the internal buffer writer
        /// </summary>
        /// <param name="internalWriter">The internal buffer writer</param>
        public FrameBufferWriter(IBufferWriter<byte> internalWriter)
        {
            _internalWriter = internalWriter ?? throw new ArgumentNullException(nameof(internalWriter));
        }

        /// <summary>
        /// Method to finish writing a frame and prepare for the next one
        /// </summary>
        /// <param name="count">The number of bytes written</param>
        public void Advance(int count)
        {
            FinishFrame(count);

            _firstBufferRequest = true;
            _lengthSlice = null;
        }

        /// <summary>
        /// Private method to finish writing a frame
        /// </summary>
        /// <param name="count">The number of bytes written</param>
        private void FinishFrame(int count)
        {
            // nothing written
            if (!_lengthSlice.HasValue)
            {
                if (count == 0)
                {
                    FinishEmptyFrame();
                }
                return;
            }

            var advance = FinishLastFrameInternal(false);

            BinaryPrimitives.WriteInt32BigEndian(_lengthSlice.Value.Span, count);
            _internalWriter.Advance(count + FrameHelpers.IntegerLengthEncodedByteCount + advance);
            _lastEndOfMessageSlice = _internalWriter.GetMemory(1);
        }
        
        private void FinishEmptyFrame()
        {
            var advance = FinishLastFrameInternal(false);
            // write length
            var slice = _internalWriter.GetSpan(FrameHelpers.IntegerLengthEncodedByteCount);
            BinaryPrimitives.WriteInt32BigEndian(slice, 0);
            _internalWriter.Advance(FrameHelpers.IntegerLengthEncodedByteCount + advance);
            _lastEndOfMessageSlice = _internalWriter.GetMemory(1);
        }

        /// <summary>
        /// Private method to mark the end of a message frame
        /// </summary>
        /// <param name="isEndOfMessage">Flag to indicate if it's the end of the message</param>
        /// <returns>The number of bytes to advance</returns>
        private int FinishLastFrameInternal(bool isEndOfMessage)
        {
            if (_lastEndOfMessageSlice == null)
            {
                return 0;
            }

            MarkEndOfMessage(_lastEndOfMessageSlice.Value, isEndOfMessage);
            return 1;
        }

        /// <summary>
        /// Method to mark the end of a message frame
        /// </summary>
        /// <param name="isEndOfMessage">Flag to indicate if it's the end of the message</param>
        public void FinishLastFrame(bool isEndOfMessage)
        {
            if (_lastEndOfMessageSlice == null)
            {
                return;
            }

            MarkEndOfMessage(_lastEndOfMessageSlice.Value, isEndOfMessage);
            _internalWriter.Advance(1);
        }

        /// <summary>
        /// Marks the end of a message in the buffer
        /// </summary>
        /// <param name="memory">The memory slice to mark</param>
        /// <param name="isEndOfMessage">Flag to indicate if it's the end of the message</param>
        private static void MarkEndOfMessage(Memory<byte> memory, bool isEndOfMessage)
        {
            memory.Span[0] = isEndOfMessage ? FrameHelpers.IsEndOfMessageByte : FrameHelpers.IsNotEndOfMessageByte;
        }

        /// <summary>
        /// Method to get a memory for writing data
        /// </summary>
        /// <param name="sizeHint">The minimum length of the returned memory</param>
        /// <returns>A memory for writing data</returns>
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var (buffer, startFrom) = GetBuffer(sizeHint);
            return buffer.Slice(FrameHelpers.IntegerLengthEncodedByteCount+startFrom);
        }

        /// <summary>
        /// Method to get a span for writing data
        /// </summary>
        /// <param name="sizeHint">The minimum length of the returned span</param>
        /// <returns>A span for writing data</returns>
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var (buffer, startFrom) = GetBuffer(sizeHint);
            return buffer.Slice(FrameHelpers.IntegerLengthEncodedByteCount+startFrom).Span;
        }

        /// <summary>
        /// Private method to get a buffer for writing data
        /// </summary>
        /// <param name="sizeHint">The minimum length of the returned buffer</param>
        /// <returns>A tuple containing the buffer and the starting index</returns>
        private (Memory<byte> buffer, int startFrom) GetBuffer(int sizeHint = 0)
        {
            if (!_firstBufferRequest)
                throw new NotSupportedException("Multiple buffer requests per frame is not supported");
            
            sizeHint = AdjustSizeHint(sizeHint);
            var realBuffer = _internalWriter.GetMemory(sizeHint);

            var startFrom = _lastEndOfMessageSlice == null ? 0 : 1;

            _firstBufferRequest = false;
            _lengthSlice = realBuffer.Slice(startFrom, FrameHelpers.IntegerLengthEncodedByteCount);
            return (realBuffer, startFrom);
        }

        /// <summary>
        /// Private method to adjust the size hint
        /// </summary>
        /// <param name="sizeHint">The original size hint</param>
        /// <returns>The adjusted size hint</returns>
        private int AdjustSizeHint(int sizeHint)
        {
            if (sizeHint != 0 && sizeHint < FrameHelpers.IntegerLengthEncodedByteCount)
            {
                sizeHint = FrameHelpers.IntegerLengthEncodedByteCount + 1;
            }
            return sizeHint;
        }
    }
}
