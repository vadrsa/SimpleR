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

    protected BadOcppMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ErrorCode = (OcppErrorCode)(info.GetValue("errorCode", typeof(OcppErrorCode)) ?? OcppErrorCode.NotSupported);
    }

    public OcppErrorCode ErrorCode { get; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("errorCode", ErrorCode);
        base.GetObjectData(info, context);
    }
}
