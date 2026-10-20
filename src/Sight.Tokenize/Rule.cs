using System;
using System.Text.RegularExpressions;

namespace Sight.Tokenize;

/// <summary>
/// Custom matching function of a <see cref="Rule"/>
/// </summary>
/// <param name="input">Text from the current position to the end of the document</param>
/// <returns>The length (in UTF-16 code units) of the match at the start of <paramref name="input"/>, 0 when there is no match</returns>
public delegate int RuleMatcher(ReadOnlySpan<char> input);

/// <summary>
/// Describe how a lexeme is recognized at the current position of a text.
/// A rule never throws on an input: it returns the length of the match or 0.
/// </summary>
public sealed class Rule
{
    private static readonly Func<char, bool> IsIdentifierStart = c => char.IsLetter(c) || c == '_';

    private readonly MatchFunc _match;
    private readonly Func<char, bool>? _canStart;

    private Rule(MatchFunc match, Func<char, bool>? canStart, bool isIdentifier = false)
    {
        _match = match;
        _canStart = canStart;
        IsIdentifier = isIdentifier;
    }

    internal delegate int MatchFunc(string text, int start, out bool unterminated);

    /// <summary>
    /// Run of whitespaces (see <see cref="char.IsWhiteSpace(char)"/>)
    /// </summary>
    public static Rule Whitespace { get; } = new Rule(MatchWhitespace, char.IsWhiteSpace);

    /// <summary>
    /// Identifier: a letter or '_' followed by letters, digits or '_'. It's the rule on which <c>Keywords</c> are looked up.
    /// </summary>
    public static Rule Identifier { get; } = new Rule(MatchIdentifier, IsIdentifierStart, true);

    /// <summary>
    /// Number: <c>-? digits (. digits)? ([eE] [+-]? digits)?</c>
    /// </summary>
    public static Rule Number { get; } = new Rule(MatchNumber, c => c is '-' or >= '0' and <= '9');

    /// <summary>
    /// Indicates if the rule recognizes identifiers (see <see cref="Identifier"/>)
    /// </summary>
    internal bool IsIdentifier { get; }

