#define DYNAMIC_SAMPLE

using Sight.Tokenize.Parsing;
using Sight.Tokenize.Tokenizers;
using Sight.Tokenize.Tokens;

namespace Sight.Tokenize.Sample
{
    public static class Program
    {
        private const string JsonSample = @"
{
    ""firstName"": ""John"",
    ""lastName"": ""Smith"",
    ""isAlive"": true,
    ""age"": 27,
    ""address"": {
        ""streetAddress"": ""21 2nd Street"",
        ""city"": ""New York"",
        ""state"": ""NY"",
        ""postalCode"": ""10021-3100""
    },
    ""phoneNumbers"": [
    {
        ""type"": ""home"",
        ""number"": ""212 555-1234""
    },
    {
        ""type"": ""office"",
        ""number"": ""646 555-4567""
    }
    ],
    ""children"": [
        ""Catherine"",
        ""Thomas"",
        ""Trevor""
    ],
    ""spouse"": null
}";

        private const string XmlSample = @"
<?xml version=""1.0"" encoding=""UTF-8"" ?>
<root>
  <item name=""Item #1"">Item 1</item>
  <item name=""Item #2"">Item 2</item>
  <item name=""Item #3"">Item 3</item>
</root>
";

        private const string DynamicSample = @"
[
  <firstName> = 'John';
  <lastName> = 'Smith';
  <isAlive> = TRUE;
  <age> = 27;
  <address> = [
    <streetAddress> = '21 2nd Street';
    <city> = 'New York';
    <state> = 'NY';
    <postalCode> = '10021-3100'
  ];
  <phoneNumbers> = [
    [
      <type> = 'home';
      <number> = '212 555-1234'
    ];
    [
      <type> = 'office';
      <number> = '646 555-4567'
    ]
  ];
  <children> = [
    'Catherine';
    'Thomas';
    'Trevor'
  ];
  <spouse> = NULL
]
";

        public static async Task Main()
        {
#if JSON_SAMPLE
            var tokenizer = new JsonTokenizer();
            const string text = JsonSample;
#endif

#if XML_SAMPLE
            var tokenizer = new XmlTokenizer();
            const string text = XmlSample;
#endif

#if DYNAMIC_SAMPLE
            var tokenizer = new DynamicTokenizer()
                .IgnoreSymbols(new [] { ' ', '\t', '\r', '\n' })
                .AddSymbol("delimiter", '[')
                .AddSymbol("delimiter", ']')
                .AddSymbol("delimiter", '=')
                .AddSymbol("delimiter", ';')
                .AddLiteral("symbol", "NULL")
                .AddLiteral("symbol", "TRUE")
                .AddLiteral("symbol", "FALSE")
                .AddNumeric("number", s => double.TryParse(s, out _))
                .AddBlock("identifier", '<', '>')
                .AddBlock("string", '\'', '\'', '\\');

            const string text = DynamicSample;
#endif

            var result = await tokenizer.ReadAsync(text);

            WriteResult(result);
        }

        private static void WriteResult(ParseResult result)
        {
            Console.WriteLine("=== RESULT ===");
            Console.WriteLine($"- IsSuccess: {result.IsSuccess}");
            if (result.Errors.Count > 0)
            {
                Console.WriteLine($"- Errors ({result.Errors.Count}):");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - \"{error}\"");
                }
            }
            else
            {
                Console.WriteLine("- No error");
            }

            if (result.Tokens.Count > 0)
            {
                Console.WriteLine($"- Tokens ({result.Tokens.Count}):");
                foreach (var token in result.Tokens)
                {
                    switch (token)
                    {
                        case SymbolToken symbolToken:
                            const int columnSize = 20;
                            var symbol = symbolToken.Symbol.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
                            Console.WriteLine($"  - S: {symbol}{new string(' ', Math.Max(1, columnSize - symbol.Length))}{symbolToken.Type}{new string(' ', Math.Max(1, columnSize - symbolToken.Type.Length))}[{symbolToken.Position}..{symbolToken.Position + symbolToken.Length}]");
                            break;
                        case InvalidToken invalidToken:
                            Console.WriteLine($"  - I: {invalidToken.Text} [{invalidToken.Position}..{invalidToken.Position + invalidToken.Length}] ({invalidToken.Error})");
                            break;
                        default:
                            Console.WriteLine($"  - U: Unknown token [{token.Position}..{token.Position + token.Length}]");
                            break;
                    }
                }
            }
            else
            {
                Console.WriteLine("- No token");
            }
        }
    }
}