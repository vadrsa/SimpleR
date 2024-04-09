using FluentAssertions;
using System.Buffers;
using System.Text;

namespace SimpleR.Ocpp.Tests;

public class ProtocolReaderTests
{
    private readonly OcppMessageProtocol _ocppMessageProtocol = new();

    [Fact]
    public void ParseMessage_ValidCallMessage_ReturnsOcppCall()
    {
        var json = "[2, \"123\", \"Action\", {}]";
        var input = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));

        var message = _ocppMessageProtocol.ParseMessage(ref input);

        var call = message.Should().BeOfType<OcppCall>().Which;
        call.UniqueId.Should().Be("123");
        call.Action.Should().Be("Action");
        call.JsonPayload.Should().Be("{}");
    }

    [Fact]
    public void ParseMessage_ValidCallResultMessage_ReturnsOcppCallResult()
    {
        var json = "[3, \"123\", {}]";
        var input = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));

        var message = _ocppMessageProtocol.ParseMessage(ref input);

        var callResult = message.Should().BeOfType<OcppCallResult>().Which;
        callResult.UniqueId.Should().Be("123");
        callResult.JsonPayload.Should().Be("{}");
    }

    [Fact]
    public void ParseMessage_ValidCallErrorMessage_ReturnsOcppCallError()
    {
        var json = "[4, \"123\", \"ErrorCode\", \"ErrorDescription\", {}]";
        var input = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));

        var message = _ocppMessageProtocol.ParseMessage(ref input);

        var callError = message.Should().BeOfType<OcppCallError>().Which;
        callError.UniqueId.Should().Be("123");
        callError.ErrorCode.Should().Be("ErrorCode");
        callError.ErrorDescription.Should().Be("ErrorDescription");
        callError.ErrorDetails.Should().Be("{}");
    }

    [Fact]
    public void ParseMessage_InvalidJson_ThrowsBadOcppMessageException()
    {
        var json = "Invalid JSON";
        var input = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));

        Action act = () => _ocppMessageProtocol.ParseMessage(ref input);

        act.Should().Throw<BadOcppMessageException>();
    }

    [Fact]
    public void ParseMessage_InvalidMessageTypeId_ThrowsBadOcppMessageException()
    {
        var json = "[5, \"123\", \"Action\", {}]";
        var input = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));

        Action act = () => _ocppMessageProtocol.ParseMessage(ref input);

        act.Should().Throw<BadOcppMessageException>();
    }
}