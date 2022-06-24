using System.Net.WebSockets;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PingPong.Server.Tests;
using Websocket.Client;

namespace PingPong.Server.Tests.Text;

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
            var bytes = Encoding.UTF8.GetBytes("Ping").Concat(new byte[]{0}).ToArray();
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
            var bytes = Encoding.UTF8.GetBytes("Ping").Concat(new byte[]{0}).ToArray();
            
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
    
}