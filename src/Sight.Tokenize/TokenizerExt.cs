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
    }
}
