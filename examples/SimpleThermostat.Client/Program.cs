using System.Text.Json;
using SimpleThermostat.Protocol;
using Websocket.Client;

using var websocketClient = new WebsocketClient(new Uri("wss://localhost:7278/thermostat/1234/socket"));

await websocketClient.Start();

websocketClient.MessageReceived.Subscribe(msg =>
{
    var command = JsonSerializer.Deserialize<ThermostatCommand>(msg.Text!);
    if (command is SetThermostatModeCommand setModeCommand)
    {
        Console.WriteLine($"Thermostat mode set to: {setModeCommand.Mode}");
    }
});

while (true)
{
    Console.WriteLine("Q: What is the current temperature? ");
    Console.Write("A: ");
    var line = Console.ReadLine();
    if (float.TryParse(line, out var temperature))
    {
        var message = new ThermostatTemperatureMetric(temperature);
        var json = JsonSerializer.Serialize<ThermostatMetric>(message);
        websocketClient.Send(json);

        Console.ReadLine();
    }
    else
    {
        Console.WriteLine("Invalid temperature value");
    }
    
}