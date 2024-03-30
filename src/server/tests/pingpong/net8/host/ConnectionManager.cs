using System.Collections.Concurrent;

namespace PingPongNet8Server;

public class ConnectionManager
{
    public readonly ConcurrentBag<string> Connections = new();
    public readonly ConcurrentBag<string> Disconnections = new();
}