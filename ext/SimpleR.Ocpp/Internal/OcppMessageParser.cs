using System.Buffers;
using System.Text.Json;
using SimpleR.Ocpp.Internal;

namespace SimpleR.Ocpp.Internal;

internal static class OcppMessageParser
{
    public static IOcppMessage Parse(ref Utf8JsonReader reader)
    {
        try
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                throw new BadOcppMessageException(OcppErrorCode.FormationViolation, "Expected an array.");
        }
        catch (JsonException ex)
        {
            throw new BadOcppMessageException(OcppErrorCode.FormationViolation, ex.Message);
        }

        reader.Read(); // Move to message type
        var messageType = reader.GetInt32();

        if (messageType is < 2 or > 4)
        {
            throw new BadOcppMessageException(OcppErrorCode.FormationViolation,
                "Expected message type to be 2, 3 or 4.");
        }

        reader.Read(); // Move to messageId
        var messageId = reader.GetStringOrFail("UniqueId");
        var action = string.Empty;
        var errorCode = string.Empty;
        var errorDescription = string.Empty;
        string payload;

        switch (messageType)
        {
            // Call
            case 2:
                reader.Read(); // Move to action
                action = reader.GetStringOrFail("Action");
                break;
            // CallError
            case 4:
                reader.Read(); // Move to errorCode
                errorCode = reader.GetStringOrFail("ErrorCode");
                reader.Read(); // Skip error description
                errorDescription = reader.GetStringOrFail("ErrorDescription");
                break;
        }

        // For both Call and CallResult, the payload is next.
        // For CallError, details are treated as payload.
        reader.Read();
        payload = GetPayloadAsString(ref reader);

        return messageType switch
        {
            2 => new OcppCall(messageId, action, payload),
            3 => new OcppCallResult(messageId, payload),
            _ => new OcppCallError(messageId, payload, errorCode, errorDescription)
        };

    }
    
    private static string GetPayloadAsString(ref Utf8JsonReader reader)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        return jsonDoc.RootElement.GetRawText();
    }
}
