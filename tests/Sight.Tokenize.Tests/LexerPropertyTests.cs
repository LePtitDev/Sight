using System.Text.RegularExpressions;

namespace Sight.Tokenize.Tests;

/// <summary>
/// Invariants of the core on random inputs: termination, linearity (timeouts), lossless tokenization and no exception
/// </summary>
public class LexerPropertyTests
{
    private enum Kind
    {
        Whitespace,
        Comment,
        Punctuation,
        Number,
        String,
        Identifier,
        Keyword,
        Open,
        Close,
        Text,
        Error
    }

    // Units are strings to compose surrogate pairs, and lone surrogates are included on purpose
    private static readonly string[] Alphabet =
    {
        "{", "}", "[", "]", "<", ">", "/", "*", "=", ":", ",", ";", "'", "\"", "\\", " ", "\t", "\r", "\n", "-", "+", ".", "e", "E",
        "0", "1", "9", "a", "b", "if", "_", "?", "!", "#", "😀", "é", "\uD83D", "\uDE00", "\0", "/*", "*/", "//", "<!--", "-->"
    };

    [Test]
    public async Task Test_random_inputs_with_a_user_lexer()
    {
        var lexer = CreateLexer(new LexerOptions { EmitTrivia = true });

        await RunAsync(lexer, 2000, 1);
    }

    [Test]
    public async Task Test_random_inputs_with_small_limits()
    {
        var lexer = CreateLexer(new LexerOptions { EmitTrivia = true, MaxTokenLength = 2, MaxDiagnostics = 3, MaxModeDepth = 2 });

        await RunAsync(lexer, 2000, 2);
    }

    [Test]
    public async Task Test_random_inputs_with_json()
    {
        await RunAsync(Grammars.CreateJson(true, new LexerOptions { EmitTrivia = true }), 2000, 3);
    }

    [Test]
    public async Task Test_random_inputs_with_xml()
    {
        await RunAsync(Grammars.CreateXml(new LexerOptions { EmitTrivia = true }), 2000, 4);
    }

    [Test]
    public async Task Test_long_random_inputs_are_tokenized_in_time()
    {
        var lexer = CreateLexer(new LexerOptions { EmitTrivia = true });
        var random = new Random(5);
        var text = string.Concat(Enumerable.Range(0, 300_000).Select(_ => Alphabet[random.Next(Alphabet.Length)]));

        var task = Task.Run(() => lexer.Tokenize(text));
        Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(10_000)), "Tokenization is too slow");
        AssertConsistent(text, await task);
    }

    [Test]
    public async Task Test_pathological_inputs_are_tokenized_in_time()
    {
        var lexer = CreateLexer(new LexerOptions { EmitTrivia = true });
        var inputs = new[]
        {
            new string('\'', 200_001),
            string.Concat(Enumerable.Repeat("'\\", 100_000)),
            string.Concat(Enumerable.Repeat("<!--", 100_000)),
            string.Concat(Enumerable.Repeat("/*", 100_000)),
            string.Concat(Enumerable.Repeat("<", 100_000)),
            new string('<', 100_000) + new string('>', 100_000),
            string.Concat(Enumerable.Repeat("1e", 100_000)),
            string.Concat(Enumerable.Repeat("😀", 100_000)),
            string.Concat(Enumerable.Repeat("\uD83D", 100_000)),
            new string(' ', 500_000)
        };

        foreach (var text in inputs)
        {
            var task = Task.Run(() => lexer.Tokenize(text));
            Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(10_000)), $"Tokenization of '{text.Substring(0, 8)}...' is too slow");
            AssertConsistent(text, await task);
        }
    }

    [Test]
    public async Task Test_json_pathological_inputs_are_tokenized_in_time()
    {
        var lexer = Grammars.CreateJson(true, new LexerOptions { EmitTrivia = true });
        var inputs = new[]
        {
            new string('"', 200_001),
            string.Concat(Enumerable.Repeat("\"\\", 100_000)),
            string.Concat(Enumerable.Repeat("\"abc\n", 100_000)),
            string.Concat(Enumerable.Repeat("\"\\u12", 100_000)),
            string.Concat(Enumerable.Repeat("-1e+", 100_000)),
            string.Concat(Enumerable.Repeat("/*", 100_000)),
            new string('[', 200_000)
        };

        foreach (var text in inputs)
        {
            var task = Task.Run(() => lexer.Tokenize(text));
            Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(10_000)), $"Tokenization of '{text.Substring(0, 8)}...' is too slow");
            AssertConsistent(text, await task);
        }
    }

    private static async Task RunAsync<TKind>(Lexer<TKind> lexer, int count, int seed)
        where TKind : struct, Enum
    {
        var random = new Random(seed);
        for (var i = 0; i < count; i++)
        {
            var text = string.Concat(Enumerable.Range(0, random.Next(0, 40)).Select(_ => Alphabet[random.Next(Alphabet.Length)]));

            var task = Task.Run(() => lexer.Tokenize(text));
            Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(5000)), $"Tokenization of '{text}' did not complete");
            AssertConsistent(text, await task);
        }
    }

    private static void AssertConsistent<TKind>(string text, TokenizeResult<TKind> result)
        where TKind : struct, Enum
    {
        // The tokens cover the whole text, in order, without hole or overlap (trivia are emitted)
        var position = 0;
        foreach (var token in result.Tokens)
        {
            Assert.AreEqual(position, token.Span.Start);
            Assert.Greater(token.Span.Length, 0);
            position = token.Span.End;
        }

        Assert.AreEqual(text.Length, position);
        Assert.AreEqual(text, string.Concat(result.Tokens.Select(x => result.GetText(x))));
        Assert.AreEqual(result.Tokens.Any(x => x.IsError) || result.Diagnostics.Count > 0, result.HasErrors);
        foreach (var diagnostic in result.Diagnostics)
            Assert.LessOrEqual(diagnostic.Span.End, text.Length);
    }

    private static Lexer<Kind> CreateLexer(LexerOptions options) => Lexer.Create<Kind>(
        b => b
            .ErrorKind(Kind.Error)
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Trivia(Kind.Comment, Rule.LineComment("//"))
            .Trivia(Kind.Comment, Rule.Delimited("/*", "*/", multiline: true))
            .Trivia(Kind.Comment, Rule.Delimited("<!--", "-->", multiline: true))
            .Token(Kind.Punctuation, "=>").Token(Kind.Punctuation, "=").Token(Kind.Punctuation, ";").Token(Kind.Punctuation, ",")
            .Token(Kind.Number, Rule.Number)
            .Token(Kind.Number, Rule.Regex(@"[-+]?\.\d+", RegexOptions.None, TimeSpan.FromSeconds(1), ".-+"))
            .Token(Kind.String, Rule.Delimited("'", "'", "\\"))
            .Token(Kind.String, Rule.Delimited("\"", "\"", "\"", multiline: true))
            .Token(Kind.Identifier, Rule.Identifier)
            .Keywords(Kind.Keyword, "if")
            .Push(Kind.Open, "<", "tag")
            .Push(Kind.Open, "[", "tag")
            .Mode("tag", m => m
                .Trivia(Kind.Whitespace, Rule.Whitespace)
                .Token(Kind.Identifier, Rule.Identifier)
                .Push(Kind.Open, "<", "tag")
                .Pop(Kind.Close, ">")
                .Pop(Kind.Close, "]")
                .Token(Kind.Text, Rule.Custom(input => input.Length > 0 && input[0] is not ('<' or '>' or ']') ? 1 : 0))),
        options);
}
