using System.Collections.Concurrent;

namespace PingPong.Server;

public class ConnectionManager
{
    public readonly ConcurrentBag<string> Connections = new();
    public readonly ConcurrentBag<string> Disconnections = new();
}