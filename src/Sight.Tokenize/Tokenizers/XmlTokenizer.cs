using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sight.Tokenize.Parsing;
using Sight.Tokenize.Tokens;

namespace Sight.Tokenize.Tokenizers
{
    /// <summary>
    /// Tokenizer that parse XML documents
    /// </summary>
    public class XmlTokenizer : ITokenizer
    {
        /// <summary>
        /// Symbol type for element delimiter
        /// </summary>
        public const string ElementSymbolType = "element_symbol";

        /// <summary>
        /// Symbol type for identifier
        /// </summary>
        public const string IdentifierType = "identifier";

        /// <summary>
        /// Symbol type for string
        /// </summary>
        public const string StringType = "string";

        /// <summary>
        /// Symbol type for content
        /// </summary>
        public const string ContentType = "content";

        /// <inheritdoc />
        public async Task<ParseResult> ReadAsync(Stream stream)
        {
            var tokens = new List<Token>();
            var errors = new List<string>();
            var readStatus = new ReadStatus();
            do
            {
                var error = await ReadTokenAsync(stream, tokens, readStatus).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(error))
                    errors.Add(error!);

            } while (!readStatus.IsEof);

            return readStatus.IsEof
                ? ParseResult.Success(tokens.AsReadOnly(), errors.AsReadOnly())
                : ParseResult.Fail(errors.AsReadOnly());
        }

        private static async Task<string?> ReadTokenAsync(Stream stream, List<Token> tokens, ReadStatus readStatus)
        {
            if (readStatus.Current == null)
            {
                var read = await stream.GetNextUtf8Async().ConfigureAwait(false);
                readStatus.Update(read);
            }

            if (readStatus.IsEof)
                return null;

            var position = readStatus.Position - 1;
            var currentChar = (int)readStatus.Current!;
            if ((readStatus.Scope == ReadScope.Content && currentChar == '<') || (readStatus.Scope == ReadScope.Tag && currentChar == '>'))
            {
                readStatus.Next();
                readStatus.Scope = currentChar == '<' ? ReadScope.Tag : ReadScope.Content;
                tokens.Add(new SymbolToken(ElementSymbolType, currentChar, position, 1));
                return null;
            }

            if (readStatus.Scope == ReadScope.Tag)
            {
                switch (currentChar)
                {
                    case '\r':
                    case '\n':
                    case '\t':
                    case ' ':
                        // Ignore spaces
                        readStatus.Next();
                        return null;
                    case '/':
                    case '?':
                    case ':':
                    case '=':
                        readStatus.Next();
                        tokens.Add(new SymbolToken(ElementSymbolType, currentChar, position, 1));
                        return null;
                    case '<':
                    case '>':
                        readStatus.Next();
                        return ToUnexpectedChar();
                    case '\'':
                    case '"':
                        var str = await ReadWhileAsync(stream, (c, bld) =>
                        {
                            if (bld.Length == 0) return c is '"' or '\'';
                            if (bld.Length == 1) return true;
                            return bld[bld.Length - 1] != bld[0];
                        }, readStatus).ConfigureAwait(false);

                        var strLength = readStatus.IsEof ? readStatus.Position - position : readStatus.Position - position - 1;
                        if (str.Length >= 2 && str[0] == str[str.Length - 1])
                        {
                            tokens.Add(new SymbolToken(StringType, str, position, strLength));
                            return null;
                        }

                        var error = $"Expected '{char.ConvertFromUtf32(currentChar)}' symbol at position '{readStatus.Position}'";
                        tokens.Add(new InvalidToken(str, error, position, strLength));
                        return error;
                }

                if (IsLiteral(currentChar))
                {
                    var literal = await ReadLiteralAsync(stream, readStatus).ConfigureAwait(false);
                    tokens.Add(new SymbolToken(IdentifierType, literal, position, literal.Length));
                    return null;
                }

                return ToUnexpectedChar();
            }

            var content = await ReadWhileAsync(stream, c => c != '<', readStatus).ConfigureAwait(false);
            if (string.IsNullOrEmpty(content))
                return null;

            var contentLength = readStatus.IsEof ? readStatus.Position - position : readStatus.Position - position - 1;
            tokens.Add(new SymbolToken(ContentType, content, position, contentLength));
            return null;

            string ToUnexpectedChar()
            {
                var text = char.ConvertFromUtf32(currentChar);
                var error = $"Unexpected character '{text}' at position '{position}'";
                tokens.Add(new InvalidToken(text, error, position, 1));
                return error;
            }
        }

        private static Task<string> ReadLiteralAsync(Stream stream, ReadStatus readStatus) => ReadWhileAsync(stream, IsLiteral, readStatus);

        private static Task<string> ReadWhileAsync(Stream stream, Predicate<int> predicate, ReadStatus readStatus)
        {
            return ReadWhileAsync(stream, (c, _) => predicate(c), readStatus);
        }

        private static async Task<string> ReadWhileAsync(Stream stream, Func<int, StringBuilder, bool> predicate, ReadStatus readStatus)
        {
            var bld = new StringBuilder();
            while (true)
            {
                if (readStatus.Current == null)
                {
                    var read = await stream.GetNextUtf8Async().ConfigureAwait(false);
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

        private static bool IsLiteral(int character) => character is >= '0' and <= '9' or >= '@' and <= 'Z' or >= 'a' and <= 'z' or '_' or > 127;

        private enum ReadScope
        {
            Tag,
            Content
        }

        private class ReadStatus
        {
            public bool IsEof { get; private set; }

            public int? Current { get; private set; }

            public long Position { get; private set; }

            public ReadScope Scope { get; set; } = ReadScope.Content;

            public void Update(ReadResult read)
            {
                if (read.Eof)
                {
                    IsEof = true;
                    return;
                }

                Current = read.Utf32;
                Position++;
            }

            public void Next() => Current = null;
        }
    }
}
