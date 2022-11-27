// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Net.WebSockets;
using System.Text;

var client = new ClientWebSocket();
await client.ConnectAsync(new Uri("wss://localhost:7145/custom"), CancellationToken.None);
while (true)
{
    Console.Write("Client> ");
    var message = Console.ReadLine()!.Replace(Environment.NewLine, "");
    var bytes = Encoding.UTF8.GetBytes(message);
    await client.SendAsync(new ArraySegment<byte>(new[] { (byte)bytes.Length }), WebSocketMessageType.Text, false, CancellationToken.None);
    await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, false, CancellationToken.None);

    await DumpIncomingMessages();
}

async Task DumpIncomingMessages()
{
    var rentedSpace = MemoryPool<byte>.Shared.Rent();
    var memory = rentedSpace.Memory;
    await client.ReceiveAsync(memory, CancellationToken.None);
    var length = memory.Span[0];
    Console.WriteLine($"Server> {length}:{Encoding.UTF8.GetString(new ReadOnlySpan<byte>(memory.Slice(1).ToArray()))}");
}