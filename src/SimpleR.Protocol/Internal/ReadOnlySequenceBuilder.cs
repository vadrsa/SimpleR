using System;
using System.Buffers;

namespace SimpleR.Protocol.Internal
{
    public class ReadOnlySequenceBuilder<T>
    {
        private MemorySegment<T>? _firstSegment;
        private MemorySegment<T>? _lastSegment;

        public void Append(ReadOnlySequence<T> sequence)
        {
            foreach (var memory in sequence)
            {
                Append(memory);
            }
        }
        
        public void Append(ReadOnlyMemory<T> memory)
        {
            var segment = new MemorySegment<T>(memory);

            if (_firstSegment == null)
            {
                _firstSegment = _lastSegment = segment;
            }
            else
            {
                _lastSegment = _lastSegment!.Append(segment.Memory);
            }
        }

        public ReadOnlySequence<T> Build()
        {
            if (_firstSegment == null)
            {
                return ReadOnlySequence<T>.Empty;
            }
            
            return new ReadOnlySequence<T>(_firstSegment, 0, _lastSegment, _lastSegment!.Memory.Length);
        }
    }
}