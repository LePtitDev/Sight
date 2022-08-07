using System.Threading.Tasks;

namespace Sight.Tokenize.Documents
{
    /// <summary>
    /// Describe a document to parse
    /// </summary>
    public interface IDocument
    {
        /// <summary>
        /// Read an array of bytes
        /// </summary>
        /// <param name="count">Expected bytes count to read</param>
        /// <returns>Bytes count read</returns>
        public Task<int> ReadAsync(int count);

        /// <summary>
        /// Get the next byte
        /// </summary>
        public byte Next();
    }
}
