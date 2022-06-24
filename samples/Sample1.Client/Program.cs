// See https://aka.ms/new-console-template for more information

using System.Net.WebSockets;
using SampleProtocols;
using SimpleR.Client;

var client = new ClientWebSocket();
await client.ConnectAsync(new Uri("wss://localhost:7145/endOfMessage"), CancellationToken.None);

await using var simpleClient = SimpleClientBuilder<EndOfMessageDelimitedSampleProtocolMessage>.Create(client)
    .WithProtocol(new EndOfMessageDelimitedSampleProtocol())
    .Build();

simpleClient.OnMessage += (message) =>
{
    Console.WriteLine($"Server> {message.Content}");
};

while(true)
{
    Console.Write("Client> ");
    var message = Console.ReadLine()!;
    await simpleClient.SendAsync(new EndOfMessageDelimitedSampleProtocolMessage(message));
    await Task.Delay(1000);
}