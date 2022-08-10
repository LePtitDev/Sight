using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Sight.Tokenize.Documents;
using Sight.Tokenize.Parsing;
using Sight.Tokenize.Tokens;

namespace Sight.Tokenize.Tokenizers
{
    /// <summary>
    /// Tokenizer that parse JSON documents
    /// </summary>
    public class JsonTokenizer : ITokenizer
    {
        /// <summary>
        /// Symbol type for keyword
        /// </summary>
        public const string KeywordType = "keyword";

        /// <summary>
        /// Symbol type for number
        /// </summary>
        public const string NumberType = "number";

        /// <summary>
        /// Symbol type for string
        /// </summary>
        public const string StringType = "string";

        /// <summary>
        /// Symbol type for comment (not allowed in JSON)
        /// </summary>
        public const string CommentType = "comment";

        /// <summary>
        /// Symbol type for field delimiter
        /// </summary>
        public const string FieldDelimiterType = "field_delimiter";

        /// <summary>
        /// Symbol type for array delimiter
        /// </summary>
        public const string ArrayDelimiterType = "array_delimiter";

        /// <summary>
        /// Symbol type for object delimiter
        /// </summary>
        public const string ObjectDelimiterType = "object_delimiter";

        /// <inheritdoc />
        public async Task<ParseResult> ReadAsync(IDocument document)
        {
            var tokens = new List<Token>();
            var errors = new List<string>();
            var readStatus = new ReadStatus();
            do
            {
                var error = await ReadTokenAsync(document, tokens, readStatus).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(error))
                    errors.Add(error!);

            } while (!readStatus.IsEof);

            return readStatus.IsEof
                ? ParseResult.Success(tokens.AsReadOnly(), errors.AsReadOnly())
                : ParseResult.Fail(errors.AsReadOnly());
        }

        private static async Task<string?> ReadTokenAsync(IDocument document, List<Token> tokens, ReadStatus readStatus)
        {
            if (readStatus.Current == null)
            {
                var read = await document.GetNextUtf8Async(readStatus.AvailableBytes).ConfigureAwait(false);
                readStatus.Update(read);
            }

            if (readStatus.IsEof)
                return null;

            var position = readStatus.Position;
            var currentChar = (int)readStatus.Current!;
            switch (currentChar)
            {
                case '\r':
                case '\n':
                case '\t':
                case ' ':
                    // Ignore spaces
                    readStatus.Next();
                    return null;
                case ':':
                case ',':
                    readStatus.Next();
                    tokens.Add(new SymbolToken(FieldDelimiterType, currentChar, readStatus.Position, 1));
                    return null;
                case '[':
                case ']':
                    readStatus.Next();
                    tokens.Add(new SymbolToken(ArrayDelimiterType, currentChar, readStatus.Position, 1));
                    return null;
                case '{':
                case '}':
                    readStatus.Next();
                    tokens.Add(new SymbolToken(ObjectDelimiterType, currentChar, readStatus.Position, 1));
                    return null;
                case '"':
                {
                    var escape = false;
                    var str = await ReadWhileAsync(document, (c, bld) =>
                    {
                        if (bld.Length == 0) return c == '"';
                        if (bld.Length == 1) return true;

                        if (c < 128 && char.IsControl((char)c))
                            return false;

                        var lastChar = bld[bld.Length - 1];
                        if (lastChar == '"')
                        {
                            if (!escape)
                                return false;

                            escape = false;
                            return true;
                        }

                        if (lastChar == '\\')
                        {
                            escape = !escape;
                            return true;
                        }

                        escape = false;
                        return true;
                    }, readStatus).ConfigureAwait(false);

                    var strLength = readStatus.IsEof ? readStatus.Position - position : readStatus.Position - position - 1;
                    if (str.EndsWith("\""))
                    {
                        tokens.Add(new SymbolToken(StringType, str, position, strLength));
                        return null;
                    }

                    var error = $"Expected '\"' symbol at position '{readStatus.Position}'";
                    tokens.Add(new InvalidToken(str, error, position, strLength));
                    return error;
                }
                case '/':
                {
                    // Support comments but it's not allowed in JSON
                    // See https://en.wikipedia.org/wiki/JSON#cite_note-DouglasCrockfordComments-28
                    // or https://archive.ph/20150704102718/https://plus.google.com/+DouglasCrockfordEsq/posts/RK8qyGVaGSr

                    var comment = await ReadWhileAsync(document, (c, bld) =>
                    {
                        if (bld.Length == 0) return c == '/';
                        if (bld.Length == 1) return c is '/' or '*';

                        return bld[1] == '/'
                            ? bld[bld.Length - 1] is not ('\r' or '\n')
                            : bld[bld.Length - 1] != '/' || bld[bld.Length - 2] != '*';
                    }, readStatus).ConfigureAwait(false);

                    if (comment.StartsWith("//") || (comment.StartsWith("/*") && comment.EndsWith("*/")))
                    {
                        tokens.Add(new SymbolToken(CommentType, comment, position, comment.Length));
                        return null;
                    }

                    if (comment.Length == 1)
                        return ToUnexpectedChar();

                    var error = $"Expected '*/' sequence at position '{readStatus.Position}'";
                    tokens.Add(new InvalidToken(comment, error, position, comment.Length));
                    return error;
                }
            }

            if (currentChar is '-' or '.' or >= '0' and <= '9')
            {
                var literal = await ReadNumericLiteralAsync(document, readStatus).ConfigureAwait(false);
                if (double.TryParse(literal, out _))
                {
                    tokens.Add(new SymbolToken(NumberType, literal, position, literal.Length));
                    return null;
                }

                return ToUnknownLiteral(literal);
            }

            if (IsLiteral(currentChar))
            {
                var literal = await ReadLiteralAsync(document, readStatus).ConfigureAwait(false);
                switch (literal)
                {
                    case "null":
                    case "true":
                    case "false":
                        tokens.Add(new SymbolToken(KeywordType, literal, position, literal.Length));
                        return null;
                    default:
                        return ToUnknownLiteral(literal);
                }
            }

            return ToUnexpectedChar();

            string ToUnexpectedChar()
            {
                var text = char.ConvertFromUtf32(currentChar);
                var error = $"Unexpected character '{text}' at position '{position}'";
                tokens.Add(new InvalidToken(text, error, position, 1));
                return error;
            }

            string ToUnknownLiteral(string literal)
            {
                var error = $"Unknown literal '{literal}' at position '{position}'";
                tokens.Add(new InvalidToken(literal, error, position, literal.Length));
                return error;
            }
        }

