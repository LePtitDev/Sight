using System.Text.Json;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Sight.Tokenize.Benchmarks.Legacy;

namespace Sight.Tokenize.Benchmarks;

/// <summary>
/// Configuration of the tokenize benchmarks: short job and allocations
/// </summary>
public sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default.WithLaunchCount(1).WithWarmupCount(1).WithIterationCount(5));
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}

/// <summary>
/// JSON of some MB: the 0.1.x <c>JsonTokenizer</c> (baseline), <see cref="Grammars.Json"/> and <c>Utf8JsonReader</c> (reference)
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class JsonBenchmarks
{
    private const string Item = @"{
    ""firstName"": ""John"",
    ""lastName"": ""Smith"",
    ""isAlive"": true,
    ""age"": 27,
    ""height"": 1.85,
    ""address"": { ""streetAddress"": ""21 2nd Street"", ""city"": ""New York"", ""state"": ""NY"", ""postalCode"": ""10021-3100"" },
    ""phoneNumbers"": [ { ""type"": ""home"", ""number"": ""212 555-1234"" }, { ""type"": ""office"", ""number"": ""646 555-4567"" } ],
    ""children"": [ ""Catherine"", ""Thomas"", ""Trevor"" ],
    ""spouse"": null
}";

    private string _json = null!;
    private byte[] _utf8 = null!;
    private Lexer<JsonKind> _lexer = null!;
    private JsonTokenizer _legacy = null!;

    [GlobalSetup]
    public void Setup()
    {
        // About 2.4 MB (the size measured in the 0.1.1 reference)
        _json = "[" + string.Join(",\n", Enumerable.Repeat(Item, 3700)) + "]";
        _utf8 = Encoding.UTF8.GetBytes(_json);
        _lexer = Grammars.Json;
        _legacy = new JsonTokenizer();

        var count = _lexer.Tokenize(_json).Tokens.Count;
        var legacy = _legacy.ReadAsync(_json).GetAwaiter().GetResult();
        if (legacy.Tokens.Count != count || _lexer.Tokenize(_json).HasErrors || !legacy.IsSuccess)
            throw new BenchmarkException($"The implementations don't give the same result ({legacy.Tokens.Count} / {count} tokens)");
    }

    [Benchmark(Baseline = true)]
    public int Legacy_0_1_x() => _legacy.ReadAsync(_json).GetAwaiter().GetResult().Tokens.Count;

    [Benchmark]
    public int NewLexer() => _lexer.Tokenize(_json).Tokens.Count;

    [Benchmark]
    public int Utf8JsonReader()
    {
        var reader = new System.Text.Json.Utf8JsonReader(_utf8);
        var count = 0;
        while (reader.Read())
            count++;

        return count;
    }
}

/// <summary>
/// A document of the <c>DynamicTokenizer</c> README: the 0.1.x tokenizer (baseline) and the equivalent lexer
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class DynamicBenchmarks
{
    private const string Document = @"
[
  <firstName> = 'John';
  <lastName> = 'Smith';
  <isAlive> = TRUE;
  <age> = 27;
  <address> = [
    <streetAddress> = '21 2nd Street';
    <city> = 'New York';
    <postalCode> = '10021-3100'
  ];
  <phoneNumbers> = [
    [ <type> = 'home'; <number> = '212 555-1234' ];
    [ <type> = 'office'; <number> = '646 555-4567' ]
  ];
  <children> = [ 'Catherine'; 'Thomas'; 'Trevor' ];
  <spouse> = NULL
]
";

    private enum Kind
    {
        Whitespace,
        Delimiter,
        Keyword,
        Number,
        Identifier,
        String,
        Error
    }

    private string _text = null!;
    private Lexer<Kind> _lexer = null!;
    private DynamicTokenizer _legacy = null!;

    [GlobalSetup]
    public void Setup()
    {
        _text = string.Concat(Enumerable.Repeat(Document, 1000));
        _lexer = Lexer.Create<Kind>(b => b
            .ErrorKind(Kind.Error)
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Token(Kind.Delimiter, "[").Token(Kind.Delimiter, "]").Token(Kind.Delimiter, "=").Token(Kind.Delimiter, ";")
            .Token(Kind.Number, Rule.Number)
            .Token(Kind.Identifier, Rule.Delimited("<", ">"))
            .Token(Kind.String, Rule.Delimited("'", "'", "\\"))
            .Token(Kind.Keyword, Rule.Identifier)
            .Keywords(Kind.Keyword, "NULL", "TRUE", "FALSE"));
        _legacy = CreateLegacy();

        var count = _lexer.Tokenize(_text).Tokens.Count;
        var legacy = _legacy.ReadAsync(_text).GetAwaiter().GetResult();
        if (legacy.Tokens.Count != count || !legacy.IsSuccess || _lexer.Tokenize(_text).HasErrors)
            throw new BenchmarkException($"The implementations don't give the same result ({legacy.Tokens.Count} / {count} tokens)");
    }

    [Benchmark(Baseline = true)]
    public int Legacy_0_1_x() => _legacy.ReadAsync(_text).GetAwaiter().GetResult().Tokens.Count;

    [Benchmark]
    public int NewLexer() => _lexer.Tokenize(_text).Tokens.Count;

    private static DynamicTokenizer CreateLegacy() => new DynamicTokenizer()
        .IgnoreSymbols(new[] { ' ', '\t', '\r', '\n' })
        .AddSymbol("delimiter", '[')
        .AddSymbol("delimiter", ']')
        .AddSymbol("delimiter", '=')
        .AddSymbol("delimiter", ';')
        .AddLiteral("symbol", "NULL")
        .AddLiteral("symbol", "TRUE")
        .AddLiteral("symbol", "FALSE")
        .AddNumeric("number", s => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
        .AddBlock("identifier", '<', '>')
        .AddBlock("string", '\'', '\'', '\\');
}

/// <summary>
/// One long string: the quadratic block reading of the 0.1.x <c>DynamicTokenizer</c> (baseline, about x2.5 per doubling
/// of the size, so 1 MB takes minutes) and the linear lexer
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class LargeStringBenchmarks
{
    private string _text = null!;
    private Lexer<JsonKind> _lexer = null!;
    private DynamicTokenizer _legacy = null!;

    [Params(25_000, 50_000, 100_000, 200_000)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _text = "'" + new string('a', Length) + "'";
        _lexer = CreateLexer();
        _legacy = new DynamicTokenizer().AddBlock("string", '\'', '\'', '\\');

        if (_lexer.Tokenize(_text).HasErrors)
            throw new BenchmarkException("The lexer doesn't tokenize the string");
    }

    [Benchmark(Baseline = true)]
    public int Legacy_0_1_x() => _legacy.ReadAsync(_text).GetAwaiter().GetResult().Tokens.Count;

    [Benchmark]
    public int NewLexer() => _lexer.Tokenize(_text).Tokens.Count;

    internal static Lexer<JsonKind> CreateLexer() => Lexer.Create<JsonKind>(b => b.Token(JsonKind.String, Rule.Delimited("'", "'", "\\", multiline: true)));
}

/// <summary>
/// One string of 1 MB with the lexer
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class HugeStringBenchmarks
{
    private string _text = null!;
    private Lexer<JsonKind> _lexer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _text = "'" + new string('a', 1_000_000) + "'";
        _lexer = LargeStringBenchmarks.CreateLexer();
    }

    [Benchmark]
    public int NewLexer() => _lexer.Tokenize(_text).Tokens.Count;
}
