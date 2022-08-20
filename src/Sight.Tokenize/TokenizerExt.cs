using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sight.Tokenize.Parsing;

namespace Sight.Tokenize
{
    /// <summary>
    /// Define extension methods for an ITokenizer
    /// </summary>
    public static class TokenizerExt
    {
        /// <summary>
        /// Extract tokens from a formatted text
        /// </summary>
        public static Task<ParseResult> ReadAsync(this ITokenizer tokenizer, string text)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return tokenizer.ReadAsync(stream);
        }

        /// <summary>
        /// Get next character from UTF8 stream
        /// </summary>
        /// <returns>UNICODE character</returns>
        public static async Task<ReadResult> GetNextUtf8Async(this Stream stream)
        {
            var data = new byte[4];
            var read = await stream.ReadAsync(data, 0, 1).ConfigureAwait(false);
            if (read == 0)
                return ReadResult.EndOfFile();

            var firstByte = data[0];
            if (firstByte < 128)
                return ReadResult.Read(firstByte);

            var readCount = firstByte switch
            {
                >= 240 => 3,
                >= 224 => 2,
                _ => 1
            };

            read = await stream.ReadAsync(data, 1, readCount).ConfigureAwait(false);
            if (read != readCount)
                throw new FormatException("End of file encountered before reading a complete UFT8 character");

            var secondByte = data[1];
            var result = secondByte & 0b00111111;

            if (firstByte < 224)
                return ReadResult.Read(result | ((firstByte & 0b00011111) << 6));

            var thirdByte = data[2];
            result = (result << 6) | (thirdByte & 0b00111111);

            if (firstByte < 240)
                return ReadResult.Read(result | ((firstByte & 0b00001111) << 12));

            var fourthByte = data[3];
            return ReadResult.Read(result | ((fourthByte & 0b00001111) << 18));
        }
    }
}
