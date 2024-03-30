using System.Collections.Concurrent;

namespace PingPongNet6Server;

public class ConnectionManager
{
    public readonly ConcurrentBag<string> Connections = new();
    public readonly ConcurrentBag<string> Disconnections = new();
}