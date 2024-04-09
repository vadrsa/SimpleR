using BenchmarkDotNet.Attributes;
using System.IO.Pipelines;

namespace SimpleR.Ocpp.Benchmarks;
[MemoryDiagnoser]
public class ProtocolWriterBenchmarks
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private OcppMessageProtocol _ocppMessageProtocol;
    private OcppCall _call;
    private OcppCallResult _callResult;
    private OcppCallError _callError;
    private TestBufferWriter _bufferWriter;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [GlobalSetup]
    public void Setup()
    {
        _bufferWriter = new TestBufferWriter();
        _ocppMessageProtocol = new OcppMessageProtocol();
        _call = new OcppCall("123", "Action", "{}");
        _callResult = new OcppCallResult("123", "{}");
        _callError = new OcppCallError("123", "{}", "code","details");
    }
    
    [Benchmark]
    public void WriteMessage_Call()
    {
        _ocppMessageProtocol.WriteMessage(_call, _bufferWriter);
    }
    
    [Benchmark]
    public void WriteMessage_CallError()
    {
        _ocppMessageProtocol.WriteMessage(_callError, _bufferWriter);
    }
    
    [Benchmark]
    public void WriteMessage_CallResult()
    {
        _ocppMessageProtocol.WriteMessage(_callResult, _bufferWriter);
    }
}