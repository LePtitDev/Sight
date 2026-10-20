using System.IO;
using System.Threading.Tasks;

namespace Sight.Tokenize.Benchmarks.Legacy;

/// <summary>
/// Describe a document parser
/// </summary>
public interface ITokenizer
{
    /// <summary>
    /// Extract tokens from a formatted document
    /// </summary>
    public Task<ParseResult> ReadAsync(Stream stream);
}
