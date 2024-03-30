# SimpleR
SimpleR is a streamlined version of [SignalR](https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/README.md), a high-performance, opinionated, real-time web framework. By removing all the custom protocols from SignalR, we are left with a simpler library, hence the name SimpleR.

# Why SimpleR?
SimpleR was created to address the need for an easy-to-use, high-performance WebSocket server on .NET, particularly in scenarios where the client cannot use SignalR. For instance, when the client is an IoT device operating with a specific protocol standard over which you have no control(OCPP for example), SignalR may not be an option. In such cases, you're often left with very low-level programming APIs. SimpleR aims to solve this problem by providing simpler and more familiar APIs to expedite your high-performance WebSocket server development.
In SimpleR terms, if you can use SignalR you should, if not go with SimpleR.

# Getting Started
Since SimpleR is protocol-agnostic, it requires the user to provide a protocol definition to be able to construct a message from the stream of bytes each connection receives. This protocol definition is provided by implementing the `IMessageProtocol<TMessage>` interface.
There are two important categories of protocols, the first category can parse messages from just the raw bytes received by the websocket connection, the second category can only parse byes that are delimited by the [EndOfMessage flag of WebsocketReceiveResult](https://learn.microsoft.com/en-Us/dotnet/api/system.net.websockets.websocketreceiveresult).
## Raw Protocols
For the first category, the protocol class can directly implement `IMessageProtocol<TMessage>`. Or if the protocol is a delimited prtocol the protocol class can inherit from the `DelimitedProtocol<TMessage>` for convinience.

### Example

```cs
public class LengthPrefixedProtocolMessage
{
    public LengthPrefixedProtocolMessage(string content)
    {
        Content = content;
    }

    public string Content { get; }
}

public class LengthPrefixedProtocol : IMessageProtocol<LengthPrefixedProtocolMessage>
{
    public bool TryParseMessage(ref ReadOnlySequence<byte> input, [NotNullWhen(true)]out LengthPrefixedProtocolMessage? message)
    {
        var length = (int)input.FirstSpan[0];
        input = input.Slice(1, input.Length - 1);
        
        if (input.Length <= length)
        {
            message = new LengthPrefixedProtocolMessage(Encoding.UTF8.GetString(input.Slice(0, length)));
            input = input.Slice(length);
            return true;
        }
        
        message = default;
        return false;
    }

    public void WriteMessage(LengthPrefixedProtocolMessage message, IBufferWriter<byte> output)
    {
        var span = output.GetSpan(1);
        var bytes = Encoding.UTF8.GetBytes(message.Content);
        if (bytes.Length > byte.MaxValue)
        {
            throw new NotSupportedException();
        }
        var length = (byte)bytes.Length;
        span[0] = length;
        output.Advance(1);
        span = output.GetSpan(length);
        bytes.CopyTo(span);
        output.Advance(length);
    }
}
```

## EndOfMessage Delimited Protocols
For the second category, the protocol must inherit `EndOfMessageDelimitedProtocol<TMessage>` so that SimpleR can detect and do the necessary transformations to the transport bytes(append the delimited when EndOfMessage is received)

### Example
```cs
public class EndOfMessageDelimitedSampleProtocolMessage
{
    public EndOfMessageDelimitedSampleProtocolMessage(string content)
    {
        Content = content;
    }

    public string Content { get; }
}

public class EndOfMessageDelimitedSampleProtocol : EndOfMessageDelimitedProtocol<EndOfMessageDelimitedSampleProtocolMessage>
{
    // this sequence of bytes is guaranteed to not occur in the protocol bytes
    protected override byte[] Delimiter { get; } = { 0xFF, 0xFF };

    protected override EndOfMessageDelimitedSampleProtocolMessage ParseMessage(ReadOnlySpan<byte> span)
    {
        return new EndOfMessageDelimitedSampleProtocolMessage(Encoding.UTF8.GetString(span));
    }

    protected override void WriteMessageCore(EndOfMessageDelimitedSampleProtocolMessage message, IBufferWriter<byte> output)
    {
        var bytes = Encoding.UTF8.GetBytes(message.Content);
        var memory = output.GetMemory(bytes.Length);
        bytes.CopyTo(memory);
        output.Advance(bytes.Length);
    }
}
```

# How to use SimpleR in aspnet core

```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebsocketServer<CustomDispatcher, CustomMessage, CustomProtocol>();

var app = builder.Build();

// ...
app.MapWebsocket<CustomDispatcher, CustomMessage>("/custom");
// ...

app.Run();
```
