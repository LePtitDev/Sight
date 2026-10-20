using System.Text.Json;

namespace Sight.Tokenize.Tests;

public class GrammarsTests
{
    private const string JsonSample = @"
{
    ""firstName"": ""John"",
    ""isAlive"": true,
    ""age"": 27,
    ""balance"": -12.5e+3,
    ""address"": { ""city"": ""New York"", ""unicode"": ""\u00e9\ud83d\ude00 😀"" },
    ""phoneNumbers"": [ { ""type"": ""home"" }, { ""type"": ""office"" } ],
    ""spouse"": null
}";

    private const string XmlSample = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<!DOCTYPE root [ <!ELEMENT root ANY> <!ENTITY e ""a>b""> ]>
<root xmlns:my-ns='urn:x'>
  <!-- a comment with <tags> -->
  <my-ns:item.1 name=""Item #1"" a='b'>Item &amp; 1</my-ns:item.1>
  <item><![CDATA[ <not> a tag ]]></item>
  <empty/>
</root>
";

    [Test]
    public void Test_json_tokens()
    {
        var result = Grammars.Json.Tokenize(JsonSample);

        Assert.IsFalse(result.HasErrors, string.Join(", ", result.Diagnostics));
        var kinds = result.Tokens.Select(x => x.Kind).ToArray();
        CollectionAssert.AreEqual(
            new[] { JsonKind.BeginObject, JsonKind.String, JsonKind.Colon, JsonKind.String, JsonKind.Comma, JsonKind.String, JsonKind.Colon, JsonKind.Keyword },
            kinds.Take(8).ToArray());
        Assert.AreEqual("-12.5e+3", result.Tokens.Where(x => x.Kind == JsonKind.Number).Select(x => result.GetText(x)).ElementAt(1));
        Assert.AreEqual(new LineColumn(2, 1), result.GetLineColumn(result.Tokens[0]));
    }

    [TestCase("-1")]
    [TestCase("1e+5")]
    [TestCase("1E-5")]
    [TestCase("0")]
    [TestCase("0.5")]
    [TestCase("-0")]
    [TestCase("123456789012345678901234567890")]
    public void Test_json_valid_numbers(string text)
    {
        var result = Grammars.Json.Tokenize(text);

        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(JsonKind.Number, result.Tokens.Single().Kind);
    }

    [TestCase("01")]
    [TestCase("1.")]
    [TestCase("1e")]
    [TestCase("1e+")]
    [TestCase("-")]
    [TestCase("--1")]
    [TestCase("1.2.3")]
    [TestCase("0x10")]
    [TestCase("1-2")]
    [TestCase("-Infinity")]
    public void Test_json_invalid_numbers_are_a_single_error_token(string text)
    {
        var result = Grammars.Json.Tokenize(text);

        Assert.IsTrue(result.HasErrors);
        Assert.AreEqual(1, result.Tokens.Count);
        Assert.IsTrue(result.Tokens[0].IsError);
        Assert.AreEqual(text.Length, result.Tokens[0].Span.Length);
        Assert.AreEqual("JSON002", result.Diagnostics.Single().Code);
    }

    [TestCase(".5")]
    [TestCase("+1")]
    public void Test_json_numbers_with_unexpected_start(string text)
    {
        Assert.IsTrue(Grammars.Json.Tokenize(text).HasErrors);
    }

    [TestCase("\"\"")]
    [TestCase("\"abc\"")]
    [TestCase("\"a\\\"b\\\\c\\/d\\b\\f\\n\\r\\t\"")]
    [TestCase("\"\\u00E9\\uD83D\\ude00\"")]
    [TestCase("\"😀 é\"")]
    public void Test_json_valid_strings(string text)
    {
        var result = Grammars.Json.Tokenize(text);

        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(JsonKind.String, result.Tokens.Single().Kind);
        Assert.AreEqual(text.Length, result.Tokens[0].Span.Length);
    }

