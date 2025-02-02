using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc.Testing;
using Websocket.Client;

namespace PingPong.Server.Tests.Binary;

public class PingPongDelimitedBinaryProtocolTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _route;

    public PingPongDelimitedBinaryProtocolTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _route = "/ping/binary/eom";
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
            await socket.SendAsync(new ArraySegment<byte>(new byte[] { 5 }), WebSocketMessageType.Binary, true,
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
            using var sub = client.MessageReceived.Subscribe((message) => { receivedQueue.Add(message.Binary); });

            await socket.SendAsync(new ArraySegment<byte>(new byte[] { 1, 2, 3, 4, 5 }), WebSocketMessageType.Binary,
                true, CancellationToken.None);
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
    public async Task SendEmptyMessage_GetReversed()
    {
        // Arrange
        var receivedQueue = new List<byte[]?>();
        var (client, socket) = await _factory.Server.ConnectWebsocketAsync(_route);
        try
        {
            using var sub = client.MessageReceived.Subscribe((message) => { receivedQueue.Add(message.Binary); });

            await socket.SendAsync(ArraySegment<byte>.Empty, WebSocketMessageType.Binary,
                true, CancellationToken.None);
            await WaitHelpers.WaitFor(() => receivedQueue.Any());

            receivedQueue.Should().HaveCount(1);
            receivedQueue[0].Should().BeEmpty();
        }
        finally
        {
            client.Dispose();
        }
    }
}