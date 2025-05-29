using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SimpleR.Internal;
using System.IO.Pipelines;

namespace SimpleR.Tests;

public class WebSocketConnectionContextZombieUnitTest
{
    private readonly HttpContext _mockHttpContext;
    private readonly ILogger _mockLogger;
    private readonly IDuplexPipe _mockTransport;
    private readonly IDuplexPipe _mockApplication;
    private readonly WebSocketConnectionDispatcherOptions _options;

    public WebSocketConnectionContextZombieUnitTest()
    {
        _mockHttpContext = Substitute.For<HttpContext>();
        _mockLogger = Substitute.For<ILogger>();
        _mockTransport = Substitute.For<IDuplexPipe>();
        _mockApplication = Substitute.For<IDuplexPipe>();
        _options = new WebSocketConnectionDispatcherOptions();
    }

    // Z – Zero
    [Fact]
    public void TestWebSocketConnectionContext_ZeroItems()
    {
        var context = new WebSocketConnectionContext(
            "test-id", _mockHttpContext, _mockLogger,
            _mockTransport, _mockApplication, _options);

        // Assert that the items dictionary is initialized to an empty dictionary
        Assert.Empty(context.Items);
    }

    // O – One
    [Fact]
    public void TestWebSocketConnectionContext_SingleItem()
    {
        var context = new WebSocketConnectionContext(
            "test-id", _mockHttpContext, _mockLogger,
            _mockTransport, _mockApplication, _options);

        // Add one item to the context
        context.Items["key"] = "value";

        // Assert that the item is added correctly
        Assert.Equal("value", context.Items["key"]);
    }

    // M – Many (or More complex)
    [Fact]
    public void TestWebSocketConnectionContext_ManyItems()
    {
        var context = new WebSocketConnectionContext(
            "test-id", _mockHttpContext, _mockLogger,
            _mockTransport, _mockApplication, _options);

        // Add many items to the context
        for (int i = 0; i < 10; i++)
        {
            context.Items[$"key{i}"] = $"value{i}";
        }

        // Assert that all items are added correctly
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal($"value{i}", context.Items[$"key{i}"]);
        }
    }

    // I – Interface definition
    [Fact]
    public void TestWebSocketConnectionContext_ImplementsFeatures()
    {
        var context = new WebSocketConnectionContext(
            "test-id", _mockHttpContext, _mockLogger,
            _mockTransport, _mockApplication, _options);

        // Assert that the required features are set
        Assert.NotNull(context.Features.Get<IConnectionUserFeature>());
        Assert.NotNull(context.Features.Get<IConnectionItemsFeature>());
        Assert.NotNull(context.Features.Get<IConnectionIdFeature>());
        Assert.NotNull(context.Features.Get<IConnectionTransportFeature>());
        Assert.NotNull(context.Features.Get<ITransferFormatFeature>());
        Assert.NotNull(context.Features.Get<IHttpTransportFeature>());
        Assert.NotNull(context.Features.Get<IConnectionLifetimeFeature>());
    }

    // E – Exercise Exceptional behavior
    [Fact]
    public void TestWebSocketConnectionContext_NullItems()
    {
        var context = new WebSocketConnectionContext(
            "test-id", _mockHttpContext, _mockLogger,
            _mockTransport, _mockApplication, _options);

        // Test boundary case: setting items to null
        Assert.Throws<ArgumentNullException>(() => context.Items = null!);
    }

    // S – Simple Scenarios
    [Fact]
    public void TestWebSocketConnectionContext_Initialization_Success()
    {
        // Arrange 
        string id = "test-id";
        // Act
        var context = new WebSocketConnectionContext(
            id, _mockHttpContext, _mockLogger,
            _mockTransport, _mockApplication, _options);

        // Assert
        Assert.Equal(id, context.ConnectionId);
        Assert.Equal(_mockApplication, context.Application);
        Assert.Equal(_mockTransport, context.Transport);
        Assert.Equal(TransferFormat.Text, context.ActiveFormat);
        Assert.Equal(_mockHttpContext.User, context.User);
    }

    [Fact]
    public void TestTryActivateConnection_ValidParameters_Success()
    {
        var context = new WebSocketConnectionContext(
            "test-id", _mockHttpContext, _mockLogger,
            _mockTransport, _mockApplication, _options);

        WebSocketsServerTransport transport = new(
            Substitute.For<WebSocketOptions>(),
            Substitute.For<IDuplexPipe>(),
            null!,
            Substitute.For<ILoggerFactory>());

        context.TryActivateConnection(Substitute.For<ConnectionDelegate>(), transport, Substitute.For<HttpContext>());

        Assert.NotNull(context.ApplicationTask);
        Assert.NotNull(context.TransportTask);
    }
}
