using System.Threading.Tasks;
using Sight.Tokenize.Documents;
using Sight.Tokenize.Parsing;

namespace Sight.Tokenize
{
    /// <summary>
    /// Describe a document parser
    /// </summary>
    public interface ITokenizer
    {
        /// <summary>
        /// Extract tokens from a formatted document
        /// </summary>
        public Task<ParseResult> ReadAsync(IDocument document);
    }
}
