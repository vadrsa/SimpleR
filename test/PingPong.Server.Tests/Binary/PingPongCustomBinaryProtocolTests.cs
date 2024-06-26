using System.Net.WebSockets;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Websocket.Client;

namespace PingPong.Server.Tests.Binary;

public class PingPongCustomBinaryProtocolTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _route;

    public PingPongCustomBinaryProtocolTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _route = "/ping/binary/custom";
    }
    
    [Fact]
    public async Task SendMultiPartMessage_GetReversed()
    {
        // Arrange
        var receivedQueue = new List<byte[]?>();
        var (client, socket) = await _factory.Server.ConnectWebsocketAsync(_route);
        try
        {
            using var sub = client.MessageReceived.Subscribe((message) => { receivedQueue.Add(message.Binary); });

            await socket.SendAsync(new ArraySegment<byte>(new byte[] { 1, 2 }), WebSocketMessageType.Binary, false,
                CancellationToken.None);
            receivedQueue.Should().BeEmpty();
            await socket.SendAsync(new ArraySegment<byte>(new byte[] { 3, 4 }), WebSocketMessageType.Binary, false,
                CancellationToken.None);
            receivedQueue.Should().BeEmpty();
            await socket.SendAsync(new ArraySegment<byte>(new byte[] { 5, 0 }), WebSocketMessageType.Binary, false,
                CancellationToken.None);
            await WaitHelpers.WaitFor(() => receivedQueue.Any());

            receivedQueue.Should().HaveCount(1);
            receivedQueue[0].Should().Equal(5, 4, 3, 2, 1);
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
        var receivedQueue = new List<byte[]?>();
        var (client, socket) = await _factory.Server.ConnectWebsocketAsync(_route);
        try
        {
            using var sub = client.MessageReceived.Subscribe(message => { receivedQueue.Add(message.Binary); });

            await socket.SendAsync(new ArraySegment<byte>(new byte[] { 1, 2, 3, 4, 5, 0 }), WebSocketMessageType.Binary,
                false, CancellationToken.None);
            await WaitHelpers.WaitFor(() => receivedQueue.Any());

            receivedQueue.Should().HaveCount(1);
            receivedQueue[0].Should().Equal(5, 4, 3, 2, 1);
        }
        finally
        {
            client.Dispose();
        }
    }
}