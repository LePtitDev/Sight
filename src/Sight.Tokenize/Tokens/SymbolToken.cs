namespace Sight.Tokenize.Tokens
{
    /// <summary>
    /// Describe a token as symbol
    /// </summary>
    public class SymbolToken : Token
    {
        /// <summary>
        /// Initialize a new instance of the class <see cref="SymbolToken"/>
        /// </summary>
        public SymbolToken(string type, string symbol, long position, long length)
            : base(position, length)
        {
            Type = type;
            Symbol = symbol;
        }

        /// <inheritdoc cref="SymbolToken(string,string,long,long)"/>
        public SymbolToken(string type, int symbol, long position, long length)
            : this(type, char.ConvertFromUtf32(symbol), position, length)
        {
        }

        /// <inheritdoc cref="SymbolToken(string,string,long,long)"/>
        public SymbolToken(string type, char symbol, long position, long length)
            : this(type, symbol.ToString(), position, length)
        {
        }

        /// <summary>
        /// Symbol type (operator, variable, etc.)
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Symbol value
        /// </summary>
        public string Symbol { get; }
    }
}
