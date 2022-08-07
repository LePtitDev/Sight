using System;
using System.Collections.Generic;
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
        /// Symbol type for object delimiter
        /// </summary>
        public const string ObjectDelimiterType = "object_delimiter";

        /// <inheritdoc />
        public async Task<ParseResult> ParseAsync(IDocument document)
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

        private async Task<string?> ReadTokenAsync(IDocument document, List<Token> tokens, ReadStatus readStatus)
        {
            var read = await document.GetNextUtf8Async(readStatus.AvailableBytes).ConfigureAwait(false);
            readStatus.Update(read);

            if (readStatus.IsEof)
                return null;

            switch (read.Utf32)
            {
                case '\r':
                case '\n':
                case '\t':
                case ' ':
                    // Ignore spaces
                    return null;
                case '{':
                case '}':
                    tokens.Add(new SymbolToken(ObjectDelimiterType, read.Utf32, readStatus.Position, 1));
                    return null;
            }

            // Maybe add it as special 'unknown' token
            return $"Unknown character '{char.ConvertFromUtf32(read.Utf32)}' at position '{readStatus.Position}'";
        }

        private class ReadStatus
        {
            public bool IsEof { get; private set; }

            public int AvailableBytes { get; private set; }

            public long Position { get; private set; }

            public void Update(ReadResult read)
            {
                if (read.Eof)
                {
                    IsEof = true;
                    return;
                }

                AvailableBytes = read.AvailableBytes;
                Position++;
            }
        }
    }
}
