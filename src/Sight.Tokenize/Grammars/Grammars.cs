using System;

namespace Sight.Tokenize;

/// <summary>
/// Predefined grammars, built with the same builder as the user lexers. They are not meant to compete with
/// <c>Utf8JsonReader</c> or <c>XmlReader</c>: they are tolerant (syntax highlighting, tools) and lossless
/// (with <see cref="LexerOptions.EmitTrivia"/>, the concatenation of the tokens is the source).
/// </summary>
public static class Grammars
{
    private static readonly Lazy<Lexer<JsonKind>> JsonLexer = new Lazy<Lexer<JsonKind>>(() => CreateJson());
    private static readonly Lazy<Lexer<JsonKind>> JsonWithCommentsLexer = new Lazy<Lexer<JsonKind>>(() => CreateJson(true));
    private static readonly Lazy<Lexer<XmlKind>> XmlLexer = new Lazy<Lexer<XmlKind>>(() => CreateXml());

    /// <summary>
    /// Strict JSON (default options: the trivia are not emitted)
    /// </summary>
    public static Lexer<JsonKind> Json => JsonLexer.Value;

    /// <summary>
    /// JSON with <c>//</c> and <c>/* */</c> comments (default options: the trivia are not emitted)
    /// </summary>
    public static Lexer<JsonKind> JsonWithComments => JsonWithCommentsLexer.Value;

    /// <summary>
    /// XML (default options: the trivia are not emitted)
    /// </summary>
    public static Lexer<XmlKind> Xml => XmlLexer.Value;

    /// <summary>
    /// Create a JSON lexer: strict numbers (<c>-? (0|[1-9]\d*) (\.\d+)? ([eE][+-]?\d+)?</c>), strict strings
    /// (escapes and <c>\uXXXX</c> are validated, no control character), keywords true/false/null. Invalid lexemes are error tokens.
    /// </summary>
    /// <param name="allowComments">Indicates if <c>//</c> and <c>/* */</c> comments are accepted</param>
    /// <param name="options">Options of the lexer</param>
    public static Lexer<JsonKind> CreateJson(bool allowComments = false, LexerOptions? options = null)
    {
        return Lexer.Create<JsonKind>(b =>
        {
            b.ErrorKind(JsonKind.Error)
                .Trivia(JsonKind.Whitespace, Rule.Custom(JsonRules.MatchWhitespace, " \t\r\n"));

            if (allowComments)
            {
                b.Trivia(JsonKind.Comment, Rule.LineComment("//"))
                    .Trivia(JsonKind.Comment, Rule.Delimited("/*", "*/", multiline: true));
            }

            b.Token(JsonKind.BeginObject, "{").Token(JsonKind.EndObject, "}")
                .Token(JsonKind.BeginArray, "[").Token(JsonKind.EndArray, "]")
                .Token(JsonKind.Colon, ":").Token(JsonKind.Comma, ",")
                .Token(JsonKind.String, Rule.Custom(JsonRules.MatchString, "\""))
                // Same length as the strict rule: the strict rule wins. Longer or unterminated: invalid string
                .Error(Rule.Delimited("\"", "\"", "\\"), "JSON001", "Invalid string (illegal escape sequence or control character)")
                .Token(JsonKind.Number, Rule.Custom(JsonRules.MatchNumber, "-0123456789"))
                .Error(Rule.Custom(JsonRules.MatchNumberLexeme, "-0123456789"), "JSON002", "Invalid number")
                .Error(Rule.Identifier, "JSON003", "Unknown literal")
                .Keywords(JsonKind.Keyword, "true", "false", "null");
        }, options);
    }

    /// <summary>
    /// Create an XML lexer. The content mode emits text, tags (<c>&lt;</c>, <c>&lt;/</c>) that activate the tag mode
    /// (names, <c>=</c>, attribute values, <c>&gt;</c> and <c>/&gt;</c> that leave it), comments, CDATA sections,
    /// processing instructions and document type declarations.
    /// </summary>
    /// <param name="options">Options of the lexer</param>
    public static Lexer<XmlKind> CreateXml(LexerOptions? options = null)
    {
        return Lexer.Create<XmlKind>(b => b
            .ErrorKind(XmlKind.Error)
            .Trivia(XmlKind.Comment, Rule.Delimited("<!--", "-->", multiline: true))
            .Token(XmlKind.CData, Rule.Delimited("<![CDATA[", "]]>", multiline: true))
            .Token(XmlKind.ProcessingInstruction, Rule.Delimited("<?", "?>", multiline: true))
            .Token(XmlKind.DocType, Rule.Create(XmlRules.MatchDocType, c => c == '<'))
            .Push(XmlKind.EndTagOpen, "</", "tag")
            .Push(XmlKind.TagOpen, "<", "tag")
            .Token(XmlKind.Text, Rule.Create(XmlRules.MatchText, c => c != '<'))
            .Mode("tag", m => m
                .Trivia(XmlKind.Whitespace, Rule.Whitespace)
                .Token(XmlKind.Name, Rule.Create(XmlRules.MatchName, XmlRules.IsNameStart))
                .Token(XmlKind.Equals, "=")
                .Token(XmlKind.String, Rule.Delimited("\"", "\"", multiline: true))
                .Token(XmlKind.String, Rule.Delimited("'", "'", multiline: true))
                .Pop(XmlKind.TagClose, ">")
                .Pop(XmlKind.SelfClose, "/>")), options);
    }

