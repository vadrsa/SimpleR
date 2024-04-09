using System.Globalization;

namespace SimpleR.Ocpp;

public struct OcppCallError : IOcppResponse
{
    public const byte MessageTypeId = 4;
    internal static readonly byte[] MessageTypeIdBytes = "4"u8.ToArray();

    /// <summary>
    /// Constructor
    /// </summary>
    public OcppCallError(string uniqueId,
        string? errorDetails,
        string? errorCode,
        string? errorDescription)
    {
        UniqueId = uniqueId;
        ErrorDetails = errorDetails;
        ErrorCode = errorCode;
        ErrorDescription = errorDescription;
    }

    /// <summary>
    /// Message ID
    /// </summary>
    public string UniqueId { get; set; }

    /// <summary>
    /// Error-Details
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Error-Code
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error-Description
    /// </summary>
    public string? ErrorDescription { get; set; }

    public string ToOcppString()
        => string.Format(CultureInfo.InvariantCulture,
            "[{0},\"{1}\",\"{2}\",\"{3}\",{4}]",
            MessageTypeId,
            UniqueId,
            ErrorCode,
            ErrorDescription,
            string.IsNullOrEmpty(ErrorDetails)
                ? "{}"
                : ErrorDetails);
}
