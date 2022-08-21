namespace Sight.Tokenize.Parsing
{
    /// <summary>
    /// Describe the result of a text reading operation
    /// </summary>
    internal readonly struct ReadResult
    {
        private ReadResult(bool eof, int utf32)
        {
            Eof = eof;
            Utf32 = utf32;
        }

        /// <summary>
        /// Indicates if an EOF was encountered
        /// </summary>
        public bool Eof { get; }

        /// <summary>
        /// Text character as UNICODE
        /// </summary>
        public int Utf32 { get; }

        /// <summary>
        /// Create a result for a character read
        /// </summary>
        public static ReadResult Read(int utf32)
        {
            return new ReadResult(false, utf32);
        }

        /// <summary>
        /// Create a result for a character read
        /// </summary>
        public static ReadResult EndOfFile()
        {
            return new ReadResult(true, -1);
        }
    }
}
