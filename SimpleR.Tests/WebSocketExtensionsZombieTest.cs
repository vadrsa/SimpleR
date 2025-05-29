using NSubstitute;
using SimpleR.Protocol.Internal;
using System.Buffers;
using System.Net.WebSockets;

namespace SimpleR.Tests;

public class WebSocketExtensionsZombieTest
{
    // O - One
    [Fact]
    public async Task TestSendAsync_SingleSegment_CallsWebSocketSendAsyncOnce()
    {
        // Arrange
        WebSocket webSocket = Substitute.For<WebSocket>();
        ReadOnlySequence<byte> buffer = new([1, 2, 3]);
        WebSocketMessageType messageType = WebSocketMessageType.Binary;
        bool isEndOfMessage = true;
        CancellationToken cancellationToken = CancellationToken.None;

        // Act
        await webSocket.SendAsync(buffer, messageType, isEndOfMessage, CancellationToken.None);

        // Assert
        await webSocket.Received(1).SendAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            messageType,
            endOfMessage: isEndOfMessage,
            cancellationToken
        );
    }
    // M - Multiple
    [Fact]
    public async Task TestSendAsync_MultiSegment_CallsWebSocketSendAsyncMultipleTimes()
    {
        // Arrange
        var webSocket = Substitute.For<WebSocket>();
        var buffer = CreateMultiSegmentSequence();
        var messageType = WebSocketMessageType.Binary;
        var isEndOfMessage = true;
        var cancellationToken = CancellationToken.None;

        // Act
        await webSocket.SendAsync(buffer, messageType, isEndOfMessage, cancellationToken);

        // Assert
        await webSocket.Received(1).SendAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            messageType,
            endOfMessage: false,
            cancellationToken);
        await webSocket.Received(1).SendAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            messageType,
            endOfMessage: isEndOfMessage,
            cancellationToken
        );
    }

    #region Helper Methods
    
    private static ReadOnlySequence<byte> CreateMultiSegmentSequence()
    {
        MemorySegment<byte> first = new(new byte[] { 1, 2, 3 });
        MemorySegment<byte> last = first.Append(new byte[] { 4, 5, 6 });

        var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        return sequence;
    }

    #endregion
}
