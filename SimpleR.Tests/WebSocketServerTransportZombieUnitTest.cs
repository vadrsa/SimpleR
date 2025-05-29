using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SimpleR.Internal;
using SimpleR.Protocol.Internal;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net.WebSockets;

namespace SimpleR.Tests;

public class WebSocketServerTransportZombieUnitTest
{
    private readonly WebSocketsServerTransport _transport;
    private readonly WebSocketOptions _options;
    private readonly IDuplexPipe _application;
    private readonly WebSocketConnectionContext _connection;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HttpContext _httpContext;

    private static byte[] GetByteLength(int length) => BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(length));

    public WebSocketServerTransportZombieUnitTest()
    {
        _options = new WebSocketOptions();
        _application = Substitute.For<IDuplexPipe>();
        _connection = new WebSocketConnectionContext(
                    "test-id", Substitute.For<HttpContext>(), Substitute.For<ILogger>(),
                    Substitute.For<IDuplexPipe>(), Substitute.For<IDuplexPipe>(), new WebSocketConnectionDispatcherOptions());
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _httpContext = Substitute.For<HttpContext>();

        var pipe = new Pipe();
        DuplexPipe duplexPipe = new(pipe.Reader, pipe.Writer);
        _transport = new WebSocketsServerTransport(_options, duplexPipe, _connection, _loggerFactory);
    }

    // B - Boundary
    [Fact]
    public async Task TestProcessSocketAsync_WaitForApplicationFinish_NormalFinish()
    {
        // Arrange
        Pipe pipe = new();
        DuplexPipe duplexPipe = new(pipe.Reader, pipe.Writer);
        WebSocketsServerTransport transport = new(_options, duplexPipe, _connection, _loggerFactory);

        pipe.Writer.Complete();

        WebSocket ws = Substitute.For<WebSocket>();
        ws.ReceiveAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
          .Returns(new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true)));

        // Act
        await transport.ProcessSocketAsync(ws);

        // Assert
        Assert.True(ws.State == WebSocketState.None);
    }
    
    [Fact]
    public async Task TestStartReceiving_CloseMessage_GracefullyClosed()
    {
        // Arrange
        WebSocket ws = Substitute.For<WebSocket>();
        ws.ReceiveAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
          .Returns(new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true)));

        // Act
        InvokeStartReceiving([ws]);

        // Assert
        await ws.Received(1).ReceiveAsync(Memory<byte>.Empty, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task TestStartReceiving_CloseAfterZeroByteRead_GracefullyClosed()
    {
        // Arrange
        WebSocket ws = Substitute.For<WebSocket>();
        ws.ReceiveAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
          .Returns(
                new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, false)),
                new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, false)));

        // Act
        InvokeStartReceiving([ws]);

        // Assert
        await ws.Received(1).ReceiveAsync(Memory<byte>.Empty, Arg.Any<CancellationToken>());
        await ws.Received(2).ReceiveAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task TestStartReceiving_FullMessageAndClose_GracefullyClosed()
    {
        // Arrange
        WebSocket ws = Substitute.For<WebSocket>();
        ws.ReceiveAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
          .Returns(
                new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, false)),
                new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(2, WebSocketMessageType.Text, false)),
                // At the end close the web socket
                new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, false)));

        // Act
        InvokeStartReceiving([ws]);

        // Assert
        await ws.Received(2).ReceiveAsync(Memory<byte>.Empty, Arg.Any<CancellationToken>());
        await ws.Received(3).ReceiveAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task TestStartReceiving_EndOfMessageAndClose_GracefullyClosed()
    {
        // Arrange
        WebSocket ws = Substitute.For<WebSocket>();
        ws.ReceiveAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
          .Returns(
                new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, true)),
                // At the end close the web socket
                new ValueTask<ValueWebSocketReceiveResult>(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, false)));

        // Act
        InvokeStartReceiving([ws]);

        // Assert
        await ws.Received(2).ReceiveAsync(Memory<byte>.Empty, Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task TestStartSending_EmptyBuffer_ShouldHandleEmptyBufferGracefully()
    {
        // Arrange
        WebSocket ws = Substitute.For<WebSocket>();
        Pipe pipe = new();
        DuplexPipe duplexPipe = new(pipe.Reader, pipe.Writer);
        WebSocketsServerTransport transport = new(_options, duplexPipe, _connection, _loggerFactory);
        pipe.Writer.Complete(); // Complete immediately as we want empty buffer

        // Act
        InvokeStartSending(transport, [ws]);

        // Assert
        await ws.DidNotReceive().SendAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await ws.Received(1).CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", Arg.Any<CancellationToken>());
        Assert.True(ws.State == WebSocketState.None);
    }
    
    [Fact]
    public async Task TestStartSending_ClosedWebSocket_ShouldSendData()
    {
        // Arrange
        byte[] payload1 = "ABC"u8.ToArray();
        int payLoad1_length = payload1.Length;
        byte[] length1 = GetByteLength(payLoad1_length);

        ReadOnlyMemory<byte> input = new([.. length1, .. payload1, FrameHelpers.IsEndOfMessageByte]);

        Pipe pipe = new();
        DuplexPipe duplexPipe = new(pipe.Reader, pipe.Writer);
        WebSocketsServerTransport transport = new(new WebSocketOptions() { FramePackets = true }, duplexPipe, _connection, _loggerFactory);
        WebSocket ws = Substitute.For<WebSocket>();
        ws.State.Returns(WebSocketState.Closed);

        await pipe.Writer.WriteAsync(input);
        pipe.Writer.Complete();

        // Act
        InvokeStartSending(transport, [ws]);

        // Assert
        await ws.DidNotReceive().SendAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await ws.DidNotReceive().CloseOutputAsync(Arg.Any<WebSocketCloseStatus>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.True(ws.State == WebSocketState.Closed);
    }
    
    [Fact]
    public async Task TestStartSending_FrameReaderWithBuffer_ShouldSendData()
    {
        // Arrange
        byte[] payload = "ABC"u8.ToArray();
        int payLoad_length = payload.Length;
        byte[] length = GetByteLength(payLoad_length);

        ReadOnlyMemory<byte> input = new([.. length, .. payload, FrameHelpers.IsNotEndOfMessageByte, .. length, .. payload, FrameHelpers.IsEndOfMessageByte]);

        Pipe pipe = new();
        DuplexPipe duplexPipe = new(pipe.Reader, pipe.Writer);
        WebSocketsServerTransport transport = new(new WebSocketOptions() { FramePackets = true }, duplexPipe, _connection, _loggerFactory);
        var ws = Substitute.For<WebSocket>();

        await pipe.Writer.WriteAsync(input);
        pipe.Writer.Complete();

        // Act
        InvokeStartSending(transport, [ws]);

        // Assert
        await ws.Received(1).SendAsync(Arg.Any<ReadOnlyMemory<byte>>(), WebSocketMessageType.Text, false, Arg.Any<CancellationToken>());
        await ws.Received(1).SendAsync(Arg.Any<ReadOnlyMemory<byte>>(), WebSocketMessageType.Text, true, Arg.Any<CancellationToken>());
        await ws.Received(1).CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", Arg.Any<CancellationToken>());
        Assert.True(ws.State == WebSocketState.None);
    }

    private object? InvokeStartReceiving(object[] parameters)
    => typeof(WebSocketsServerTransport)
        .GetMethod("StartReceiving", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
        .Invoke(_transport, parameters);

    private static object? InvokeStartSending(WebSocketsServerTransport transport, object[] parameters)
    => typeof(WebSocketsServerTransport)
        .GetMethod("StartSending", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
        .Invoke(transport, parameters);
}