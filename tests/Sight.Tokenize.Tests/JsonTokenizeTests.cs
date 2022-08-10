using Sight.Tokenize.Tokenizers;
using Sight.Tokenize.Tokens;

namespace Sight.Tokenize.Tests
{
    public class JsonTokenizeTests
    {
        [Test]
        public async Task Test_all_tokens_ranges_are_valid()
        {
            var tokenizer = new JsonTokenizer();
            var result = await tokenizer.ReadAsync(JsonSample);

            Assert.IsTrue(result.IsSuccess, "result.IsSuccess");
            Assert.IsEmpty(result.Errors, "Errors is not empty");
            Assert.IsTrue(result.Tokens.All(x => x is SymbolToken), "Not all symbol tokens");

            foreach (var token in result.Tokens.OfType<SymbolToken>())
            {
                Assert.AreEqual(token.Symbol, JsonSample.Substring((int)token.Position, (int)token.Length));
            }
        }

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
    }
}
