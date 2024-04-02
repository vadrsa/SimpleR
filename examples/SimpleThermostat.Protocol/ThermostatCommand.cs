using System.Text.Json.Serialization;

namespace SimpleThermostat.Protocol;

[JsonDerivedType(typeof(SetThermostatModeCommand), "setMode")]
public class ThermostatCommand;