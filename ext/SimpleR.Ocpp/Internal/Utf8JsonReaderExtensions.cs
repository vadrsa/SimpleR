using System.Text.Json;

namespace SimpleR.Ocpp.Internal;

public static class Utf8JsonReaderExtensions
{
    public static string GetStringOrFail(this ref Utf8JsonReader reader, string name)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new BadOcppMessageException(OcppErrorCode.PropertyConstraintViolation, $"Expected property '{name}' to be a string.");
        }
        return reader.GetString() ?? throw new BadOcppMessageException(OcppErrorCode.PropertyConstraintViolation, $"Property '{name}' cannot be empty.");
    }
}