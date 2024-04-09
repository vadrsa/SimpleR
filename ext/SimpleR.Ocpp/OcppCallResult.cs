using System.Globalization;

namespace SimpleR.Ocpp;

public readonly struct OcppCallResult : IOcppResponse
{
    public const byte MessageTypeId = 3;
    internal static readonly byte[] MessageTypeIdBytes = "3"u8.ToArray();

    /// <summary>
    /// Constructor
    /// </summary>
    public OcppCallResult(string uniqueId, string jsonPayload)
    {
        UniqueId = uniqueId;
        JsonPayload = jsonPayload;
    }

    /// <summary>
    /// Message ID
    /// </summary>
    public string UniqueId { get; }

    /// <summary>
    /// JSON-Payload
    /// </summary>
    public string JsonPayload { get; }

    public string ToOcppString()
        => string.Format(CultureInfo.InvariantCulture,
            "[{0},\"{1}\",{2}]",
            MessageTypeId,
            UniqueId,
            string.IsNullOrEmpty(JsonPayload)
                ? "{}"
                : JsonPayload);
}
