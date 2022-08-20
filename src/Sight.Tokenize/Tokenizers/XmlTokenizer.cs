using System.Collections.Generic;
using System.IO;
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
            if (readStatus.Scope == ReadScope.Content)
            {
                if (currentChar is '<' or '>')
                {
                    readStatus.Next();
                    readStatus.Scope = currentChar == '<' ? ReadScope.Tag : ReadScope.Content;
                    tokens.Add(new SymbolToken(ElementSymbolType, currentChar, position, 1));
                    return null;
                }

                // Read content
            }
            else
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
                        readStatus.Scope = currentChar == '<' ? ReadScope.Tag : ReadScope.Content;
                        tokens.Add(new SymbolToken(ElementSymbolType, currentChar, position, 1));
                        return null;
                    case '"':
                        // Read string
                        break;
                }

                // Read identifiers
            }

            return null;
        }

        private enum ReadScope
        {
            Tag,
            Content
        }

        private class ReadStatus
        {
            public bool IsEof { get; private set; }

            public int? Current { get; private set; }

            public int AvailableBytes { get; private set; }

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
