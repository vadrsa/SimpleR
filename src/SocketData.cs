using System.Buffers;
using System.Text;
using System.Text.Json;

namespace SimpleR;

public struct IncomingSocketData
{
    public IncomingSocketData(ReadOnlySequence<byte> buffer)
    {
        Buffer = buffer;
    }

    internal ReadOnlySequence<byte> Buffer { get; }

    public T? Deserialize<T>(JsonSerializerOptions? options = null)
    {
        var reader = new Utf8JsonReader(Buffer);

        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public string ParseUtf8String() => Encoding.UTF8.GetString(Buffer.ToArray());
}

public abstract class OutboundSocketData
{
    public abstract void Write(IBufferWriter<byte> writer);

    public static OutboundSocketData Json<T>(T data) => new JsonOutboundSocketData<T>(data);
    public static OutboundSocketData Text(string data) => new Utf8TextOutboundSocketData(data);
}

public class JsonOutboundSocketData<T> : OutboundSocketData
{
    public JsonOutboundSocketData(T data)
    {
        Data = data;
    }

    public T Data { get; }

    public override void Write(IBufferWriter<byte> writer)
    {
        var jsonWriter = new Utf8JsonWriter(writer);
        JsonSerializer.Serialize(jsonWriter, Data, typeof(T));
    }
}

public class Utf8TextOutboundSocketData : OutboundSocketData
{
    public Utf8TextOutboundSocketData(string data)
    {
        Data = data;
    }

    public string Data { get; }

    public override void Write(IBufferWriter<byte> writer) => writer.Write(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(Data), 0, Data.Length));
}
