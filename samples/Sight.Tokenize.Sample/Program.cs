using Sight.Tokenize;

namespace Sight.Tokenize.Sample;

public static class Program
{
    private const string JsonSample = @"
{
    ""firstName"": ""John"",
    ""isAlive"": true,
    ""age"": 27,
    ""balance"": -12.5e+3,
    ""address"": { ""city"": ""New York"", ""postalCode"": ""10021-3100"" },
    ""phoneNumbers"": [ { ""type"": ""home"", ""number"": ""212 555-1234"" } ],
    ""spouse"": null,
    ""invalid"": 01
}";

    private const string XmlSample = @"
<?xml version=""1.0"" encoding=""UTF-8"" ?>
<root>
  <!-- items -->
  <item name=""Item #1"">Item 1</item>
  <item name=""Item #2"">Item 2</item>
  <empty />
</root>
";

    private const string DynamicSample = @"
[
  <firstName> = 'John';
  <isAlive> = TRUE;
  <age> = 27;
  <address> = [
    <city> = 'New York';
    <postalCode> = '10021-3100'
  ];
  <children> = [ 'Catherine'; 'Thomas'; 'Trevor' ];
  <spouse> = NULL;
  <unclosed> = 'oops
]
";

    private enum DynamicKind
    {
        Whitespace,
        Comment,
        Delimiter,
        Keyword,
        Number,
        Identifier,
        String,
        Error
    }

    /// <summary>
    /// Usage: <c>dotnet run [json|xml|dynamic]</c> (default: dynamic)
    /// </summary>
    public static void Main(string[] args)
    {
        switch (args.FirstOrDefault()?.ToLowerInvariant())
        {
            case "json":
                Write(Grammars.Json.Tokenize(JsonSample));
                break;
            case "xml":
                Write(Grammars.Xml.Tokenize(XmlSample));
                break;
            default:
                Write(CreateDynamicLexer().Tokenize(DynamicSample));
                break;
        }
    }

    // A lexer declared with the builder: longest match, then order of declaration
    private static Lexer<DynamicKind> CreateDynamicLexer() => Lexer.Create<DynamicKind>(b => b
        .ErrorKind(DynamicKind.Error)
        .Trivia(DynamicKind.Whitespace, Rule.Whitespace)
        .Trivia(DynamicKind.Comment, Rule.LineComment("#"))
        .Token(DynamicKind.Delimiter, "[").Token(DynamicKind.Delimiter, "]")
        .Token(DynamicKind.Delimiter, "=").Token(DynamicKind.Delimiter, ";")
        .Token(DynamicKind.Number, Rule.Number)
        .Token(DynamicKind.Identifier, Rule.Delimited("<", ">"))
        .Token(DynamicKind.String, Rule.Delimited("'", "'", escape: "\\"))
        .Token(DynamicKind.Keyword, Rule.Identifier)
        .Keywords(DynamicKind.Keyword, "NULL", "TRUE", "FALSE"));

    private static void Write<TKind>(TokenizeResult<TKind> result)
        where TKind : struct, Enum
    {
        Console.WriteLine("=== RESULT ===");
        Console.WriteLine($"- HasErrors: {result.HasErrors}");
        if (result.Diagnostics.Count > 0)
        {
            Console.WriteLine($"- Diagnostics ({result.Diagnostics.Count}):");
            foreach (var diagnostic in result.Diagnostics)
                Console.WriteLine($"  - {diagnostic.Code} at {result.GetLineColumn(diagnostic.Span)}: {diagnostic.Message}");
        }

        Console.WriteLine($"- Tokens ({result.Tokens.Count}):");
        foreach (var token in result.Tokens)
        {
            const int columnSize = 24;
            var text = result.GetText(token).Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            var kind = token.Kind.ToString();
            var error = token.IsError ? " (error)" : string.Empty;
            Console.WriteLine($"  - {text}{new string(' ', Math.Max(1, columnSize - text.Length))}{kind}{new string(' ', Math.Max(1, 14 - kind.Length))}{token.Span} @ {result.GetLineColumn(token)}{error}");
        }
    }
}
