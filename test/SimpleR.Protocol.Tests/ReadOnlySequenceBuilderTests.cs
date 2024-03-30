using System.Buffers;
using SimpleR.Protocol.Internal;

namespace SimpleR.Protocol.Tests;

public class ReadOnlySequenceBuilderTests
{
    [Fact]
    public void Append_ShouldAddSegmentToSequence()
    {
        // Arrange
        var builder = new ReadOnlySequenceBuilder<byte>();
        var memory = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });

        // Act
        builder.Append(memory);
        var sequence = builder.Build();

        // Assert
        sequence.First.Span.ToArray().Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Build_ShouldReturnEmptySequence_WhenNoSegmentsAppended()
    {
        // Arrange
        var builder = new ReadOnlySequenceBuilder<byte>();

        // Act
        var sequence = builder.Build();

        // Assert
        sequence.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Build_ShouldReturnSequenceWithAllAppendedSegments()
    {
        // Arrange
        var builder = new ReadOnlySequenceBuilder<byte>();
        var memory1 = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        var memory2 = new ReadOnlyMemory<byte>(new byte[] { 4, 5, 6 });

        // Act
        builder.Append(memory1);
        builder.Append(memory2);
        var sequence = builder.Build();

        // Assert
        sequence.ToArray().Should().Equal(1, 2, 3, 4, 5, 6);
    }
}