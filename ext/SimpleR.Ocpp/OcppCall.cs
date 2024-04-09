using System.Diagnostics;
using System.Globalization;

namespace SimpleR.Ocpp;

[DebuggerDisplay("OCPPCall - {{Action}}")]
public class OcppCall : IOcppMessage
{
    public const byte MessageTypeId = 2;
    internal static readonly byte[] MessageTypeIdBytes = "2"u8.ToArray();

    /// <summary>
    /// Create an Ocpp call
    /// </summary>
    public OcppCall(string uniqueId, string action, string jsonPayload)
    {
        UniqueId = uniqueId;
        Action = action;
        JsonPayload = jsonPayload;
    }

    /// <summary>
    /// Message ID
    /// </summary>
    public string UniqueId { get; set; }

    /// <summary>
    /// Action
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    /// JSON-Payload
    /// </summary>
    public string JsonPayload { get; set; }
    
    public string ToOcppString()
        => string.Format(CultureInfo.InvariantCulture,
            "[{0},\"{1}\",\"{2}\",{3}]",
            MessageTypeId,
            UniqueId,
            Action,
            string.IsNullOrEmpty(JsonPayload)
                ? "{}"
                : JsonPayload);
}
