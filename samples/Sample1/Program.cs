using Sample1;
using SampleProtocols;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebsocketServer<LengthPrefixedProtocolDispatcher, LengthPrefixedProtocolMessage, LengthPrefixedProtocol>();
builder.Services.AddWebsocketServer<EndOfMessageDelimitedProtocolDispatcher, EndOfMessageDelimitedSampleProtocolMessage, EndOfMessageDelimitedSampleProtocol>();

var app = builder.Build();

app.MapGet("/", () => "Hello to a SimpleR World!");

app.MapWebsocket<LengthPrefixedProtocolDispatcher, LengthPrefixedProtocolMessage>("/lengthPrefixed");
app.MapWebsocket<EndOfMessageDelimitedProtocolDispatcher, EndOfMessageDelimitedSampleProtocolMessage>("/endOfMessage");

app.Run();