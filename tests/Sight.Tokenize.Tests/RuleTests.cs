using System.Text.RegularExpressions;

namespace Sight.Tokenize.Tests;

public class RuleTests
{
    [TestCase("=>", "=> x", 0, 2)]
    [TestCase("=>", "= x", 0, 0)]
    [TestCase("=>", "a=>", 1, 2)]
    [TestCase("=>", "=", 0, 0)]
    [TestCase("=>", "", 0, 0)]
    public void Test_literal(string literal, string text, int start, int expected)
    {
        Assert.AreEqual(expected, Match(Rule.Literal(literal), text, start));
    }

    [Test]
    public void Test_literal_ignore_case()
    {
        Assert.AreEqual(3, Match(Rule.Literal("nUl", true), "NULL"));
        Assert.AreEqual(0, Match(Rule.Literal("nUl"), "NULL"));
        Assert.IsTrue(Rule.Literal("a", true).CanStart('A'));
        Assert.IsFalse(Rule.Literal("a").CanStart('A'));
    }

    [Test]
    public void Test_empty_literal_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Rule.Literal(string.Empty));
        Assert.Throws<ArgumentException>(() => Rule.Delimited(string.Empty, "'"));
        Assert.Throws<ArgumentException>(() => Rule.Delimited("'", string.Empty));
        Assert.Throws<ArgumentException>(() => Rule.Delimited("'", "'", string.Empty));
        Assert.Throws<ArgumentException>(() => Rule.LineComment(string.Empty));
        Assert.Throws<ArgumentException>(() => Rule.Regex(string.Empty));
        Assert.Throws<ArgumentNullException>(() => Rule.Custom(null!));
    }

    [TestCase("  \t\r\n x", 0, 6)]
    [TestCase("x ", 0, 0)]
    [TestCase("", 0, 0)]
    public void Test_whitespace(string text, int start, int expected)
    {
        Assert.AreEqual(expected, Match(Rule.Whitespace, text, start));
    }

    [TestCase("abc def", 3)]
    [TestCase("_a1_ ", 4)]
    [TestCase("1a", 0)]
    [TestCase("é1è-", 3)]
    [TestCase("", 0)]
    public void Test_identifier(string text, int expected)
    {
        Assert.AreEqual(expected, Match(Rule.Identifier, text));
        Assert.IsTrue(Rule.Identifier.IsIdentifier);
        Assert.IsFalse(Rule.Number.IsIdentifier);
    }

    [TestCase("12", 2)]
    [TestCase("-1", 2)]
    [TestCase("1.5", 3)]
    [TestCase("1.5.5", 3)]
    [TestCase("1.", 1)]
    [TestCase("1e5", 3)]
    [TestCase("1E+5", 4)]
    [TestCase("1e-", 1)]
    [TestCase("1e", 1)]
    [TestCase("007", 3)]
    [TestCase("-", 0)]
    [TestCase(".5", 0)]
    [TestCase("a", 0)]
    public void Test_number(string text, int expected)
    {
        Assert.AreEqual(expected, Match(Rule.Number, text));
    }

    [TestCase("'abc' x", 5, false)]
    [TestCase("''", 2, false)]
    [TestCase("'a\\'b' x", 6, false)]
    [TestCase("'a\\\\' x", 5, false)]
    [TestCase("'abc", 4, true)]
    [TestCase("'abc\nx'", 4, true)]
    [TestCase("'abc\\", 5, true)]
    [TestCase("'abc\\\nx'", 5, true)]
    [TestCase("x", 0, false)]
    public void Test_delimited_single_line(string text, int expected, bool unterminated)
    {
        var length = Match(Rule.Delimited("'", "'", "\\"), text, 0, out var isUnterminated);

        Assert.AreEqual(expected, length);
        Assert.AreEqual(unterminated, isUnterminated);
    }

    [TestCase("/* a\n b */ x", 10, false)]
    [TestCase("/**/", 4, false)]
    [TestCase("/* a\n b", 7, true)]
    [TestCase("/*/", 3, true)]
    public void Test_delimited_multiline(string text, int expected, bool unterminated)
    {
        var length = Match(Rule.Delimited("/*", "*/", multiline: true), text, 0, out var isUnterminated);

        Assert.AreEqual(expected, length);
        Assert.AreEqual(unterminated, isUnterminated);
    }

    [TestCase("'it''s' x", 7, false)]
    [TestCase("''", 2, false)]
    [TestCase("'''", 3, true)]
    [TestCase("''''", 4, false)]
    public void Test_delimited_with_doubled_end(string text, int expected, bool unterminated)
    {
        var length = Match(Rule.Delimited("'", "'", "'"), text, 0, out var isUnterminated);

        Assert.AreEqual(expected, length);
        Assert.AreEqual(unterminated, isUnterminated);
    }

    [TestCase("// abc\nx", 6)]
    [TestCase("// abc\r\nx", 6)]
    [TestCase("// abc", 6)]
    [TestCase("//", 2)]
    [TestCase("/ x", 0)]
    public void Test_line_comment(string text, int expected)
    {
        Assert.AreEqual(expected, Match(Rule.LineComment("//"), text));
    }

    [Test]
    public void Test_regex_is_anchored_at_the_current_position()
    {
        var rule = Rule.Regex(@"-?\d+(\.\d+)?");

        Assert.AreEqual(4, Match(rule, "-1.5 x"));
        Assert.AreEqual(0, Match(rule, "x 1"));
        Assert.AreEqual(2, Match(rule, "x 12", 2));
        Assert.AreEqual(0, Match(rule, string.Empty));
    }

    [Test]
    public void Test_regex_zero_length_match_is_no_match()
    {
        Assert.AreEqual(0, Match(Rule.Regex("a*"), "bbb"));
    }

    [Test]
    public void Test_regex_with_backreference_is_supported()
    {
        // The non backtracking engine doesn't support backreferences: the rule falls back to the default engine
        Assert.AreEqual(4, Match(Rule.Regex(@"(a)\1+b?"), "aaab"));
    }

    [Test]
    public void Test_regex_timeout_is_not_a_match()
    {
        // Catastrophic backtracking with the default engine (when the non backtracking engine is not usable)
        var rule = Rule.Regex(@"(a+)+\1$", RegexOptions.None, TimeSpan.FromMilliseconds(50));
        var text = new string('a', 40) + "!";

        var start = Environment.TickCount;
        Assert.AreEqual(0, Match(rule, text));
        Assert.Less(Environment.TickCount - start, 5000);
    }

    [Test]
    public void Test_custom_is_clamped()
    {
        Assert.AreEqual(3, Match(Rule.Custom(_ => 100), "abc"));
        Assert.AreEqual(0, Match(Rule.Custom(_ => -5), "abc"));
        Assert.AreEqual(2, Match(Rule.Custom(input => input.Length > 1 ? 2 : 0), "xab", 1));
    }

    [Test]
    public void Test_first_chars_hint()
    {
        var rule = Rule.Custom(_ => 1, "ab");

        Assert.IsTrue(rule.CanStart('a'));
        Assert.IsFalse(rule.CanStart('c'));
        Assert.IsTrue(Rule.Custom(_ => 1).CanStart('c'));
        Assert.IsTrue(Rule.Whitespace.CanStart(' '));
        Assert.IsFalse(Rule.Whitespace.CanStart('a'));
        Assert.IsTrue(Rule.Identifier.CanStart('_'));
        Assert.IsFalse(Rule.Identifier.CanStart('1'));
    }

    [Test]
    public void Test_implicit_conversions()
    {
        Rule fromString = "ab";
        Rule fromDelegate = (RuleMatcher)(_ => 1);

        Assert.AreEqual(2, Match(fromString, "abc"));
        Assert.AreEqual(1, Match(fromDelegate, "abc"));
    }

    private static int Match(Rule rule, string text, int start = 0) => Match(rule, text, start, out _);

    private static int Match(Rule rule, string text, int start, out bool unterminated)
    {
        var length = rule.Match(text, start, out unterminated);
        Assert.GreaterOrEqual(length, 0);
        Assert.LessOrEqual(length, text.Length - start);
        return length;
    }
}
