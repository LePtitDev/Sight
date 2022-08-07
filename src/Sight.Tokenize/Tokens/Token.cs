namespace Sight.Tokenize.Tokens
{
    /// <summary>
    /// Describe base class of a parsed token
    /// </summary>
    public abstract class Token
    {
        /// <summary>
        /// Initialize a new instance of the class <see cref="Token"/>
        /// </summary>
        protected Token(long position, long length)
        {
            Position = position;
            Length = length;
        }

        /// <summary>
        /// Token position in the document
        /// </summary>
        public long Position { get; }

        /// <summary>
        /// Token length in the document
        /// </summary>
        public long Length { get; }
    }
}
