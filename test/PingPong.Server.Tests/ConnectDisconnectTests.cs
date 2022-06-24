using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Websocket.Client;

namespace PingPong.Server.Tests;

public class ConnectDisconnectTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _route;

    public ConnectDisconnectTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _route = "/noop";
    }
    
    [Fact]
    public async Task ConnectMultiple_DisconnectSome_CountIsCorrect()
    {
        // Arrange
        var connectionManager = _factory.Server.Services.GetRequiredService<ConnectionManager>();
        using var client1 = (await _factory.Server.ConnectWebsocketAsync(_route)).client;
        using var client2 = (await _factory.Server.ConnectWebsocketAsync(_route)).client;
        using var client3 = (await _factory.Server.ConnectWebsocketAsync(_route)).client;
        client1.Dispose();
        client2.Dispose();
        client3.Dispose();
        await WaitHelpers.WaitFor(() => connectionManager.Connections.Count >= 3);
        await WaitHelpers.WaitFor(() => connectionManager.Disconnections.Count >= 3);
        connectionManager.Connections.Should().Contain(connectionManager.Disconnections);
        connectionManager.Connections.Count.Should().Be(3);
        connectionManager.Disconnections.Count .Should().Be(3);
    }
}