using System.Buffers;
using System.Text.Json;
using SimpleR.Protocol;

namespace SimpleThermostat.Protocol;

public class ThermostatMessageWriter : IMessageWriter<ThermostatCommand>
{
    public void WriteMessage(ThermostatCommand message, IBufferWriter<byte> output)
    {
        var jsonWriter = new Utf8JsonWriter(output);
        JsonSerializer.Serialize(jsonWriter, message);
    }
}