using System.Collections.Concurrent;

namespace PingPong.Server;

public class ConnectionManager
{
    public ConcurrentBag<string> Connections { get; } = new();
    public ConcurrentBag<string> Disconnections { get; } = new();
    
    public void OnConnected(string connectionId)
    {
        Connections.Add(connectionId);
    }
    
    public void OnDisconnected(string connectionId)
    {
        Disconnections.Add(connectionId);
    }
}