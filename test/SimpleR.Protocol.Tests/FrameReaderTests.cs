using System.Buffers;
using SimpleR.Protocol.Internal;

namespace SimpleR.Protocol.Tests;

public class FrameReaderTests
{
    private readonly FrameReader _frameReader;

    public FrameReaderTests()
    {
        _frameReader = new FrameReader();
    }

    [Fact]
    public void ReadFrame_ShouldReturnFalse_WhenInputIsEmpty()
    {
        var input = new ReadOnlySequence<byte>(Array.Empty<byte>());

        var result = _frameReader.ReadFrame(ref input, out var frame, out var isEndOfMessage);

        result.Should().BeFalse();
        frame.IsEmpty.Should().BeTrue();
        isEndOfMessage.Should().BeFalse();
    }

    [Fact]
    public void ReadFrame_ShouldReturnFalse_WhenInputIsLessThanIntegerLength()
    {
        var input = new ReadOnlySequence<byte>(new byte[3]);

        var result = _frameReader.ReadFrame(ref input, out var frame, out var isEndOfMessage);

        result.Should().BeFalse();
        frame.IsEmpty.Should().BeTrue();
        isEndOfMessage.Should().BeFalse();
    }

    [Fact]
    public void ReadFrame_ShouldReturnTrue_WhenInputContainsValidFrame()
    {
        var input = new ReadOnlySequence<byte>(new byte[] { 0, 0, 0, 5, 1, 2, 3, 4, 5, 1, 0, 0, 0, 2, 6, 7, 0 });

        var result = _frameReader.ReadFrame(ref input, out var frame, out var isEndOfMessage);

        result.Should().BeTrue();
        frame.Length.Should().Be(5);
        frame.ToArray().Should().Equal(1, 2, 3, 4, 5);
        input.ToArray().Should().Equal(0, 0, 0, 2, 6, 7, 0);
        isEndOfMessage.Should().BeTrue();
    }
}