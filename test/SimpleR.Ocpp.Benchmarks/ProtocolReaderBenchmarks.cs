using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;

namespace SimpleR.Ocpp.Benchmarks;
[MemoryDiagnoser]
public class ProtocolReaderBenchmarks
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private OcppMessageProtocol _ocppMessageProtocol;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ReadOnlySequence<byte> _call;
    private ReadOnlySequence<byte> _result;
    private ReadOnlySequence<byte> _error;

    [GlobalSetup]
    public void Setup()
    {
        _ocppMessageProtocol = new OcppMessageProtocol();
        _call = new ReadOnlySequence<byte>("[2, \"123\", \"Action\", {}]"u8.ToArray());
        _result = new ReadOnlySequence<byte>("[3, \"123\", {}]"u8.ToArray());
        _error = new ReadOnlySequence<byte>("[4, \"123\", \"ErrorCode\", \"ErrorDescription\", {}]"u8.ToArray());
    }

    [Benchmark]
    public void ParseMessage_Call()
    {
        _ocppMessageProtocol.ParseMessage(ref _call);
    }
    
    [Benchmark]
    public void ParseMessage_Result()
    {
        _ocppMessageProtocol.ParseMessage(ref _result);
    }
    
    [Benchmark]
    public void ParseMessage_Error()
    {
        _ocppMessageProtocol.ParseMessage(ref _error);
    }
}