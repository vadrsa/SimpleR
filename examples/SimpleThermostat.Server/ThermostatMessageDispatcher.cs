using System.Security.Claims;
using SimpleR;
using SimpleThermostat.Protocol;

namespace SimpleThermostat.Server;

public class ThermostatMessageDispatcher : IWebSocketMessageDispatcher<ThermostatMetric, ThermostatCommand>
{
    private readonly ThermostatConnectionManager _connectionManager;
    private readonly ThermostatRepository _repository;

    public ThermostatMessageDispatcher(ThermostatConnectionManager connectionManager, ThermostatRepository repository)
    {
        _connectionManager = connectionManager;
        _repository = repository;
    }

    public Task OnConnectedAsync(IWebsocketConnectionContext<ThermostatCommand> connection)
    {
        var deviceId = connection.User.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("Current user is not a device");
        _connectionManager.AddConnection(deviceId, connection);
        return Task.CompletedTask;
    }

    public Task OnDisconnectedAsync(IWebsocketConnectionContext<ThermostatCommand> connection, Exception? exception)
    {
        var deviceId = connection.User.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("Current user is not a device");
        _connectionManager.RemoveConnection(deviceId);
        return Task.CompletedTask;
    }

    public async Task DispatchMessageAsync(IWebsocketConnectionContext<ThermostatCommand> connection, ThermostatMetric message)
    {
        var deviceId = connection.User.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("Current user is not a device");
        var state = _repository.GetState(deviceId);
        var settings = _repository.GetSettings(deviceId);
        if(message is ThermostatTemperatureMetric temperatureMetric)
        {
            // update temperature
            state.CurrentTemperature = temperatureMetric.Temperature;
            
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