using System.Buffers;
using System.Text.Json;
using SimpleR.Protocol;

namespace SimpleThermostat.Protocol;

public class ThermostatMessageReader : IDelimitedMessageReader<ThermostatMetric>
{
    public ThermostatMetric ParseMessage(ref ReadOnlySequence<byte> input)
    {
        var jsonReader = new Utf8JsonReader(input);

        return JsonSerializer.Deserialize<ThermostatMetric>(ref jsonReader)!;
    }
}