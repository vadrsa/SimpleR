using System.Text.Json.Serialization;

namespace SimpleThermostat.Protocol;


[JsonDerivedType(typeof(ThermostatTemperatureMetric), "temperature")]
public class ThermostatMetric;