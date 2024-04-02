using System.Collections.Concurrent;

namespace SimpleThermostat.Server;

public class ThermostatRepository
{
    private ConcurrentDictionary<string, (ThermostatState State, ThermostatSettings Settings)> _thermostats = new();
    
    public ThermostatState GetState(string deviceId)
    {
        return _thermostats.GetOrAdd(deviceId, _ => (new ThermostatState(), new ThermostatSettings())).State;
    }
    
    public ThermostatSettings GetSettings(string deviceId)
    {
        return _thermostats.GetOrAdd(deviceId, _ => (new ThermostatState(), new ThermostatSettings())).Settings;
    }
}
public class ThermostatSettings
{
    public float TargetTemperature { get; set; } = 22;
}
public class ThermostatState
{
    public float? CurrentTemperature { get; set; }
}