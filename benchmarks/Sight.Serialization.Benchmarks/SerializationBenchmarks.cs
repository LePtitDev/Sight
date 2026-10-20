using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack;
using Sight.Serialization.Data;

namespace Sight.Serialization.Benchmarks;

/// <summary>
/// Same set of benchmarks for each data set: SBDL binary and text (typed and untyped), System.Text.Json and MessagePack
/// </summary>
[Config(typeof(BenchmarkConfig))]
public abstract class SerializationBenchmarks<T> : IDataSizeProvider
    where T : class
{
    private static readonly SerializationOptions s_deflate = new() { Compression = Compression.Deflate };
    private static readonly MessagePackSerializerOptions s_lz4 = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

    private T _value = null!;
    private byte[] _binary = null!;
    private byte[] _binaryDeflate = null!;
    private string _text = null!;
    private DataDocument _document = null!;
    private byte[] _json = null!;
    private byte[] _messagePack = Array.Empty<byte>();
    private byte[] _messagePackLz4 = Array.Empty<byte>();

    protected virtual JsonSerializerOptions JsonOptions { get; } = new();

    protected abstract T CreateValue();

    [GlobalSetup]
    public void Setup()
    {
        _value = CreateValue();
        _binary = BinarySerializer.Write(_value);
        _binaryDeflate = BinarySerializer.Write(_value, s_deflate);
        _text = TextSerializer.Write(_value);
        _document = BinarySerializer.Read(_binary);
        _json = JsonSerializer.SerializeToUtf8Bytes(_value, JsonOptions);
        if (GetType().IsDefined(typeof(NoMessagePackAttribute), true))
            return;

        _messagePack = MessagePackSerializer.Serialize(_value);
        _messagePackLz4 = MessagePackSerializer.Serialize(_value, s_lz4);
    }

    [Benchmark]
    public byte[] Sight_Binary_Write() => BinarySerializer.Write(_value);

    [Benchmark]
    public T Sight_Binary_Read() => BinarySerializer.Read<T>(_binary);

    [Benchmark]
    public byte[] Sight_Binary_Write_Deflate() => BinarySerializer.Write(_value, s_deflate);

    [Benchmark]
    public T Sight_Binary_Read_Deflate() => BinarySerializer.Read<T>(_binaryDeflate, s_deflate);

    [Benchmark]
    public string Sight_Text_Write() => TextSerializer.Write(_value);

    [Benchmark]
    public T Sight_Text_Read() => TextSerializer.Read<T>(_text);

    [Benchmark]
    public DataDocument Sight_Document_Read() => BinarySerializer.Read(_binary);

    [Benchmark]
    public byte[] Sight_Document_Write() => BinarySerializer.Write(_document);

    [Benchmark]
    public DataDocument Sight_Document_ReadText() => TextSerializer.Read(_text);

    [Benchmark]
    public string Sight_Document_WriteText() => TextSerializer.Write(_document);

    [Benchmark]
    public byte[] Json_Write() => JsonSerializer.SerializeToUtf8Bytes(_value, JsonOptions);

    [Benchmark]
    public T? Json_Read() => JsonSerializer.Deserialize<T>(_json, JsonOptions);

    [Benchmark]
    [BenchmarkCategory(BenchmarkConfig.MessagePackCategory)]
    public byte[] MessagePack_Write() => MessagePackSerializer.Serialize(_value);

    [Benchmark]
    [BenchmarkCategory(BenchmarkConfig.MessagePackCategory)]
    public T MessagePack_Read() => MessagePackSerializer.Deserialize<T>(_messagePack);

    [Benchmark]
    [BenchmarkCategory(BenchmarkConfig.MessagePackCategory)]
    public byte[] MessagePack_Write_Lz4() => MessagePackSerializer.Serialize(_value, s_lz4);

    [Benchmark]
    [BenchmarkCategory(BenchmarkConfig.MessagePackCategory)]
    public T MessagePack_Read_Lz4() => MessagePackSerializer.Deserialize<T>(_messagePackLz4, s_lz4);

    public long? GetDataSize(string benchmark)
    {
        return benchmark switch
        {
            nameof(Sight_Binary_Write) or nameof(Sight_Binary_Read) or nameof(Sight_Document_Read) or nameof(Sight_Document_Write) => _binary.Length,
            nameof(Sight_Binary_Write_Deflate) or nameof(Sight_Binary_Read_Deflate) => _binaryDeflate.Length,
            nameof(Sight_Text_Write) or nameof(Sight_Text_Read) or nameof(Sight_Document_ReadText) or nameof(Sight_Document_WriteText) => Encoding.UTF8.GetByteCount(_text),
            nameof(Json_Write) or nameof(Json_Read) => _json.Length,
            nameof(MessagePack_Write) or nameof(MessagePack_Read) => _messagePack.Length,
            nameof(MessagePack_Write_Lz4) or nameof(MessagePack_Read_Lz4) => _messagePackLz4.Length,
            _ => null,
        };
    }
}

/// <summary>
/// Excludes the MessagePack benchmarks of a class (data set not supported by MessagePack)
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class NoMessagePackAttribute : Attribute
{
}

/// <summary>
/// Small object: a few scalars and a short list of strings
/// </summary>
public class SmallObjectBenchmarks : SerializationBenchmarks<Person>
{
    protected override Person CreateValue() => TestData.CreatePerson();
}

/// <summary>
/// List of 10 000 structs of numeric fields
/// </summary>
public class LargeListBenchmarks : SerializationBenchmarks<SampleSet>
{
    protected override SampleSet CreateValue() => TestData.CreateSampleSet();
}

/// <summary>
/// Graph of 1 050 polymorphic entities with shared references and cycles (MessagePack supports neither)
/// </summary>
[NoMessagePack]
public class GraphBenchmarks : SerializationBenchmarks<Scene>
{
    protected override JsonSerializerOptions JsonOptions { get; } = new() { ReferenceHandler = ReferenceHandler.Preserve };

    protected override Scene CreateValue() => TestData.CreateScene();
}
