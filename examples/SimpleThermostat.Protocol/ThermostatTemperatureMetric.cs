namespace SimpleThermostat.Protocol;

public class ThermostatTemperatureMetric : ThermostatMetric
{
    public ThermostatTemperatureMetric(float temperature)
    {
        Temperature = temperature;
    }

    public float Temperature { get; }
}