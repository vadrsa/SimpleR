using System.Runtime.Serialization;

namespace SimpleR.Ocpp;

public class BadOcppMessageException : Exception
{
    public BadOcppMessageException(OcppErrorCode errorCode) : this(errorCode, "Bad OCPP message format.")
    {
        ErrorCode = errorCode;
    }

    public BadOcppMessageException(OcppErrorCode errorCode, string? message) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public OcppErrorCode ErrorCode { get; }
}
