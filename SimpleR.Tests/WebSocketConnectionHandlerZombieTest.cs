using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SimpleR.Internal;
using SimpleR.Protocol;
using SimpleR.Tests.Mocks;
using System.Buffers;
using System.IO.Pipelines;

namespace SimpleR.Tests;

public class WebSocketConnectionHandlerZombieTest
{
    private readonly IMessageProtocol<string, string> _messageProtocol;
    private readonly IWebSocketMessageDispatcher<string, string> _dispatcher;
    private readonly ILogger<WebSocketConnectionHandler<string, string>> _logger;
    private readonly WebSocketConnectionHandler<string, string> _handler;
    private readonly ConnectionContext _connection;

    public WebSocketConnectionHandlerZombieTest()
    {
        _messageProtocol = Substitute.For<IMessageProtocol<string, string>>();
        _dispatcher = Substitute.For<IWebSocketMessageDispatcher<string, string>>();
        _logger = new NullLogger<WebSocketConnectionHandler<string, string>>();
        _handler = new WebSocketConnectionHandler<string, string>(_messageProtocol, _dispatcher, _logger);
        _connection = Substitute.For<ConnectionContext>();
    }

    // O - One
    [Fact]
    public void TestTryParseMessageImpl_ValidMessage_ParsesSuccessfully()
    {
        // Arrange
        var expectedMessage = "someMessage";
        ReadOnlySequence<byte> buffer = new(); // Buffer can be empty as we mock TryParseMessageImpl method
        object[] parameters = [buffer, null!, null!];
        _messageProtocol.TryParseMessage(ref Arg.Any<ReadOnlySequence<byte>>(), out Arg.Any<string>())
            .Returns(x =>
            {
                x[1] = expectedMessage;
                return true;
            });

        // Act
        bool? result = (bool?)InvokeTryParseMessageImpl(parameters);

        // Assert
        Assert.True(result);
        Assert.NotNull(parameters[1]);
        Assert.Equal(parameters[1], expectedMessage);
        Assert.Null(parameters[2]);
    }

    [Fact]
    public void TestTryParseMessageImpl_ValidMessage_ReturnsFalse()
    {
        // Arrange
        ReadOnlySequence<byte> buffer = new(); // Buffer can be empty as we mock TryParseMessageImpl method
        object[] parameters = [buffer, null!, null!];
        _messageProtocol.TryParseMessage(ref Arg.Any<ReadOnlySequence<byte>>(), out Arg.Any<string>())
            .Returns(x =>
            {
                x[1] = null;
                return false;
            });

        // Act
        bool? result = (bool?)InvokeTryParseMessageImpl(parameters);

        // Assert
        Assert.False(result);
        Assert.Null(parameters[1]);
        Assert.Null(parameters[2]);
    }

    [Fact]
    public async Task TestOnConnectedAsync_TryParseMessage_DispatchMessageIsSuccessful()
    {
        // Arrange
        string message = "test";
        MessageProtocolMock<string, string> protocol = new(message, null);
        WebSocketConnectionHandler<string, string> handler = new(protocol, _dispatcher, _logger);

        Pipe pipe = new();
        DuplexPipe duplexPipe = new(pipe.Reader, pipe.Writer);
        byte[] data = []; // Buffer can be empty as we mock data returned from TryParseMessage
        Memory<byte> memory = pipe.Writer.GetMemory(data.Length);

        data.CopyTo(memory);
        pipe.Writer.Advance(memory.Length);

        await pipe.Writer.FlushAsync();
        pipe.Writer.Complete();

        _connection.Transport.Returns(duplexPipe);

        // Act
        await handler.OnConnectedAsync(_connection);

        // Assert
        await _dispatcher.Received(1).DispatchMessageAsync(Arg.Any<IWebsocketConnectionContext<string>>(), message);
        await _dispatcher.Received(1).OnDisconnectedAsync(Arg.Any<IWebsocketConnectionContext<string>>(), null);
    }

