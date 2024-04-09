using System.Buffers;
using System.Text;
using System.Text.Json;
using SimpleR.Ocpp.Internal;
using SimpleR.Protocol;

namespace SimpleR.Ocpp;

public class OcppMessageProtocol : IDelimitedMessageProtocol<IOcppMessage>
{
    private readonly int _messageMaxSize;
    private static readonly byte[] ArrayStartBytes = "["u8.ToArray();
    private static readonly byte[] CommaBytes = ","u8.ToArray();
    private static readonly byte[] DoubleQuoteBytes = "\""u8.ToArray();
    private static readonly byte[] ArrayEndBytes = "]"u8.ToArray();
    private static readonly string JsonEmptyObject = "{}";
    
    public OcppMessageProtocol(OcppMessageProtocolOptions options)
    {
        _messageMaxSize = options.MessageMaxSize;
    }
    public OcppMessageProtocol() : this(new())
    {
    }
    public void WriteMessage(IOcppMessage message, IBufferWriter<byte> output)
    {
        switch (message)
        {
            case OcppCall call:
            {
                var jsonPayload = string.IsNullOrEmpty(call.JsonPayload)
                    ? JsonEmptyObject
                    : call.JsonPayload;
                var length = 0;
                var span = output.GetSpan(_messageMaxSize);
                // [
                length += WriteToSpan(ref span, ArrayStartBytes);
                // {MessageTypeId}
                length += WriteToSpan(ref span, OcppCall.MessageTypeIdBytes);
                // ,
                length += WriteToSpan(ref span, CommaBytes);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // {UniqueId}
                length += WriteUtf8ToSpan(call.UniqueId, ref span);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // ,
                length += WriteToSpan(ref span, CommaBytes);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // {Action}
                length += WriteUtf8ToSpan(call.Action, ref span);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // ,
                length += WriteToSpan(ref span, CommaBytes);
                // {JsonPayload}
                length += WriteUtf8ToSpan(jsonPayload, ref span);
                // ]
                length += WriteToSpan(ref span, ArrayEndBytes);
                output.Advance(length);
                break;
            }
            case OcppCallResult callResult:
            {
                var jsonPayload = string.IsNullOrEmpty(callResult.JsonPayload)
                    ? JsonEmptyObject
                    : callResult.JsonPayload;
                var length = 0;
                var span = output.GetSpan(_messageMaxSize);
                // [
                length += WriteToSpan(ref span, ArrayStartBytes);
                // {MessageTypeId}
                length += WriteToSpan(ref span, OcppCallResult.MessageTypeIdBytes);
                // ,
                length += WriteToSpan(ref span, CommaBytes);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // {UniqueId}
                length += WriteUtf8ToSpan(callResult.UniqueId, ref span);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // ,
                length += WriteToSpan(ref span, CommaBytes);
                // {JsonPayload}
                length += WriteUtf8ToSpan(jsonPayload, ref span);
                length += WriteToSpan(ref span, ArrayEndBytes);
                output.Advance(length);
                break;
            }
            case OcppCallError callError:
            {
                var errorDetails = string.IsNullOrEmpty(callError.ErrorDetails)
                    ? JsonEmptyObject
                    : callError.ErrorDetails;
                var length = 0;
                var span = output.GetSpan(_messageMaxSize);
                // [
                length += WriteToSpan(ref span, ArrayStartBytes);
                // {MessageTypeId}
                length += WriteToSpan(ref span, OcppCallError.MessageTypeIdBytes);
                // ,
                length += WriteToSpan(ref span, CommaBytes);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // {UniqueId}
                length += WriteUtf8ToSpan(callError.UniqueId, ref span);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // ,
                length += WriteToSpan(ref span, CommaBytes);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // {ErrorCode}
                length += WriteUtf8ToSpan(callError.ErrorCode, ref span);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // ,
                length += WriteToSpan(ref span, CommaBytes);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // {ErrorDescription}
                length += WriteUtf8ToSpan(callError.ErrorDescription, ref span);
                // "
                length += WriteToSpan(ref span, DoubleQuoteBytes);
                // ,
                length += WriteToSpan(ref span, CommaBytes);
                // {ErrorDetails}
                length += WriteUtf8ToSpan(errorDetails, ref span);
                length += WriteToSpan(ref span, ArrayEndBytes);
                
                output.Advance(length);
                break;
            }
            default:
                throw new InvalidOperationException("A message should be one of call, result or error.");
        }
    }
    
    private int WriteUtf8ToSpan(string? str, ref Span<byte> span)
    {
        var length = Encoding.UTF8.GetBytes(str ?? "", span);

        span = span[length..];
        return length;
    }

    private int WriteToSpan(ref Span<byte> span, byte[] bytes)
    {
        bytes.CopyTo(span);
        span = span[bytes.Length..];

        return bytes.Length;
    }

    public IOcppMessage ParseMessage(ref ReadOnlySequence<byte> input)
    {
        var reader = new Utf8JsonReader(input);
        return OcppMessageParser.Parse(ref reader);
    }
}