using System;
using System.Text;
using System.Threading.Tasks;
using Sight.Tokenize.Documents;
using Sight.Tokenize.Parsing;

namespace Sight.Tokenize
{
    /// <summary>
    /// Define extension methods for an ITokenizer
    /// </summary>
    public static class TokenizerExt
    {
        private const int DefaultReadCount = 256;

        /// <summary>
        /// Parse a formatted text
        /// </summary>
        public static Task<ParseResult> ParseAsync(this ITokenizer tokenizer, string text)
        {
            return tokenizer.ParseAsync(new StringDocument(text));
        }

        /// <summary>
        /// Get next character from UTF8 document
        /// </summary>
        /// <returns>UNICODE character</returns>
        public static async Task<ReadResult> GetNextUtf8Async(this IDocument document, int availableBytes, int readCount = DefaultReadCount)
        {
            availableBytes = await ReadIfNeeded(document, availableBytes, readCount, false).ConfigureAwait(false);
            if (availableBytes == 0)
                return ReadResult.EndOfFile();

            var firstByte = document.Next();
            availableBytes--;

            if (firstByte < 128)
                return ReadResult.Read(firstByte, availableBytes);

            availableBytes = await ReadIfNeeded(document, availableBytes, readCount, true).ConfigureAwait(false);

            var secondByte = document.Next();
            availableBytes--;

            var result = secondByte & 0b00111111;

            if (firstByte < 224)
                return ReadResult.Read(result | ((firstByte & 0b00011111) << 6), availableBytes);

            availableBytes = await ReadIfNeeded(document, availableBytes, readCount, true).ConfigureAwait(false);

            var thirdByte = document.Next();
            availableBytes--;

            result = (result << 6) | (thirdByte & 0b00111111);

            if (firstByte < 240)
                return ReadResult.Read(result | ((firstByte & 0b00001111) << 12), availableBytes);

            availableBytes = await ReadIfNeeded(document, availableBytes, readCount, true).ConfigureAwait(false);

            var fourthByte = document.Next();
            return ReadResult.Read(result | ((fourthByte & 0b00001111) << 18), availableBytes - 1);

            static async Task<int> ReadIfNeeded(IDocument document, int availableBytes, int readCount, bool throwIfEof)
            {
                if (availableBytes == 0)
                {
                    availableBytes = await document.ReadAsync(readCount).ConfigureAwait(false);
                    if (availableBytes == 0 && throwIfEof)
                        throw new FormatException("End of file encountered before reading a complete UFT8 character");
                }

                return availableBytes;
            }
        }
    }
}