    // B - Boundary
    [Fact]
    public async Task TestOnConnectedAsync_TerminatedConnectionWhileEmptyBuffer_ConnectionIsAbortedNormally()
    {
        // Arrange
        var pipe = new Pipe();
        DuplexPipe duplexPipe = new(pipe.Reader, pipe.Writer);
        await pipe.Writer.FlushAsync();
        pipe.Writer.Complete();

        _connection.Transport.Returns(duplexPipe);

        // Act
        await _handler.OnConnectedAsync(_connection);

        // Assert
        await _dispatcher.Received(1).OnDisconnectedAsync(Arg.Any<IWebsocketConnectionContext<string>>(), null);
    }

    [Fact]
    public async Task TestOnConnectedAsync_TerminatedConnectionWhileReading_ConnectionIsAborted()
    {
        // Arrange
        Pipe pipe = new();
        DuplexPipe duplexPipe = new(pipe.Reader, pipe.Writer);
        byte[] data = "test"u8.ToArray();
        Memory<byte> memory = pipe.Writer.GetMemory(data.Length);

        data.CopyTo(memory);
        pipe.Writer.Advance(memory.Length);

        await pipe.Writer.FlushAsync();
        pipe.Writer.Complete();

        _connection.Transport.Returns(duplexPipe);

        // Act
        await _handler.OnConnectedAsync(_connection);

        // Assert
        await _dispatcher.Received(1).OnDisconnectedAsync(Arg.Any<IWebsocketConnectionContext<string>>(), Arg.Any<InvalidDataException>());
    }

    // E - Exception
    [Fact]
    public async Task TestOnConnectedAsync_NoInput_ThrowsError()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(async () => await _handler.OnConnectedAsync(_connection));
    }

    [Fact]
    public void TestTryParseMessageImpl_ThrowsError_CapturesException()
    {
        // Arrange
        ReadOnlySequence<byte> buffer = new("test"u8.ToArray());
        object[] parameters = [buffer, null!, null!];
        _messageProtocol.TryParseMessage(ref Arg.Any<ReadOnlySequence<byte>>(), out Arg.Any<string>())
            .Throws(new InvalidOperationException("Parsing error"));

        // Act
        bool? result = (bool?)InvokeTryParseMessageImpl(parameters);

        // Assert
        Assert.True(result);
        Assert.Null(parameters[1]);
        Assert.IsType<InvalidOperationException>(parameters[2]);
    }

    [Fact]
    public async Task TestOnConnectedAsync_TryParseMessageException_DispatchMessageIsNotSuccessful()
    {
        // Arrange
        Exception ex = new("Some error");
        MessageProtocolMock<string, string> protocol = new("test", ex);
        WebSocketConnectionHandler<string, string> handler = new(protocol, _dispatcher, _logger);
        Pipe pipe = new();
        DuplexPipe duplexPipe = new(pipe.Reader, pipe.Writer);
        byte[] data = []; // Buffer can be empty as we mock data returned from TryParseMessage
        Memory<byte> memory = pipe.Writer.GetMemory(data.Length);

        data.CopyTo(memory);
        pipe.Writer.Advance(memory.Length);

        await pipe.Writer.FlushAsync();
        pipe.Writer.Complete();

        _connection.Transport.Returns(duplexPipe);

        // Act
        await handler.OnConnectedAsync(_connection);

        // Assert
        await _dispatcher.Received(1).OnParsingIssueAsync(Arg.Any<IWebsocketConnectionContext<string>>(), ex);
        await _dispatcher.Received(1).OnDisconnectedAsync(Arg.Any<IWebsocketConnectionContext<string>>(), null);
    }

    #region Helper methods
    
    private object? InvokeTryParseMessageImpl(object[] parameters)
        => typeof(WebSocketConnectionHandler<string, string>)
            .GetMethod("TryParseMessageImpl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_handler, parameters);

    #endregion
}