        private static Task<string> ReadLiteralAsync(IDocument document, ReadStatus readStatus) => ReadWhileAsync(document, IsLiteral, readStatus);

        private static Task<string> ReadNumericLiteralAsync(IDocument document, ReadStatus readStatus) => ReadWhileAsync(document, IsNumericLiteral, readStatus);

        private static Task<string> ReadWhileAsync(IDocument document, Predicate<int> predicate, ReadStatus readStatus)
        {
            return ReadWhileAsync(document, (c, _) => predicate(c), readStatus);
        }

        private static async Task<string> ReadWhileAsync(IDocument document, Func<int, StringBuilder, bool> predicate, ReadStatus readStatus)
        {
            var bld = new StringBuilder();
            while (true)
            {
                if (readStatus.Current == null)
                {
                    var read = await document.GetNextUtf8Async(readStatus.AvailableBytes).ConfigureAwait(false);
                    readStatus.Update(read);
                }

                if (readStatus.IsEof)
                    return bld.ToString();

                var currentChar = (int)readStatus.Current!;
                if (!predicate(currentChar, bld))
                    return bld.ToString();

                readStatus.Next();
                bld.Append(char.ConvertFromUtf32(currentChar));
            }
        }

        private static bool IsLiteral(int character) => character is >= '0' and <= '9' or >= '@' and 'Z' or >= 'a' and <= 'z' or '_' or > 127;

        private static bool IsNumericLiteral(int character) => character == '.' || IsLiteral(character);

        private class ReadStatus
        {
            public bool IsEof { get; private set; }

            public int? Current { get; private set; }

            public int AvailableBytes { get; private set; }

            public long Position { get; private set; }

            public void Update(ReadResult read)
            {
                if (read.Eof)
                {
                    IsEof = true;
                    return;
                }

                Current = read.Utf32;
                AvailableBytes = read.AvailableBytes;
                Position++;
            }

            public void Next() => Current = null;
        }
    }
}
