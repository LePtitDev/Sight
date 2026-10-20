namespace Sight.Tokenize.Tests;

public class CoreTypesTests
{
    private enum Kind
    {
        A,
        B
    }

    [Test]
    public void Test_text_span()
    {
        var span = new TextSpan(2, 3);

        Assert.AreEqual(5, span.End);
        Assert.IsFalse(span.IsEmpty);
        Assert.IsTrue(span.Contains(2));
        Assert.IsTrue(span.Contains(4));
        Assert.IsFalse(span.Contains(5));
        Assert.AreEqual(new TextSpan(2, 3), span);
        Assert.IsTrue(span == new TextSpan(2, 3));
        Assert.IsTrue(span != new TextSpan(2, 4));
        Assert.IsTrue(default(TextSpan).IsEmpty);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextSpan(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextSpan(0, -1));
    }

    [Test]
    public void Test_options_validation()
    {
        var options = new LexerOptions();

        Assert.IsFalse(options.EmitTrivia);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxTokenLength = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDiagnostics = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxModeDepth = 0);
    }

    [TestCase("", 0, 1, 1)]
    [TestCase("abc", 0, 1, 1)]
    [TestCase("abc", 3, 1, 4)]
    [TestCase("ab\ncd", 3, 2, 1)]
    [TestCase("ab\ncd", 2, 1, 3)]
    [TestCase("ab\r\ncd", 3, 1, 4)]
    [TestCase("ab\r\ncd", 4, 2, 1)]
    [TestCase("ab\rcd", 3, 2, 1)]
    [TestCase("\n\n\nx", 3, 4, 1)]
    public void Test_line_column(string text, int index, int line, int column)
    {
        var result = new TokenizeResult<Kind>(text, new Token<Kind>[0], new Diagnostic[0], false);

        Assert.AreEqual(new LineColumn(line, column), result.GetLineColumn(index));
        Assert.AreEqual($"{line}:{column}", result.GetLineColumn(index).ToString());
    }

    [Test]
    public void Test_line_column_out_of_range()
    {
        var result = new TokenizeResult<Kind>("abc", new Token<Kind>[0], new Diagnostic[0], false);

        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetLineColumn(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetLineColumn(4));
    }

    [Test]
    public void Test_line_count()
    {
        Assert.AreEqual(1, new TokenizeResult<Kind>("abc", new Token<Kind>[0], new Diagnostic[0], false).LineCount);
        Assert.AreEqual(3, new TokenizeResult<Kind>("a\r\nb\nc", new Token<Kind>[0], new Diagnostic[0], false).LineCount);
        Assert.AreEqual(2, new TokenizeResult<Kind>("a\n", new Token<Kind>[0], new Diagnostic[0], false).LineCount);
    }

    [Test]
    public void Test_get_text()
    {
        var token = new Token<Kind>(Kind.B, new TextSpan(1, 2));
        var result = new TokenizeResult<Kind>("abcd", new[] { token }, new Diagnostic[0], false);

        Assert.AreEqual("bc", result.GetText(token));
        Assert.AreEqual("bc", result.GetSpan(token).ToString());
        Assert.AreEqual(new LineColumn(1, 2), result.GetLineColumn(token));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetText(new TextSpan(3, 2)));
    }

    [Test]
    public void Test_diagnostic_and_token_to_string()
    {
        Assert.AreEqual("X1 [1..3): message", new Diagnostic("X1", "message", new TextSpan(1, 2)).ToString());
        Assert.AreEqual("B [1..3) (error)", new Token<Kind>(Kind.B, new TextSpan(1, 2), true).ToString());
    }
}
