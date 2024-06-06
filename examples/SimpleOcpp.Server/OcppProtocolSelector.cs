namespace SimpleOcpp.Server;

public static class OcppProtocolSelector
{
    public static Func<IList<string>, string> Create(params string[] protocols)
    {
        return available =>
        {
            foreach (var protocol in protocols)
            {
                if (available.Contains(protocol))
                {
                    return protocol;
                }
            }

            throw new InvalidOperationException("No available protocol matched the enforced ocpp protocol");
        };
    }
}