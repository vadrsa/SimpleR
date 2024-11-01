using System.Net.WebSockets;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PingPong.Server.Tests;
using Websocket.Client;

namespace PingPong.Server.Tests.Text;

public class PingPongDelimitedTextProtocolTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _route;

    public PingPongDelimitedTextProtocolTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _route = "/ping/text/eom";
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
            var bytes = Encoding.UTF8.GetBytes("Ping");
            for(var i = 0; ; i += 2)
            {
                receivedQueue.Should().BeEmpty();
                if(i+2 >= bytes.Length)
                {
                    await socket.SendAsync(new ArraySegment<byte>(bytes[i..]), WebSocketMessageType.Text, true,
                        CancellationToken.None);
                    break;
                }
                else
                {
                    await socket.SendAsync(new ArraySegment<byte>(bytes[i..Math.Min(i + 2, bytes.Length)]), WebSocketMessageType.Text, false,
                        CancellationToken.None);
                }
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
            var bytes = Encoding.UTF8.GetBytes("Ping");
            
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
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
    public async Task SendEmptyMessage_GetBack()
    {
        // Arrange
        var receivedQueue = new List<string?>();
        var (client, socket) = await _factory.Server.ConnectWebsocketAsync(_route);
        try
        {
            using var sub = client.MessageReceived.Subscribe((message) => { receivedQueue.Add(message.Text); });
            await socket.SendAsync(new ArraySegment<byte>([], 0, 0), WebSocketMessageType.Text, true,
                CancellationToken.None);
            await socket.SendAsync(new ArraySegment<byte>([], 0, 0), WebSocketMessageType.Text, true,
                CancellationToken.None);
            
            await WaitHelpers.WaitFor(() => receivedQueue.Count == 2);

            receivedQueue.Should().HaveCount(2);
            receivedQueue[0].Should().Be("");
            receivedQueue[1].Should().Be("");
        }
        finally
        {
            client.Dispose();
        }
    }
}