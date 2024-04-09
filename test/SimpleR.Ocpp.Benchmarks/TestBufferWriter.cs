using System.Buffers;

namespace SimpleR.Ocpp.Benchmarks;

public class TestBufferWriter : IBufferWriter<byte>
{
    // huge buffer that should be large enough for writing any content
    private readonly byte[] _buffer = new byte[20000];

    public void Advance(int bytes)
    {
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        return _buffer;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        return _buffer;
    }
}