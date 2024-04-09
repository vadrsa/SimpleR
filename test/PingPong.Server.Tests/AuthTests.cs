using Microsoft.AspNetCore.Mvc.Testing;
using Websocket.Client;

namespace PingPong.Server.Tests;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _route;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _route = "/noop/auth";
    }
    
    [Fact]
    public async Task Connect_Rejected()
    {
        using var client = _factory.Server.CreateWebsocketAsync(_route).client;
        var start = async () => await client.StartOrFail();
        await start.Should().ThrowAsync<Exception>();
    }
}