    [TestCase("\"\\x\"")]
    [TestCase("\"\\u12\"")]
    [TestCase("\"\\u12G4\"")]
    [TestCase("\"a\tb\"")]
    [TestCase("\"\\\"")]
    public void Test_json_invalid_strings(string text)
    {
        var result = Grammars.Json.Tokenize(text);

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Tokens.All(x => x.IsError));
        Assert.AreEqual(text.Length, result.Tokens.Sum(x => x.Span.Length));
    }

    [Test]
    public void Test_json_invalid_string_diagnostic_codes()
    {
        Assert.AreEqual("JSON001", Grammars.Json.Tokenize("\"\\x\"").Diagnostics.Single().Code);
        Assert.AreEqual("LEX003", Grammars.Json.Tokenize("\"abc").Diagnostics.Single().Code);
    }

    [Test]
    public void Test_json_unterminated_string_stops_at_the_end_of_the_line()
    {
        var result = Grammars.Json.Tokenize("[\"abc\n1]");

        CollectionAssert.AreEqual(
            new[] { JsonKind.BeginArray, JsonKind.Error, JsonKind.Number, JsonKind.EndArray },
            result.Tokens.Select(x => x.Kind).ToArray());
        Assert.AreEqual(1, result.Diagnostics.Count);
    }

    [TestCase("true", JsonKind.Keyword)]
    [TestCase("false", JsonKind.Keyword)]
    [TestCase("null", JsonKind.Keyword)]
    [TestCase("True", JsonKind.Error)]
    [TestCase("nul", JsonKind.Error)]
    [TestCase("undefined", JsonKind.Error)]
    public void Test_json_keywords(string text, JsonKind expected)
    {
        var result = Grammars.Json.Tokenize(text);

        Assert.AreEqual(expected, result.Tokens.Single().Kind);
        Assert.AreEqual(expected == JsonKind.Error, result.HasErrors);
    }

    [Test]
    public void Test_json_unexpected_characters()
    {
        var result = Grammars.Json.Tokenize("{ ??? }");

        Assert.AreEqual(5, result.Tokens.Count);
        Assert.AreEqual(3, result.Tokens.Count(x => x.IsError));
        Assert.AreEqual(3, result.Diagnostics.Count);
    }

    [Test]
    public void Test_json_comments_are_optional()
    {
        const string text = "[1, // line\n 2 /* block */]";

        Assert.IsTrue(Grammars.Json.Tokenize(text).HasErrors);

        var result = Grammars.JsonWithComments.Tokenize(text);
        Assert.IsFalse(result.HasErrors);
        CollectionAssert.AreEqual(
            new[] { JsonKind.BeginArray, JsonKind.Number, JsonKind.Comma, JsonKind.Number, JsonKind.EndArray },
            result.Tokens.Select(x => x.Kind).ToArray());

        var withTrivia = Grammars.CreateJson(true, new LexerOptions { EmitTrivia = true }).Tokenize(text);
        Assert.AreEqual(2, withTrivia.Tokens.Count(x => x.Kind == JsonKind.Comment));
        Assert.AreEqual(text, string.Concat(withTrivia.Tokens.Select(x => withTrivia.GetText(x))));
    }

    [TestCase("/*/")]
    [TestCase("/*")]
    [TestCase("/* a")]
    public void Test_json_unclosed_comments_are_errors(string text)
    {
        var result = Grammars.JsonWithComments.Tokenize(text);

        Assert.IsTrue(result.HasErrors);
        Assert.AreEqual("LEX003", result.Diagnostics.Single().Code);
    }

    [Test]
    public void Test_json_is_independent_from_the_culture()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
            var result = Grammars.Json.Tokenize("[1.5, -2.25e3]");

            Assert.IsFalse(result.HasErrors);
            Assert.AreEqual(2, result.Tokens.Count(x => x.Kind == JsonKind.Number));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    [Test]
    public void Test_json_generated_documents_are_lossless_and_valid()
    {
        var random = new Random(42);
        var lexer = Grammars.CreateJson(false, new LexerOptions { EmitTrivia = true });
        for (var i = 0; i < 300; i++)
        {
            var builder = new StringBuilder();
            GenerateJson(random, builder, 0);
            var text = builder.ToString();

            using (JsonDocument.Parse(text))
            {
            }

            var result = lexer.Tokenize(text);
            Assert.IsFalse(result.HasErrors, $"{text}: {string.Join(", ", result.Diagnostics)}");
            Assert.AreEqual(text, string.Concat(result.Tokens.Select(x => result.GetText(x))));
        }
    }

    [Test]
    public void Test_json_mutated_documents_terminate_and_stay_lossless()
    {
        var random = new Random(7);
        var lexer = Grammars.CreateJson(true, new LexerOptions { EmitTrivia = true });
        var junk = new[] { "\"", "\\", "/", "*", "-", "+", ".", "e", "{", "]", "\n", "\t", "😀", "\u0000", "\\u" };
        for (var i = 0; i < 500; i++)
        {
            var builder = new StringBuilder();
            GenerateJson(random, builder, 0);
            var text = builder.ToString();
            var index = random.Next(text.Length + 1);
            text = random.Next(2) == 0
                ? text.Insert(index, junk[random.Next(junk.Length)])
                : text.Substring(0, index);

            var result = lexer.Tokenize(text);
            Assert.AreEqual(text, string.Concat(result.Tokens.Select(x => result.GetText(x))), text);
            Assert.AreEqual(result.Tokens.Any(x => x.IsError), result.HasErrors, text);
        }
    }

    [Test]
    public void Test_xml_tokens()
    {
        var result = Grammars.Xml.Tokenize(XmlSample);

        Assert.IsFalse(result.HasErrors, string.Join(", ", result.Diagnostics));
        var tokens = result.Tokens.Select(x => (x.Kind, result.GetText(x))).ToArray();
        CollectionAssert.Contains(tokens, (XmlKind.ProcessingInstruction, "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>"));
        CollectionAssert.Contains(tokens, (XmlKind.DocType, "<!DOCTYPE root [ <!ELEMENT root ANY> <!ENTITY e \"a>b\"> ]>"));
        CollectionAssert.Contains(tokens, (XmlKind.Name, "xmlns:my-ns"));
        CollectionAssert.Contains(tokens, (XmlKind.Name, "my-ns:item.1"));
        CollectionAssert.Contains(tokens, (XmlKind.String, "\"Item #1\""));
        CollectionAssert.Contains(tokens, (XmlKind.String, "'b'"));
        CollectionAssert.Contains(tokens, (XmlKind.Text, "Item &amp; 1"));
        CollectionAssert.Contains(tokens, (XmlKind.CData, "<![CDATA[ <not> a tag ]]>"));
        CollectionAssert.Contains(tokens, (XmlKind.SelfClose, "/>"));
        CollectionAssert.Contains(tokens, (XmlKind.EndTagOpen, "</"));
        Assert.IsFalse(result.Tokens.Any(x => x.Kind == XmlKind.Comment), "comments are trivia");
    }

    [Test]
    public void Test_xml_is_lossless()
    {
        var lexer = Grammars.CreateXml(new LexerOptions { EmitTrivia = true });

        var result = lexer.Tokenize(XmlSample);

        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(XmlSample, string.Concat(result.Tokens.Select(x => result.GetText(x))));
        Assert.AreEqual(1, result.Tokens.Count(x => x.Kind == XmlKind.Comment));
    }

    [Test]
    public void Test_xml_element_tokens()
    {
        var result = Grammars.Xml.Tokenize("<a b='c'>d</a>");

        CollectionAssert.AreEqual(
            new[]
            {
                XmlKind.TagOpen, XmlKind.Name, XmlKind.Name, XmlKind.Equals, XmlKind.String, XmlKind.TagClose,
                XmlKind.Text, XmlKind.EndTagOpen, XmlKind.Name, XmlKind.TagClose
            },
            result.Tokens.Select(x => x.Kind).ToArray());
    }

    [TestCase("<a $>")]
    [TestCase("<a <b>")]
    [TestCase("<a / >")]
    [TestCase("<!-- never closed")]
    [TestCase("<![CDATA[ never closed")]
    [TestCase("<? never closed")]
    [TestCase("<!DOCTYPE never closed [")]
    [TestCase("<a b='never closed>")]
    public void Test_xml_errors_are_reported(string text)
    {
        var lexer = Grammars.CreateXml(new LexerOptions { EmitTrivia = true });

        var result = lexer.Tokenize(text);

        Assert.IsTrue(result.HasErrors);
        Assert.IsNotEmpty(result.Diagnostics);
        Assert.AreEqual(text, string.Concat(result.Tokens.Select(x => result.GetText(x))));
    }

    [Test]
    public void Test_xml_random_documents_terminate_and_stay_lossless()
    {
        var random = new Random(99);
        var units = new[] { "<", ">", "/", "</", "/>", "<!--", "-->", "<![CDATA[", "]]>", "<?", "?>", "<!DOCTYPE", "[", "]", "=", "\"", "'", "a", "b-c", " ", "\n", "&", "😀", "é", "1" };
        var lexer = Grammars.CreateXml(new LexerOptions { EmitTrivia = true });
        for (var i = 0; i < 500; i++)
        {
            var text = string.Concat(Enumerable.Range(0, random.Next(0, 30)).Select(_ => units[random.Next(units.Length)]));

            var result = lexer.Tokenize(text);

            Assert.AreEqual(text, string.Concat(result.Tokens.Select(x => result.GetText(x))), text);
        }
    }

    [Test]
    public void Test_predefined_lexers_are_shared()
    {
        Assert.AreSame(Grammars.Json, Grammars.Json);
        Assert.AreSame(Grammars.Xml, Grammars.Xml);
        Assert.AreNotSame(Grammars.Json, Grammars.JsonWithComments);
    }

    private static void GenerateJson(Random random, StringBuilder builder, int depth)
    {
        var ws = new[] { string.Empty, " ", "\n  ", "\t", "\r\n" };
        string Ws() => ws[random.Next(ws.Length)];

        switch (depth > 3 ? random.Next(4, 8) : random.Next(8))
        {
            case 0:
            case 1:
                builder.Append('[').Append(Ws());
                for (var i = 0; i < random.Next(4); i++)
                {
                    if (i > 0)
                        builder.Append(Ws()).Append(',').Append(Ws());

                    GenerateJson(random, builder, depth + 1);
                }

                builder.Append(Ws()).Append(']');
                break;
            case 2:
            case 3:
                builder.Append('{').Append(Ws());
                for (var i = 0; i < random.Next(4); i++)
                {
                    if (i > 0)
                        builder.Append(Ws()).Append(',').Append(Ws());

                    builder.Append(JsonSerializer.Serialize("k" + random.Next(100) + "é😀\"\\\n")).Append(Ws()).Append(':').Append(Ws());
                    GenerateJson(random, builder, depth + 1);
                }

                builder.Append(Ws()).Append('}');
                break;
            case 4:
                builder.Append(JsonSerializer.Serialize(new string(Enumerable.Range(0, random.Next(10)).Select(_ => (char)random.Next(32, 0x2FF)).ToArray())));
                break;
            case 5:
                builder.Append(random.Next(-1000, 1000));
                break;
            case 6:
                builder.Append((random.NextDouble() * 1e6 - 5e5).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                break;
            default:
                builder.Append(new[] { "true", "false", "null", "1e+5", "-0.5E-3" }[random.Next(5)]);
                break;
        }
    }
}
