using System.IO;
using Sight.Tokenize.Builder;

namespace Sight.Tokenize.Tests;

public class LexerTests
{
    private enum Kind
    {
        Whitespace,
        Comment,
        LBracket,
        RBracket,
        Equals,
        Arrow,
        EqualsEquals,
        Semicolon,
        Keyword,
        Number,
        String,
        Identifier,
        Tag,
        Text,
        Error
    }

    private const string Sample = "[ a = 'x' ; b = -12.5 ; c => NULL ] // end";

    [Test]
    public void Test_declarative_lexer()
    {
        var result = CreateLexer().Tokenize(Sample);

        Assert.IsFalse(result.HasErrors);
        Assert.IsEmpty(result.Diagnostics);
        var tokens = result.Tokens.Select(x => (x.Kind, result.GetText(x))).ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                (Kind.LBracket, "["), (Kind.Identifier, "a"), (Kind.Equals, "="), (Kind.String, "'x'"), (Kind.Semicolon, ";"),
                (Kind.Identifier, "b"), (Kind.Equals, "="), (Kind.Number, "-12.5"), (Kind.Semicolon, ";"),
                (Kind.Identifier, "c"), (Kind.Arrow, "=>"), (Kind.Keyword, "NULL"), (Kind.RBracket, "]")
            },
            tokens);
    }

    [Test]
    public void Test_trivia_are_emitted_on_demand_and_the_tokenization_is_lossless()
    {
        var lexer = CreateLexer(new LexerOptions { EmitTrivia = true });

        var result = lexer.Tokenize(Sample);

        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(Sample, string.Concat(result.Tokens.Select(x => result.GetText(x))));
        Assert.IsTrue(result.Tokens.Any(x => x.Kind == Kind.Whitespace));
        Assert.IsTrue(result.Tokens.Any(x => x.Kind == Kind.Comment));
    }

    [Test]
    public void Test_longest_match_then_declaration_order()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Token(Kind.Equals, "=")
            .Token(Kind.Arrow, "=>")
            .Token(Kind.EqualsEquals, "==")
            .Token(Kind.Identifier, Rule.Identifier)
            .Token(Kind.Keyword, "if"));

        var result = lexer.Tokenize("== => = if iff");

        // "if" is also matched by the identifier rule (same length): the first declared wins
        CollectionAssert.AreEqual(
            new[] { Kind.EqualsEquals, Kind.Arrow, Kind.Equals, Kind.Identifier, Kind.Identifier },
            result.Tokens.Select(x => x.Kind).ToArray());
        Assert.IsFalse(result.HasErrors);
    }

    [Test]
    public void Test_keywords_replace_the_kind_of_identifiers()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Token(Kind.Identifier, Rule.Identifier)
            .Keywords(Kind.Keyword, "NULL", "TRUE"));

        var result = lexer.Tokenize("NULL null TRUE NULLS T");

        CollectionAssert.AreEqual(
            new[] { Kind.Keyword, Kind.Identifier, Kind.Keyword, Kind.Identifier, Kind.Identifier },
            result.Tokens.Select(x => x.Kind).ToArray());
    }

    [Test]
    public void Test_keywords_can_ignore_case()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Token(Kind.Identifier, Rule.Identifier)
            .Keywords(Kind.Keyword, true, "select"));

        var result = lexer.Tokenize("SELECT Select selects");

        CollectionAssert.AreEqual(new[] { Kind.Keyword, Kind.Keyword, Kind.Identifier }, result.Tokens.Select(x => x.Kind).ToArray());
    }

    [Test]
    public void Test_keywords_do_not_apply_to_other_rules()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .Token(Kind.Identifier, Rule.Identifier)
            .Token(Kind.String, Rule.Delimited("'", "'"))
            .Keywords(Kind.Keyword, "NULL"));

        var result = lexer.Tokenize("'NULL'");

        Assert.AreEqual(Kind.String, result.Tokens.Single().Kind);
    }

    [Test]
    public void Test_modes_are_a_stack()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .Push(Kind.Tag, "<", "tag")
            .Token(Kind.Text, Rule.Custom(input => input.IndexOf('<') is var i && i < 0 ? input.Length : i, "abc "))
            .Mode("tag", m => m
                .Trivia(Kind.Whitespace, Rule.Whitespace)
                .Token(Kind.Identifier, Rule.Identifier)
                .Push(Kind.Tag, "[", "inner")
                .Pop(Kind.Tag, ">"))
            .Mode("inner", m => m
                .Token(Kind.Number, Rule.Number)
                .Pop(Kind.RBracket, "]")));

        var result = lexer.Tokenize("a <b c [1]> d");

        Assert.IsFalse(result.HasErrors, string.Join(", ", result.Diagnostics));
        CollectionAssert.AreEqual(
            new[] { "a ", "<", "b", "c", "[", "1", "]", ">", " d" },
            result.Tokens.Select(x => result.GetText(x)).ToArray());
        CollectionAssert.AreEqual(
            new[] { Kind.Text, Kind.Tag, Kind.Identifier, Kind.Identifier, Kind.Tag, Kind.Number, Kind.RBracket, Kind.Tag, Kind.Text },
            result.Tokens.Select(x => x.Kind).ToArray());
    }

    [Test]
    public void Test_pop_without_pushed_mode_is_ignored()
    {
        var lexer = Lexer.Create<Kind>(b => b.Pop(Kind.RBracket, "]").Token(Kind.LBracket, "["));

        var result = lexer.Tokenize("]][");

        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(3, result.Tokens.Count);
    }

    [Test]
    public void Test_mode_depth_is_bounded()
    {
        var lexer = Lexer.Create<Kind>(
            b => b.Push(Kind.Tag, "<", "tag").Mode("tag", m => m.Push(Kind.Tag, "<", "tag").Pop(Kind.Tag, ">")),
            new LexerOptions { MaxModeDepth = 4 });

        var result = lexer.Tokenize(new string('<', 100) + new string('>', 100));

        Assert.IsTrue(result.HasErrors);
        Assert.IsTrue(result.Diagnostics.Any(x => x.Code == "LEX004"));
        Assert.AreEqual(200, result.Tokens.Count);
    }

    [Test]
    public void Test_unexpected_characters_are_error_tokens()
    {
        var lexer = Lexer.Create<Kind>(b => b.ErrorKind(Kind.Error).Token(Kind.Identifier, Rule.Identifier));

        var result = lexer.Tokenize("a?\n😀b");

        Assert.IsTrue(result.HasErrors);
        CollectionAssert.AreEqual(
            new[] { Kind.Identifier, Kind.Error, Kind.Error, Kind.Error, Kind.Identifier },
            result.Tokens.Select(x => x.Kind).ToArray());
        Assert.IsTrue(result.Tokens.Where(x => x.Kind == Kind.Error).All(x => x.IsError));
        Assert.IsFalse(result.Tokens.Where(x => x.Kind != Kind.Error).Any(x => x.IsError));
        Assert.AreEqual(2, result.Tokens[3].Span.Length, "surrogate pair");
        CollectionAssert.AreEqual(new[] { "LEX001", "LEX001", "LEX001" }, result.Diagnostics.Select(x => x.Code).ToArray());
        StringAssert.Contains("'?'", result.Diagnostics[0].Message);
        StringAssert.Contains("U+000A", result.Diagnostics[1].Message);
        Assert.AreEqual(new LineColumn(2, 1), result.GetLineColumn(result.Tokens[3]));
    }

    [Test]
    public void Test_unterminated_delimited_token_is_reported_once()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .ErrorKind(Kind.Error)
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Token(Kind.String, Rule.Delimited("'", "'"))
            .Token(Kind.Identifier, Rule.Identifier));

        var result = lexer.Tokenize("a 'b c\nd 'e'");

        // The error covers the rest of the line, then the lexer goes on
        CollectionAssert.AreEqual(new[] { "a", "'b c", "d", "'e'" }, result.Tokens.Select(x => result.GetText(x)).ToArray());
        Assert.IsTrue(result.Tokens[1].IsError);
        Assert.AreEqual(Kind.String, result.Tokens[1].Kind);
        Assert.AreEqual(1, result.Diagnostics.Count);
        Assert.AreEqual("LEX003", result.Diagnostics[0].Code);
        Assert.AreEqual(new TextSpan(2, 4), result.Diagnostics[0].Span);
    }

    [Test]
    public void Test_error_rule_reports_its_diagnostic()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .ErrorKind(Kind.Error)
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Token(Kind.Number, Rule.Number)
            .Error(Rule.Custom(input =>
            {
                var i = 0;
                while (i < input.Length && char.IsLetterOrDigit(input[i]))
                    i++;

                return i;
            }, "0123456789"), "NUM001", "Invalid number"));

        var result = lexer.Tokenize("12 1a");

        Assert.AreEqual(new[] { Kind.Number, Kind.Error }, result.Tokens.Select(x => x.Kind).ToArray());
        Assert.AreEqual("NUM001", result.Diagnostics.Single().Code);
        Assert.AreEqual("Invalid number", result.Diagnostics.Single().Message);
        Assert.AreEqual(new TextSpan(3, 2), result.Diagnostics.Single().Span);
    }

    [Test]
    public void Test_max_token_length()
    {
        var lexer = Lexer.Create<Kind>(
            b => b.ErrorKind(Kind.Error).Token(Kind.Identifier, Rule.Identifier),
            new LexerOptions { MaxTokenLength = 3 });

        var result = lexer.Tokenize("abcdefgh");

        CollectionAssert.AreEqual(new[] { "abc", "def", "gh" }, result.Tokens.Select(x => result.GetText(x)).ToArray());
        Assert.IsTrue(result.Tokens.Take(2).All(x => x.IsError && x.Kind == Kind.Error));
        Assert.IsFalse(result.Tokens[2].IsError);
        Assert.AreEqual(new[] { "LEX002", "LEX002" }, result.Diagnostics.Select(x => x.Code).ToArray());
    }

    [Test]
    public void Test_max_token_length_does_not_split_surrogate_pairs()
    {
        var lexer = Lexer.Create<Kind>(
            b => b.Token(Kind.Text, Rule.Custom(input => input.Length)),
            new LexerOptions { MaxTokenLength = 3 });

        var result = lexer.Tokenize("ab😀cd");

        CollectionAssert.AreEqual(new[] { "ab", "😀c", "d" }, result.Tokens.Select(x => result.GetText(x)).ToArray());
    }

    [Test]
    public void Test_max_diagnostics()
    {
        var lexer = Lexer.Create<Kind>(b => b.Token(Kind.Identifier, Rule.Identifier), new LexerOptions { MaxDiagnostics = 5 });

        var result = lexer.Tokenize(new string('?', 100));

        Assert.IsTrue(result.HasErrors);
        Assert.AreEqual(5, result.Diagnostics.Count);
        Assert.AreEqual(100, result.Tokens.Count(x => x.IsError));
    }

    [Test]
    public void Test_regex_rule()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Token(Kind.Number, Rule.Regex(@"-?\d+(\.\d+)?", firstChars: "-0123456789"))
            .Token(Kind.Identifier, Rule.Regex("[a-z]+")));

        var result = lexer.Tokenize("12 -3.5 ab");

        Assert.IsFalse(result.HasErrors);
        CollectionAssert.AreEqual(new[] { Kind.Number, Kind.Number, Kind.Identifier }, result.Tokens.Select(x => x.Kind).ToArray());
    }

    [Test]
    public void Test_non_ascii_characters()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Token(Kind.Identifier, Rule.Identifier)
            .Token(Kind.Tag, Rule.Literal("→")));

        var result = lexer.Tokenize("été → ça");

        Assert.IsFalse(result.HasErrors);
        CollectionAssert.AreEqual(new[] { Kind.Identifier, Kind.Tag, Kind.Identifier }, result.Tokens.Select(x => x.Kind).ToArray());
    }

    [Test]
    public void Test_empty_text_and_default_options()
    {
        var result = CreateLexer().Tokenize(string.Empty);

        Assert.IsEmpty(result.Tokens);
        Assert.IsEmpty(result.Diagnostics);
        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(1, result.LineCount);
    }

    [Test]
    public void Test_all_input_types_give_the_same_result()
    {
        var lexer = CreateLexer();
        var expected = lexer.Tokenize(Sample).Tokens.Select(x => x.Span).ToArray();

        CollectionAssert.AreEqual(expected, lexer.Tokenize(Sample.AsSpan()).Tokens.Select(x => x.Span).ToArray());
        CollectionAssert.AreEqual(expected, lexer.Tokenize(Sample.AsMemory()).Tokens.Select(x => x.Span).ToArray());
        CollectionAssert.AreEqual(expected, lexer.Tokenize(("##" + Sample).AsMemory(2)).Tokens.Select(x => x.Span).ToArray());
        CollectionAssert.AreEqual(expected, lexer.Tokenize(new StringReader(Sample)).Tokens.Select(x => x.Span).ToArray());
        Assert.Throws<ArgumentNullException>(() => lexer.Tokenize((string)null!));
        Assert.Throws<ArgumentNullException>(() => lexer.Tokenize((TextReader)null!));
    }

    [Test]
    public void Test_text_reader_handles_encoding_and_bom()
    {
        var lexer = CreateLexer();
        using var stream = new MemoryStream(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'[', 0xF0, 0x9F, 0x98, 0x80, 0xFF, (byte)']' });
        using var reader = new StreamReader(stream, new UTF8Encoding(false), true);

        var result = lexer.Tokenize(reader);

        // Invalid bytes are replaced by U+FFFD by the reader, never an exception
        Assert.IsTrue(result.HasErrors);
        Assert.AreEqual(new TextSpan(0, 1), result.Tokens[0].Span);
        Assert.AreEqual(Kind.LBracket, result.Tokens[0].Kind);
    }

    [Test]
    public void Test_lexer_is_thread_safe()
    {
        var lexer = CreateLexer(new LexerOptions { EmitTrivia = true });
        var expected = lexer.Tokenize(Sample).Tokens.Select(x => x.Span).ToArray();

        Parallel.For(0, 200, _ =>
        {
            var result = lexer.Tokenize(Sample);
            CollectionAssert.AreEqual(expected, result.Tokens.Select(x => x.Span).ToArray());
        });
    }

    [Test]
    public void Test_options_are_copied()
    {
        var options = new LexerOptions { EmitTrivia = true };
        var lexer = CreateLexer(options);
        options.EmitTrivia = false;

        Assert.IsTrue(lexer.Tokenize(Sample).Tokens.Any(x => x.Kind == Kind.Whitespace));
    }

    [Test]
    public void Test_build_validates_the_configuration()
    {
        var noRule = Assert.Throws<InvalidOperationException>(() => Lexer.Create<Kind>(_ => { }));
        StringAssert.Contains("no rule", noRule!.Message);

        var unknownMode = Assert.Throws<InvalidOperationException>(() => Lexer.Create<Kind>(b => b.Push(Kind.Tag, "<", "missing")));
        StringAssert.Contains("missing", unknownMode!.Message);

        var emptyMode = Assert.Throws<InvalidOperationException>(() => Lexer.Create<Kind>(b => b.Token(Kind.Tag, "<").Mode("empty", _ => { })));
        StringAssert.Contains("empty", emptyMode!.Message);

        var noIdentifier = Assert.Throws<InvalidOperationException>(() => Lexer.Create<Kind>(b => b.Token(Kind.Tag, "<").Keywords(Kind.Keyword, "if")));
        StringAssert.Contains("Identifier", noIdentifier!.Message);

        var duplicateKeyword = Assert.Throws<InvalidOperationException>(() => Lexer.Create<Kind>(b => b.Token(Kind.Identifier, Rule.Identifier).Keywords(Kind.Keyword, "if", "if")));
        StringAssert.Contains("'if'", duplicateKeyword!.Message);

        Assert.Throws<ArgumentException>(() => Lexer.Create<Kind>(b => b.Token(Kind.Tag, "<").Mode("a", m => m.Token(Kind.Tag, ">")).Mode("a", m => m.Token(Kind.Tag, ">"))));
        Assert.Throws<ArgumentException>(() => Lexer.Create<Kind>(b => b.Token(Kind.Tag, string.Empty)));
        Assert.Throws<ArgumentException>(() => Lexer.Create<Kind>(b => b.Push(Kind.Tag, "<", string.Empty)));
        Assert.Throws<ArgumentException>(() => Lexer.Create<Kind>(b => b.Keywords(Kind.Keyword)));
        Assert.Throws<ArgumentException>(() => Lexer.Create<Kind>(b => b.Error("x", string.Empty, "message")));
        Assert.Throws<ArgumentNullException>(() => Lexer.Create<Kind>(b => b.Token(Kind.Tag, (Rule)null!)));
        Assert.Throws<ArgumentNullException>(() => Lexer.Create<Kind>(null!));
    }

    [Test]
    public async Task Test_input_is_tokenized_in_linear_time()
    {
        var lexer = CreateLexer();
        var chunk = "[ a = 'xyz' ; b = -12.5 ; c => NULL ] // end\n";
        var text = string.Concat(Enumerable.Repeat(chunk, 25_000));

        // Quadratic implementations need several minutes on 1 MB
        var task = Task.Run(() => lexer.Tokenize(text));
        Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(10_000)));
        Assert.AreEqual(25_000 * 13, (await task).Tokens.Count);
    }

    [Test]
    public async Task Test_unterminated_delimiters_are_scanned_once()
    {
        var lexer = Lexer.Create<Kind>(b => b
            .Token(Kind.String, Rule.Delimited("'", "'", "\\", multiline: true))
            .Token(Kind.Comment, Rule.Delimited("/*", "*/", multiline: true)));

        // Each unterminated opening scans the rest of the document: it must be consumed by the first error token
        var text = string.Concat(Enumerable.Repeat("/*'", 100_000));
        var task = Task.Run(() => lexer.Tokenize(text));
        Assert.AreSame(task, await Task.WhenAny(task, Task.Delay(10_000)));

        var result = await task;
        Assert.AreEqual(1, result.Tokens.Count);
        Assert.IsTrue(result.HasErrors);
        Assert.AreEqual(text.Length, result.Tokens[0].Span.Length);
    }

    private static Lexer<Kind> CreateLexer(LexerOptions options = null!) => Lexer.Create<Kind>(
        b => b
            .ErrorKind(Kind.Error)
            .Trivia(Kind.Whitespace, Rule.Whitespace)
            .Trivia(Kind.Comment, Rule.LineComment("//"))
            .Token(Kind.LBracket, "[").Token(Kind.RBracket, "]")
            .Token(Kind.Equals, "=").Token(Kind.Semicolon, ";").Token(Kind.Arrow, "=>")
            .Token(Kind.Number, Rule.Regex(@"-?\d+(\.\d+)?"))
            .Token(Kind.String, Rule.Delimited("'", "'", "\\"))
            .Token(Kind.Identifier, Rule.Identifier)
            .Keywords(Kind.Keyword, "NULL", "TRUE", "FALSE"),
        options);
}
