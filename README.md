<!----------------------------------- Heading Section ------------------------------------>
<p align="center">

![GitHub Cover](https://repository-images.githubusercontent.com/506986482/2795e4c7-8f81-466d-b52c-0abd42c7894e)

</p>

# SimpleR [![NuGet Version](https://img.shields.io/nuget/v/SimpleR.Server)](https://www.nuget.org/packages/SimpleR.Server)
SimpleR is a streamlined version of [SignalR](https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/README.md), a high-performance, opinionated, real-time web framework. By removing all the custom protocols from SignalR, we are left with a simpler library, hence the name SimpleR.

# When should I use SimpleR?
In short, If you can use SignalR, you should. If not, go with SimpleR.

SimpleR was created to address the need for an easy-to-use, high-performance WebSocket server on .NET, particularly in scenarios where the client cannot use SignalR. For instance, when the client is an IoT device operating with a specific protocol standard over which you have no control ([OCPP](https://openchargealliance.org) for example), SignalR may not be an option. In such cases, you're often left with very low-level programming APIs. SimpleR aims to solve this problem by providing simpler and more familiar APIs to expedite your high-performance WebSocket server development.

# Standard Protocols
- OCPP [![NuGet Version](https://img.shields.io/nuget/v/SimpleR.Ocpp)](https://www.nuget.org/packages/SimpleR.Ocpp)

# Examples
Examples can be found [here](https://github.com/vadrsa/SimpleR/tree/master/examples)

# Getting Started
SimpleR can be installed using the Nuget package manager or the `dotnet` CLI.

```
dotnet add package SimpleR.Server --prerelease
```

## Configure SimpleR
Here is a simple configuration example.
```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSimpleR();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapSimpleR<ThermostatMetric, ThermostatCommand>("thermostat/{deviceId}", b =>
{
    b.UseDispatcher<ThermostatMessageDispatcher>()
        .UseEndOfMessageDelimitedProtocol(new ThermostatMessageProtocol());
})
.RequireAuthorization();

app.Run();
```
The preceding code adds SimpleR to the ASP.NET Core dependency injections, routing systems and defines the message protocol and the message dispatcher.
## Create a Message Dispatcher
A message dispatcher is a high-level pipeline that encapsulates the logic of where to dispatch connection messages.
```cs
public class ThermostatMessageDispatcher : IWebSocketMessageDispatcher<ThermostatMetric, ThermostatCommand>
{
    
    /// <summary>
    /// Called when a connection is established.
    /// </summary>
    /// <param name="connection">The connection.</param>
    public Task OnConnectedAsync(IWebsocketConnectionContext<ThermostatCommand> connection)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a connection is disconnected.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="exception">The exception that occurred, if any.</param>
    public Task OnDisconnectedAsync(IWebsocketConnectionContext<ThermostatCommand> connection, Exception? exception)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches a message to the application.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="message">The message to dispatch.</param>
    public async Task DispatchMessageAsync(IWebsocketConnectionContext<ThermostatCommand> connection, ThermostatMetric message)
    {
        var deviceId = connection.User.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("Current user is not a device");
        var settings = GetSettings(deviceId);
        if(message is ThermostatTemperatureMetric temperatureMetric)
        {
            if (temperatureMetric.Temperature < settings.TargetTemperature)
            {
                // If the temperature is below the target temperature, set the thermostat to heat mode
                await connection.WriteAsync(new SetThermostatModeCommand(ThermostatMode.Heat));
            }
            else if (temperatureMetric.Temperature > settings.TargetTemperature)
            {
                // If the temperature is above the target temperature, set the thermostat to cool mode
                await connection.WriteAsync(new SetThermostatModeCommand(ThermostatMode.Cool));
            }
            else
            {
                // If the temperature is at the target temperature, turn off the thermostat
                await connection.WriteAsync(new SetThermostatModeCommand(ThermostatMode.Off));
            }
        }
    }
}
```
Each SimpleR route has one message dispatcher instance.
## Defining Message Protocols
Since SimpleR is protocol-agnostic, it requires the user to provide a protocol definition to be able to construct a message from the stream of bytes each connection receives.
There are two categories of a message protocol:
- Messages are delimited by the [EndOfMessage flag of WebsocketReceiveResult](https://learn.microsoft.com/en-Us/dotnet/api/system.net.websockets.websocketreceiveresult)
- Messages have custom delimiters
### EndOfMessage Delimited Protocol
Here is a simple delimited protocol implementation:
```cs
public class ThermostatMessageProtocol: IDelimitedMessageProtocol<ThermostatMetric, ThermostatCommand>
{
    public ThermostatMetric ParseMessage(ref ReadOnlySequence<byte> input)
    {
        var jsonReader = new Utf8JsonReader(input);

        return JsonSerializer.Deserialize<ThermostatMetric>(ref jsonReader)!;
    }
    
    public void WriteMessage(ThermostatCommand message, IBufferWriter<byte> output)
    {
        var jsonWriter = new Utf8JsonWriter(output);
        JsonSerializer.Serialize(jsonWriter, message);
    }
}
```
To use the delimited protocol call the `UseEndOfMessageDelimitedProtocol` method of the builder.
```cs
app.MapSimpleR<ThermostatMetric, ThermostatCommand>("thermostat/{deviceId}", b =>
{
    b.UseDispatcher<ThermostatMessageDispatcher>()
        .UseEndOfMessageDelimitedProtocol(new ThermostatMessageProtocol());
})
```
### Custom Protocol
Here is a simple custom protocol implementation:
```cs
public class ChatMessageProtocol : IMessageProtocol<ChatMessage>
{

    public void WriteMessage(ChatMessage message, IBufferWriter<byte> output)
    {
        var span = output.GetSpan(Encoding.UTF8.GetByteCount(message.Content));

        var bytesWritten = Encoding.UTF8.GetBytes(message.Content, span);

        output.Advance(bytesWritten);
    }

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, out ChatMessage message)
    {
        var reader = new SequenceReader<byte>(input);

        if (reader.TryReadTo(out ReadOnlySequence<byte> payload, delimiter: 0, advancePastDelimiter: true))
        {
            message = new ChatMessage { Content = Encoding.UTF8.GetString(payload) };
            input = reader.UnreadSequence;
            return true;
        }

        message = default;
        return false;
    }
}
```
To use the delimited protocol call the `UseCustomProtocol` method of the builder.
```cs
app.MapSimpleR<ChatMessage>("/chat",
    b =>
    {
        b.UseCustomProtocol(new ChatMessageProtocol())
        .UseDispatcher<ChatMessageDispatcher>();
    }
);
```
### How to work with low level network buffers
To learn more about working with `ReadOnlySequence<T>` and `IBufferWriter<T>` check out [this article](https://learn.microsoft.com/en-us/dotnet/standard/io/buffers).
