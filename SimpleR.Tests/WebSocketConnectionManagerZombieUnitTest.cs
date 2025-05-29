using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SimpleR.Internal;
using System.Collections.Concurrent;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

namespace SimpleR.Tests;

public class WebSocketConnectionManagerZombieUnitTest
{
    private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();
    private readonly WebSocketConnectionManager _manager;
    private readonly ILogger<WebSocketConnectionManager> _logger = new NullLogger<WebSocketConnectionManager>();

    public WebSocketConnectionManagerZombieUnitTest()
    {
        _loggerFactory.CreateLogger<WebSocketConnectionManager>().Returns(_logger);
        _manager = new WebSocketConnectionManager(_loggerFactory);
    }

    private ConcurrentDictionary<string, WebSocketConnectionContext> GetConnections()
    => (ConcurrentDictionary<string, WebSocketConnectionContext>)
        typeof(WebSocketConnectionManager)
            .GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_manager)!;

    private void InvokeRemoveConnection(string id)
        => typeof(WebSocketConnectionManager)
            .GetMethod("RemoveConnection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(_manager, [id]);

    // Z - zero
    [Fact]
    public void TestConnections_ConnectionCount_ShouldStartWithNoConnections()
    {
        // Act & Assert
        Assert.Empty(GetConnections());
    }

    // O - One
    [Fact]
    public void TestCreateConnection_CreateSingleConnection_ShouldAddSingleConnection()
    {
        // Arrange
        var httpContext = Substitute.For<HttpContext>();
        var options = new WebSocketConnectionDispatcherOptions();

        // Act
        var connection = _manager.CreateConnection(httpContext, options);

        // Assert
        Assert.Single(GetConnections());
        Assert.Equal(connection, GetConnections()[connection.ConnectionId]);
    }

    // M - Many
    [Fact]
    public void TestCreateConnection_CreateMultipleConnections_ShouldAddMultipleConnections()
    {
        // Arrange
        var httpContext = Substitute.For<HttpContext>();
        var options = new WebSocketConnectionDispatcherOptions();

        // Act
        _manager.CreateConnection(httpContext, options);
        _manager.CreateConnection(httpContext, options);
        _manager.CreateConnection(httpContext, options);

        // Assert
        Assert.Equal(3, GetConnections().Count);
    }

    // B - Boundary
    [Fact]
    public void TestRemoveConnection_InvalidId_ShouldNotCauseErrors()
    {
        // Act & Assert
        Exception ex = Record.Exception(() => InvokeRemoveConnection("non-existent-id"));
        Assert.Null(ex);
    }

    [Fact]
    public void TestCreateConnection_CreateMultipleConnections_ShouldHaveDifferentIds()
    {
        // Arrange
        var httpContext = Substitute.For<HttpContext>();
        var options = new WebSocketConnectionDispatcherOptions();

        // Act
        WebSocketConnectionContext connection1 = _manager.CreateConnection(httpContext, options);
        WebSocketConnectionContext connection2 = _manager.CreateConnection(httpContext, options);

        // Assert
        Assert.NotEqual(connection1.ConnectionId, connection2.ConnectionId);
    }

    // I - Interface
    [Fact]
    public async Task TestDisposeAndRemoveAsync_CreateAndRemoveConnection_ShouldDisposeConnection()
    {
        // Arrange
        var httpContext = Substitute.For<HttpContext>();
        var options = new WebSocketConnectionDispatcherOptions();
        var connection = _manager.CreateConnection(httpContext, options);

        // Act
        await _manager.DisposeAndRemoveAsync(connection, closeGracefully: true);

        // Assert
        Assert.Empty(GetConnections());
    }
}
