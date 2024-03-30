using System.Buffers;
using SimpleR.Protocol.Internal;

namespace SimpleR.Protocol.Tests;

public class FrameBufferWriterTests
{
    // Buffer writer to write bytes into
    private readonly ArrayBufferWriter<byte> _bufferWriter;
    // Frame writer to write frames into the buffer
    private readonly FrameBufferWriter _frameWriter;

    public FrameBufferWriterTests()
    {
        // Initialize buffer and frame writers
        _bufferWriter = new ArrayBufferWriter<byte>();
        _frameWriter = new FrameBufferWriter(_bufferWriter);
    }

    [Fact]
    public void WriteTwoFrames_UsingMemory_ShouldBeCorrect()
    {
        // Get memory from frame writer
        var memory = _frameWriter.GetMemory();

        // Copy bytes into the memory
        new byte[] { 1, 2, 3, 4, 5 }.CopyTo(memory);

        // Advance the writer by the number of bytes written
        _frameWriter.Advance(5);
        var memory2 = _frameWriter.GetMemory();

        // Copy more bytes into the memory
        new byte[] { 6, 7 }.CopyTo(memory2);

        // Advance the writer by the number of bytes written
        _frameWriter.Advance(2);

        // Finish writing the last frame
        _frameWriter.FinishLastFrame(true);

        // Get the written bytes as an array
        var result = _bufferWriter.WrittenMemory.ToArray();

        // Assert that the result is as expected
        result.Should()
            .BeEquivalentTo(new byte[] { 0, 0, 0, 5, 1, 2, 3, 4, 5, 0 }.Concat(new byte[] { 0, 0, 0, 2, 6, 7, 1 }));
    }

    [Fact]
    public void WriteTwoFrames_UsingSpan_ShouldBeCorrect()
    {
        // Get span from frame writer
        var span = _frameWriter.GetSpan();

        // Copy bytes into the span
        new byte[] { 1, 2, 3, 4, 5 }.CopyTo(span);

        // Advance the writer by the number of bytes written
        _frameWriter.Advance(5);
        var span2 = _frameWriter.GetSpan();

        // Copy more bytes into the span
        new byte[] { 6, 7 }.CopyTo(span2);

        // Advance the writer by the number of bytes written
        _frameWriter.Advance(2);

        // Finish writing the last frame
        _frameWriter.FinishLastFrame(true);

        // Get the written bytes as an array
        var result = _bufferWriter.WrittenMemory.ToArray();

        // Assert that the result is as expected
        result.Should()
            .BeEquivalentTo(new byte[] { 0, 0, 0, 5, 1, 2, 3, 4, 5, 0 }.Concat(new byte[] { 0, 0, 0, 2, 6, 7, 1 }));
    }
}