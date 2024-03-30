using System.Diagnostics;

namespace Websocket.Client;

public static class WaitHelpers
{
    public static async Task WaitFor(Func<bool> condition, int timeoutMs = 10000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
        {
            await Task.Delay(100);
        }
    }
}