    /// <summary>
    /// Create a rule that matches a literal text (like "=>")
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public static Rule Literal(string value, bool ignoreCase = false)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Literal must be non empty", nameof(value));

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var first = value[0];
        return new Rule(
            (string text, int start, out bool unterminated) =>
            {
                unterminated = false;
                return text.AsSpan(start).StartsWith(value.AsSpan(), comparison) ? value.Length : 0;
            },
            ignoreCase ? c => char.ToUpperInvariant(c) == char.ToUpperInvariant(first) || char.ToLowerInvariant(c) == char.ToLowerInvariant(first) : c => c == first);
    }

    /// <summary>
    /// Create a rule that matches a delimited text (string, block comment, etc.).
    /// The match ends on the first <paramref name="end"/> that is not escaped. When the end is missing the whole rest
    /// of the line (or of the document when <paramref name="multiline"/>) is matched once and reported as an unterminated token.
    /// When <paramref name="escape"/> equals <paramref name="end"/>, a doubled end is an escaped end (like <c>""</c> in a SQL string).
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public static Rule Delimited(string begin, string end, string? escape = null, bool multiline = false)
    {
        if (string.IsNullOrEmpty(begin))
            throw new ArgumentException("Begin must be non empty", nameof(begin));

        if (string.IsNullOrEmpty(end))
            throw new ArgumentException("End must be non empty", nameof(end));

        if (escape != null && escape.Length == 0)
            throw new ArgumentException("Escape must be non empty", nameof(escape));

        var doubledEnd = escape != null && string.Equals(escape, end, StringComparison.Ordinal);
        var first = begin[0];
        return new Rule(
            (string text, int start, out bool unterminated) =>
            {
                unterminated = false;
                var length = text.Length;
                if (!StartsWith(text, start, begin))
                    return 0;

                var i = start + begin.Length;
                while (i < length)
                {
                    var c = text[i];
                    if (!multiline && c is '\r' or '\n')
                        break;

                    if (escape != null && !doubledEnd && StartsWith(text, i, escape))
                    {
                        i += escape.Length;

                        // The escaped character is skipped (but a line break still ends a single line token)
                        if (i < length && (multiline || text[i] is not ('\r' or '\n')))
                            i++;

                        continue;
                    }

                    if (StartsWith(text, i, end))
                    {
                        i += end.Length;
                        if (doubledEnd && StartsWith(text, i, end))
                        {
                            i += end.Length;
                            continue;
                        }

                        return i - start;
                    }

                    i++;
                }

                unterminated = true;
                return i - start;
            },
            c => c == first);
    }

    /// <summary>
    /// Create a rule that matches a line comment: the prefix and all the following characters until the end of the line (excluded)
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public static Rule LineComment(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("Prefix must be non empty", nameof(prefix));

        var first = prefix[0];
        return new Rule(
            (string text, int start, out bool unterminated) =>
            {
                unterminated = false;
                if (!StartsWith(text, start, prefix))
                    return 0;

                var i = start + prefix.Length;
                while (i < text.Length && text[i] is not ('\r' or '\n'))
                    i++;

                return i - start;
            },
            c => c == first);
    }

    /// <summary>
    /// Create a rule from a regular expression anchored at the current position. A match timeout is mandatory
    /// (protection against ReDoS), on timeout the rule doesn't match. The non backtracking engine is used when available.
    /// </summary>
    /// <param name="pattern">Regular expression pattern</param>
    /// <param name="options">Regular expression options</param>
    /// <param name="timeout">Maximum duration of one match attempt (default: 1 second)</param>
    /// <param name="firstChars">Characters that can start a match (null if unknown), it speeds up the lexer</param>
    /// <exception cref="ArgumentException"></exception>
    public static Rule Regex(string pattern, RegexOptions options = RegexOptions.None, TimeSpan? timeout = null, string? firstChars = null)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern must be non empty", nameof(pattern));

        var matcher = new RegexMatcher(pattern, options, timeout ?? TimeSpan.FromSeconds(1));
        return new Rule(matcher.Match, FirstChars(firstChars));
    }

    /// <summary>
    /// Create a rule from a custom function
    /// </summary>
    /// <param name="matcher">Matching function</param>
    /// <param name="firstChars">Characters that can start a match (null if unknown), it speeds up the lexer</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static Rule Custom(RuleMatcher matcher, string? firstChars = null)
    {
        if (matcher == null)
            throw new ArgumentNullException(nameof(matcher));

        return new Rule(
            (string text, int start, out bool unterminated) =>
            {
                unterminated = false;
                var length = matcher(text.AsSpan(start));
                return length <= 0 ? 0 : Math.Min(length, text.Length - start);
            },
            FirstChars(firstChars));
    }

    /// <summary>
    /// Create a rule with the internal signature (used by the predefined grammars, it can report an unterminated token)
    /// </summary>
    internal static Rule Create(MatchFunc match, Func<char, bool>? canStart = null) => new Rule(match, canStart);

    /// <summary>
    /// Convert a literal text to a rule (see <see cref="Literal(string,bool)"/>)
    /// </summary>
    public static implicit operator Rule(string literal) => Literal(literal);

    /// <summary>
    /// Convert a function to a rule (see <see cref="Custom(RuleMatcher,string?)"/>)
    /// </summary>
    public static implicit operator Rule(RuleMatcher matcher) => Custom(matcher);

    /// <summary>
    /// Match the rule at a position (the length is always between 0 and the remaining length)
    /// </summary>
    internal int Match(string text, int start, out bool unterminated) => _match(text, start, out unterminated);

    /// <summary>
    /// Indicates if the rule can match a text that starts with the character
    /// </summary>
    internal bool CanStart(char c) => _canStart == null || _canStart(c);

    private static Func<char, bool>? FirstChars(string? firstChars)
    {
        if (firstChars == null)
            return null;

        return c => firstChars.IndexOf(c) >= 0;
    }

    private static bool StartsWith(string text, int index, string value) => text.AsSpan(index).StartsWith(value.AsSpan(), StringComparison.Ordinal);

    private static int MatchWhitespace(string text, int start, out bool unterminated)
    {
        unterminated = false;
        var i = start;
        while (i < text.Length && char.IsWhiteSpace(text[i]))
            i++;

        return i - start;
    }

    private static int MatchIdentifier(string text, int start, out bool unterminated)
    {
        unterminated = false;
        var i = start;
        if (i >= text.Length || !IsIdentifierStart(text[i]))
            return 0;

        i++;
        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
            i++;

        return i - start;
    }

    private static int MatchNumber(string text, int start, out bool unterminated)
    {
        unterminated = false;
        var i = start;
        if (i < text.Length && text[i] == '-')
            i++;

        var digits = SkipDigits(text, ref i);
        if (digits == 0)
            return 0;

        if (i + 1 < text.Length && text[i] == '.' && IsDigit(text[i + 1]))
        {
            i++;
            SkipDigits(text, ref i);
        }

        if (i < text.Length && text[i] is 'e' or 'E')
        {
            var j = i + 1;
            if (j < text.Length && text[j] is '+' or '-')
                j++;

            if (SkipDigits(text, ref j) > 0)
                i = j;
        }

        return i - start;
    }

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static int SkipDigits(string text, ref int i)
    {
        var start = i;
        while (i < text.Length && IsDigit(text[i]))
            i++;

        return i - start;
    }

    private sealed class RegexMatcher
    {
        private readonly System.Text.RegularExpressions.Regex _regex;

        public RegexMatcher(string pattern, RegexOptions options, TimeSpan timeout)
        {
#if NET7_0_OR_GREATER
            // \A: the match must start at the beginning of the span (the text from the current position)
            var anchored = @"\A(?:" + pattern + ")";
            try
            {
                _regex = new System.Text.RegularExpressions.Regex(anchored, options | RegexOptions.NonBacktracking, timeout);
            }
            catch (NotSupportedException)
            {
                // Some constructs (backreferences, lookarounds...) are not supported by the non backtracking engine
                _regex = new System.Text.RegularExpressions.Regex(anchored, options, timeout);
            }
#else
            // \G: the match must start at the given start index
            _regex = new System.Text.RegularExpressions.Regex(@"\G(?:" + pattern + ")", options, timeout);
#endif
        }

        public int Match(string text, int start, out bool unterminated)
        {
            unterminated = false;
            try
            {
#if NET7_0_OR_GREATER
                foreach (var match in _regex.EnumerateMatches(text.AsSpan(start)))
                    return match.Index == 0 ? match.Length : 0;

                return 0;
#else
                var match = _regex.Match(text, start);
                return match.Success && match.Index == start ? match.Length : 0;
#endif
            }
            catch (RegexMatchTimeoutException)
            {
                return 0;
            }
        }
    }
}
