using Microsoft.AspNetCore.Connections;
using NSubstitute;
using SimpleR.Internal;
using SimpleR.Protocol;
using System.IO.Pipelines;

namespace SimpleR.Tests;

public class ApplicationConnectionContextZombieUnitTest
{
    private readonly ConnectionContext _mockConnectionContext;
    private readonly IMessageWriter<string> _mockWriter;
    private readonly ApplicationConnectionContext<string> _applicationConnectionContext;

    public ApplicationConnectionContextZombieUnitTest()
    {
        _mockConnectionContext = Substitute.For<ConnectionContext>();
        _mockWriter = Substitute.For<IMessageWriter<string>>();

        var mockReader = Substitute.For<PipeReader>();
        var mockWriter = Substitute.For<PipeWriter>();
        _mockConnectionContext.Transport.Returns(new DuplexPipe(mockReader, mockWriter));

        _applicationConnectionContext = new ApplicationConnectionContext<string>(_mockConnectionContext, _mockWriter);
    }

    // Z - Zero

    // O - One
    [Fact]
    public async Task TestWriteAsync_OneMessage_OneWrite()
    {
        // Arrange
        string message = "TestMessage";

        // Act
        await _applicationConnectionContext.WriteAsync(message);

        // Assert
        _mockWriter.Received(1).WriteMessage(message, _mockConnectionContext.Transport.Output);
    }

    // M - Multiple
    [Fact]
    public async Task TestWriteAsync_MultipleMessage_MultipleWrites()
    {
        // Arrange
        string message = "TestMessage";

        // Act
        await _applicationConnectionContext.WriteAsync(message);
        await _applicationConnectionContext.WriteAsync(message);
        await _applicationConnectionContext.WriteAsync(message);

        // Assert
        _mockWriter.Received(3).WriteMessage(message, _mockConnectionContext.Transport.Output);
    }

    // B - Boundary
    [Fact]
    public async Task TestAbortAsync_AbortsCorrectly()
    {
        // Arrange
        string message = "TestMessage";

        // Act
        _applicationConnectionContext.Abort();
        await _applicationConnectionContext.WriteAsync(message);
        await Task.Delay(10);

        // Assert
        Assert.True(_applicationConnectionContext.ConnectionAborted.IsCancellationRequested);
        _mockWriter.DidNotReceive().WriteMessage(message, _mockConnectionContext.Transport.Output);
    }

    // I - Interface
    [Fact]
    public void ShouldImplementIWebSocketConnectionContext()
    {
        Assert.IsAssignableFrom<IWebsocketConnectionContext<string>>(_applicationConnectionContext);
    }

    // E - Exceptional behavior
    [Fact]
    public async Task TestWriteAsync_ExceptionDuringWrite_AbortsConnectionAndCatchException()
    {
        // Arrange
        _mockWriter
            .When(x => x.WriteMessage(Arg.Any<string>(), Arg.Any<PipeWriter>()))
            .Do(_ => throw new InvalidOperationException("Write failed"));

        // Act
        await _applicationConnectionContext.WriteAsync("Test");
        await Task.Delay(10);

        // Assert
        Assert.True(_applicationConnectionContext.ConnectionAborted.IsCancellationRequested);
        Assert.NotNull(_applicationConnectionContext.CloseException);
        Assert.IsType<InvalidOperationException>(_applicationConnectionContext.CloseException);
    }

    // Other tests
    [Fact]
    public void User_FeatureNotAvailable_ReturnsNewClaimsPrincipal()
    {
        // Arrange & Act
        var user = _applicationConnectionContext.User;

        // Assert
        Assert.NotNull(user);
    }
}
