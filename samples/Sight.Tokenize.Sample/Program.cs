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

        public static async Task Main()
        {
            var tokenizer = new JsonTokenizer();
            var result = await tokenizer.ReadAsync(JsonSample);

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
                            Console.WriteLine($"  - S: {symbolToken.Symbol}{new string(' ', Math.Max(1, columnSize - symbolToken.Symbol.Length))}{symbolToken.Type}{new string(' ', Math.Max(1, columnSize - symbolToken.Type.Length))}[{symbolToken.Position}..{symbolToken.Position + symbolToken.Length}]");
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