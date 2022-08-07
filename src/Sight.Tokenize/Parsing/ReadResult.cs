namespace Sight.Tokenize.Parsing
{
    /// <summary>
    /// Describe the result of a text reading operation
    /// </summary>
    public readonly struct ReadResult
    {
        private ReadResult(bool eof, int utf32, int availableBytes)
        {
            Eof = eof;
            Utf32 = utf32;
            AvailableBytes = availableBytes;
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
        /// Indicates available bytes count before an other read
        /// </summary>
        public int AvailableBytes { get; }

        /// <summary>
        /// Create a result for a character read
        /// </summary>
        public static ReadResult Read(int utf32, int availableBytes)
        {
            return new ReadResult(false, utf32, availableBytes);
        }

        /// <summary>
        /// Create a result for a character read
        /// </summary>
        public static ReadResult EndOfFile()
        {
            return new ReadResult(true, -1, 0);
        }
    }
}
