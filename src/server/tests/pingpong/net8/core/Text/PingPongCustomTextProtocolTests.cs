using System.Net.WebSockets;
using System.Text;
using PingPongNet8Server;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Websocket.Client;

namespace PingPongNet8Tests.Text;

public class PingPongCustomTextProtocolTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _route;

    public PingPongCustomTextProtocolTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _route = "/ping/text/custom";
    }
    
    [Fact]
    public async Task SendMultiPartMessage_GetReversed()
    {
        // Arrange
        var receivedQueue = new List<string?>();
        var (client, socket) = await _factory.Server.ConnectWebsocketAsync(_route);
        try
        {
            using var sub = client.MessageReceived.Subscribe((message) => { receivedQueue.Add(message.Text); });
            var bytes = "Ping"u8.ToArray().Concat(new byte[]{0}).ToArray();
            for(var i = 0; i < bytes.Length; i += 2)
            {
                receivedQueue.Should().BeEmpty();
                await socket.SendAsync(new ArraySegment<byte>(bytes[i..Math.Min(i + 2, bytes.Length)]), WebSocketMessageType.Text, false,
                    CancellationToken.None);
            }
            
            await WaitHelpers.WaitFor(() => receivedQueue.Any());

            receivedQueue.Should().HaveCount(1);
            receivedQueue[0].Should().Be("gniP");
        }
        finally
        {
            client.Dispose();
        }
    }
    
    [Fact]
    public async Task SendSinglePartMessage_GetReversed()
    {
        
        // Arrange
        var receivedQueue = new List<string?>();
        var (client, socket) = await _factory.Server.ConnectWebsocketAsync(_route);
        try
        {
            using var sub = client.MessageReceived.Subscribe((message) => { receivedQueue.Add(message.Text); });
            var bytes = "Ping"u8.ToArray().Concat(new byte[]{0}).ToArray();
            
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, false,
                CancellationToken.None);
            
            await WaitHelpers.WaitFor(() => receivedQueue.Any());

            receivedQueue.Should().HaveCount(1);
            receivedQueue[0].Should().Be("gniP");
        }
        finally
        {
            client.Dispose();
        }
    }
    
    [Fact]
    public async Task ConnectMultiple_DisconnectSome_CountIsCorrect()
    {
        // Arrange
        var connectionManager = _factory.Server.Services.GetRequiredService<ConnectionManager>();
        var initialConnections = connectionManager.Connections.Count;
        var initialDisconnections = connectionManager.Disconnections.Count;
        using var client1 = (await _factory.Server.ConnectWebsocketAsync(_route)).client;
        using var client2 = (await _factory.Server.ConnectWebsocketAsync(_route)).client;
        using var client3 = (await _factory.Server.ConnectWebsocketAsync(_route)).client;
        client1.Dispose();
        client2.Dispose();
        await WaitHelpers.WaitFor(() => connectionManager.Disconnections.Count != initialDisconnections);
        connectionManager.Connections.Count.Should().Be(initialConnections + 3);
        connectionManager.Disconnections.Count.Should().Be(initialDisconnections + 2);
    }
    
}