namespace SimpleThermostat.Protocol;

public class SetThermostatModeCommand : ThermostatCommand
{
    public SetThermostatModeCommand(ThermostatMode mode)
    {
        Mode = mode;
    }

    public ThermostatMode Mode { get; }
}