    private static class JsonRules
    {
        public static int MatchWhitespace(ReadOnlySpan<char> input)
        {
            var i = 0;
            while (i < input.Length && input[i] is ' ' or '\t' or '\r' or '\n')
                i++;

            return i;
        }

        public static int MatchString(ReadOnlySpan<char> input)
        {
            if (input.Length == 0 || input[0] != '"')
                return 0;

            var i = 1;
            while (i < input.Length)
            {
                var c = input[i];
                if (c == '"')
                    return i + 1;

                if (c < ' ')
                    return 0;

                if (c != '\\')
                {
                    i++;
                    continue;
                }

                if (i + 1 >= input.Length)
                    return 0;

                switch (input[i + 1])
                {
                    case '"':
                    case '\\':
                    case '/':
                    case 'b':
                    case 'f':
                    case 'n':
                    case 'r':
                    case 't':
                        i += 2;
                        break;
                    case 'u':
                        if (i + 6 > input.Length)
                            return 0;

                        for (var j = i + 2; j < i + 6; j++)
                        {
                            if (!IsHexDigit(input[j]))
                                return 0;
                        }

                        i += 6;
                        break;
                    default:
                        return 0;
                }
            }

            return 0;
        }

        // -? (0|[1-9]\d*) (\.\d+)? ([eE][+-]?\d+)?  (the longest valid prefix)
        public static int MatchNumber(ReadOnlySpan<char> input)
        {
            var i = 0;
            if (i < input.Length && input[i] == '-')
                i++;

            if (i >= input.Length)
                return 0;

            if (input[i] == '0')
                i++;
            else if (input[i] is >= '1' and <= '9')
                SkipDigits(input, ref i);
            else
                return 0;

            if (i + 1 < input.Length && input[i] == '.' && IsDigit(input[i + 1]))
            {
                i++;
                SkipDigits(input, ref i);
            }

            if (i < input.Length && input[i] is 'e' or 'E')
            {
                var j = i + 1;
                if (j < input.Length && input[j] is '+' or '-')
                    j++;

                if (j < input.Length && IsDigit(input[j]))
                {
                    SkipDigits(input, ref j);
                    i = j;
                }
            }

            return i;
        }

        // Everything that looks like a number: used to report an invalid number as a whole
        public static int MatchNumberLexeme(ReadOnlySpan<char> input)
        {
            var i = 0;
            while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] is '-' or '+' or '.'))
                i++;

            return i;
        }

        private static bool IsDigit(char c) => c is >= '0' and <= '9';

        private static bool IsHexDigit(char c) => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';

        private static void SkipDigits(ReadOnlySpan<char> input, ref int i)
        {
            while (i < input.Length && IsDigit(input[i]))
                i++;
        }
    }

    private static class XmlRules
    {
        public static int MatchText(string text, int start, out bool unterminated)
        {
            unterminated = false;
            var i = start;
            while (i < text.Length && text[i] != '<')
                i++;

            return i - start;
        }

        public static bool IsNameStart(char c) => char.IsLetter(c) || c is '_' or ':' || char.IsSurrogate(c);

        public static int MatchName(string text, int start, out bool unterminated)
        {
            unterminated = false;
            var i = start;
            if (i >= text.Length || !IsNameStart(text[i]))
                return 0;

            i++;
            while (i < text.Length && IsNameChar(text[i]))
                i++;

            return i - start;
        }

        // <!DOCTYPE name [ internal subset ]>: '>' inside quotes or brackets doesn't end the declaration
        public static int MatchDocType(string text, int start, out bool unterminated)
        {
            const string prefix = "<!DOCTYPE";
            unterminated = false;
            if (string.CompareOrdinal(text, start, prefix, 0, prefix.Length) != 0)
                return 0;

            var i = start + prefix.Length;
            var depth = 0;
            var quote = '\0';
            while (i < text.Length)
            {
                var c = text[i++];
                if (quote != '\0')
                {
                    if (c == quote)
                        quote = '\0';
                }
                else if (c is '"' or '\'')
                {
                    quote = c;
                }
                else if (c == '[')
                {
                    depth++;
                }
                else if (c == ']' && depth > 0)
                {
                    depth--;
                }
                else if (c == '>' && depth == 0)
                {
                    return i - start;
                }
            }

            unterminated = true;
            return i - start;
        }

        private static bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c is '_' or ':' or '-' or '.' or '·' || char.IsSurrogate(c)
            || char.GetUnicodeCategory(c) is System.Globalization.UnicodeCategory.NonSpacingMark or System.Globalization.UnicodeCategory.SpacingCombiningMark;
    }
}
