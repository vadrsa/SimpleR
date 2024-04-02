using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SimpleR;
using SimpleThermostat.Protocol;

namespace SimpleThermostat.Server;

public class ThermostatConnectionManager
{
    private readonly ILogger<ThermostatConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, IWebsocketConnectionContext<ThermostatCommand>> _connections = new();
    
    public ThermostatConnectionManager(ILogger<ThermostatConnectionManager> logger)
    {
        _logger = logger;
    }
    
    public void AddConnection(string deviceId, IWebsocketConnectionContext<ThermostatCommand> connection)
    {
        if (!_connections.TryAdd(deviceId, connection))
        {
            throw new InvalidOperationException("Device is already connected");
        }
        
        _logger.LogInformation("Device connected: {deviceId}", deviceId);
    }
    
    public bool TryGetConnection(string deviceId, [NotNullWhen(true)]out IWebsocketConnectionContext<ThermostatCommand>? connection)
    {
        return _connections.TryGetValue(deviceId, out connection);
    }
    
    public void RemoveConnection(string deviceId)
    {
        _connections.TryRemove(deviceId, out _);
        _logger.LogInformation("Device disconnected: {deviceId}", deviceId);
    }
}