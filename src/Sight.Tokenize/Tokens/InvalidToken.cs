namespace Sight.Tokenize.Tokens
{
    /// <summary>
    /// Describe an unexpected token
    /// </summary>
    public class InvalidToken : Token
    {
        /// <summary>
        /// Initialize a new instance of the class <see cref="InvalidToken"/>
        /// </summary>
        public InvalidToken(string text, string error, long position, long length)
            : base(position, length)
        {
            Text = text;
            Error = error;
        }

        /// <summary>
        /// Invalid text
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Error message
        /// </summary>
        public string Error { get; }
    }
}
