using System.Buffers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SimpleR.Protocol;

namespace SimpleR.Ocpp.Tests;

public class ProtocolWriterTests
{
    private readonly IMessageWriter<IOcppMessage> _ocppMessageProtocol = new OcppMessageProtocol();
    private readonly ArrayBufferWriter<byte> _bufferWriter = new();

    [Fact]
    public void WriteMessage_WritesOcppCallCorrectly()
    {
        var ocppCall = new OcppCall("123", "SomeAction", "{}");

        _ocppMessageProtocol.WriteMessage(ocppCall, _bufferWriter);

        var writtenBytes = _bufferWriter.WrittenSpan;
        var writtenString = Encoding.UTF8.GetString(writtenBytes);

        var jsonArray = JsonSerializer.Deserialize<JsonElement>(writtenString);

        jsonArray[0].GetRawText().Should().Be("2");
        jsonArray[1].GetString().Should().Be(ocppCall.UniqueId);
        jsonArray[2].GetString().Should().Be(ocppCall.Action);
        jsonArray[3].GetRawText().Should().Be(ocppCall.JsonPayload);
    }

    [Fact]
    public void WriteMessage_WritesOcppCallResultCorrectly()
    {
        var ocppCallResult = new OcppCallResult("123", "{}");

        _ocppMessageProtocol.WriteMessage(ocppCallResult, _bufferWriter);

        var writtenBytes = _bufferWriter.WrittenSpan;
        var writtenString = Encoding.UTF8.GetString(writtenBytes);

        var jsonArray = JsonSerializer.Deserialize<JsonElement>(writtenString);

        jsonArray[0].GetRawText().Should().Be("3");
        jsonArray[1].GetString().Should().Be(ocppCallResult.UniqueId);
        jsonArray[2].GetRawText().Should().Be(ocppCallResult.JsonPayload);
    }

    [Fact]
    public void WriteMessage_WritesOcppCallErrorCorrectly()
    {
        var ocppCallError = new OcppCallError("123", "{}", "SomeCode", "SomeDescription");

        _ocppMessageProtocol.WriteMessage(ocppCallError, _bufferWriter);

        var writtenBytes = _bufferWriter.WrittenSpan;
        var writtenString = Encoding.UTF8.GetString(writtenBytes);

        var jsonArray = JsonSerializer.Deserialize<JsonElement>(writtenString);

        jsonArray[0].GetRawText().Should().Be("4");
        jsonArray[1].GetString().Should().Be(ocppCallError.UniqueId);
        jsonArray[2].GetString().Should().Be(ocppCallError.ErrorCode);
        jsonArray[3].GetString().Should().Be(ocppCallError.ErrorDescription);
        jsonArray[4].GetRawText().Should().Be(ocppCallError.ErrorDetails);
    }

    [Fact]
    public void WriteMessage_WritesLargeOcppCallCorrectly()
    {
        var largeAction = new string('a', 10000); // Large JSON payload
        var ocppCall = new OcppCall("123", largeAction, "{}");

        _ocppMessageProtocol.WriteMessage(ocppCall, _bufferWriter);

        var writtenBytes = _bufferWriter.WrittenSpan;
        var writtenString = Encoding.UTF8.GetString(writtenBytes);

        var jsonArray = JsonSerializer.Deserialize<JsonElement>(writtenString);

        jsonArray[0].GetRawText().Should().Be("2");
        jsonArray[1].GetString().Should().Be(ocppCall.UniqueId);
        jsonArray[2].GetString().Should().Be(ocppCall.Action);
        jsonArray[3].GetRawText().Should().Be(ocppCall.JsonPayload);
    }

    [Fact]
    public void WriteMessage_WritesLargeOcppCallResultCorrectly()
    {
        var largeJsonPayload = "{ \"key\": \"" + new string('a', 10000) + "\"}"; // Large JSON payload
        var ocppCallResult = new OcppCallResult("123", largeJsonPayload);

        _ocppMessageProtocol.WriteMessage(ocppCallResult, _bufferWriter);

        var writtenBytes = _bufferWriter.WrittenSpan;
        var writtenString = Encoding.UTF8.GetString(writtenBytes);

        var jsonArray = JsonSerializer.Deserialize<JsonElement>(writtenString);

        jsonArray[0].GetRawText().Should().Be("3");
        jsonArray[1].GetString().Should().Be(ocppCallResult.UniqueId);
        jsonArray[2].GetRawText().Should().Be(ocppCallResult.JsonPayload);
    }

    [Fact]
    public void WriteMessage_WritesLargeOcppCallErrorCorrectly()
    {
        var largeDescription = new string('a', 10000); // Large error details
        var ocppCallError = new OcppCallError("123", "{}", "SomeCode", largeDescription);

        _ocppMessageProtocol.WriteMessage(ocppCallError, _bufferWriter);

        var writtenBytes = _bufferWriter.WrittenSpan;
        var writtenString = Encoding.UTF8.GetString(writtenBytes);

        var jsonArray = JsonSerializer.Deserialize<JsonElement>(writtenString);

        jsonArray[0].GetRawText().Should().Be("4");
        jsonArray[1].GetString().Should().Be(ocppCallError.UniqueId);
        jsonArray[2].GetString().Should().Be(ocppCallError.ErrorCode);
        jsonArray[3].GetString().Should().Be(ocppCallError.ErrorDescription);
        jsonArray[4].GetRawText().Should().Be(ocppCallError.ErrorDetails);
    }
}