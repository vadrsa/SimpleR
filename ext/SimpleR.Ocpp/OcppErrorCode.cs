namespace SimpleR.Ocpp;

/// <summary>
/// Defined OCPP error codes
/// </summary>
public enum OcppErrorCode
{
    /// <summary>
    /// Requested Action is not known by receiver
    /// </summary>
    NotImplemented,

    /// <summary>
    /// Requested Action is recognized but not supported by the receiver
    /// </summary>
    NotSupported,

    /// <summary>
    /// InternalError An internal error occurred and the receiver was not able to process the requested Action successfully
    /// </summary>
    InternalError,

    /// <summary>
    /// Payload for Action is incomplete
    /// </summary>
    ProtocolError,

    /// <summary>
    /// During the processing of Action a security issue occurred preventing receiver from completing the Action successfully
    /// </summary>
    SecurityError,

    /// <summary>
    /// Payload for Action is syntactically incorrect or not conform the PDU structure for Action
    /// </summary>
    FormationViolation,

    /// <summary>
    /// Payload is syntactically correct but at least one field contains an invalid value
    /// </summary>
    PropertyConstraintViolation,

    /// <summary>
    /// Payload for Action is syntactically correct but at least one of the fields violates occurence constraints
    /// </summary>
    OccurenceConstraintViolation,

    /// <summary>
    ///  Payload for Action is syntactically correct but at least one of the fields violates data type constraints(e.g. “somestring”: 12)
    /// </summary>
    TypeConstraintViolation,

    /// <summary>
    /// Any other error not covered by the previous ones
    /// </summary>
    GenericError
}
