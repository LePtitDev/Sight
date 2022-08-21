using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sight.Tokenize.Helpers;
using Sight.Tokenize.Parsing;
using Sight.Tokenize.Tokens;

namespace Sight.Tokenize.Tokenizers
{
    /// <summary>
    /// Tokenizer that parse user specific documents
    /// </summary>
    public class DynamicTokenizer : ITokenizer
    {
        private readonly Dictionary<string, Scope> _scopes = new Dictionary<string, Scope>
        {
            { string.Empty, new Scope() }
        };

        private Scope? _defaultScope;

        /// <summary>
        /// Add a scope
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public DynamicTokenizer AddScope(string name, bool isDefault = false)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Scope name must be non empty", nameof(name));

            if (_scopes.ContainsKey(name))
                throw new InvalidOperationException($"Scope '{name}' already defined");

            var scope = new Scope();
            if (isDefault)
            {
                if (_defaultScope != null)
                    throw new InvalidOperationException("Default scope already defined");

                _defaultScope = scope;
            }

            _scopes[name] = scope;
            return this;
        }

        /// <summary>
        /// Add a character symbol
        /// </summary>
        public DynamicTokenizer AddSymbol(string type, int utf32, string? scope = null, string? toScope = null)
        {
            return AddToScope(scope, s => s.Symbols.Add(new CharSymbol(type, toScope, utf32)));
        }

        /// <inheritdoc cref="AddSymbol(string,int,string?,string?)"/>
        public DynamicTokenizer AddSymbol(string type, char utf32, string? scope = null, string? toScope = null)
        {
            return AddSymbol(type, (int)utf32, scope, toScope);
        }

        /// <summary>
        /// Add a complex symbol
        /// </summary>
        public DynamicTokenizer AddSymbol(string type, Func<int, StringBuilder, bool> whileFunc, Func<string, bool> validateFunc, string? scope = null, string? toScope = null)
        {
            return AddToScope(scope, s => s.Symbols.Add(new PredicableSymbol(type, toScope, whileFunc, validateFunc)));
        }

        /// <inheritdoc cref="AddSymbol(string,Func{int,StringBuilder,bool},Func{string,bool},string?,string?)"/>
        public DynamicTokenizer AddSymbol(string type, Func<int, StringBuilder, bool> whileFunc, Regex validateRegex, string? scope = null, string? toScope = null)
        {
            return AddSymbol(type, whileFunc, validateRegex.IsMatch, scope, toScope);
        }

        /// <summary>
        /// Add a literal symbol [a-zA-Z0-9@_] and special characters (> 127)
        /// </summary>
        public DynamicTokenizer AddLiteral(string type, Func<string, bool> validateFunc, string? scope = null, string? toScope = null)
        {
            return AddSymbol(type, (c, _) => IsLiteral(c), validateFunc, scope, toScope);
        }

        /// <inheritdoc cref="AddLiteral(string,Func{string,bool},string?,string?)"/>
        public DynamicTokenizer AddLiteral(string type, Regex validateRegex, string? scope = null, string? toScope = null)
        {
            return AddLiteral(type, validateRegex.IsMatch, scope, toScope);
        }

        /// <inheritdoc cref="AddLiteral(string,Func{string,bool},string?,string?)"/>
        /// <exception cref="ArgumentException"></exception>
        public DynamicTokenizer AddLiteral(string type, string literal, StringComparison comparison = StringComparison.Ordinal, string? scope = null, string? toScope = null)
        {
            if (string.IsNullOrEmpty(literal))
                throw new ArgumentException("Literal must be non empty", nameof(literal));

            return AddLiteral(type, s => string.Equals(s, literal, comparison), scope, toScope);
        }

        /// <summary>
        /// Add a numeric symbol
        /// </summary>
        public DynamicTokenizer AddNumeric(string type, Func<string, bool> validateFunc, string? scope = null, string? toScope = null)
        {
            return AddSymbol(type, IsNumeric, validateFunc, scope, toScope);
        }

        /// <inheritdoc cref="AddNumeric(string,Func{string,bool},string?,string?)"/>
        public DynamicTokenizer AddNumeric(string type, Regex validateRegex, string? scope = null, string? toScope = null)
        {
            return AddSymbol(type, IsNumeric, validateRegex.IsMatch, scope, toScope);
        }

        /// <inheritdoc cref="AddNumeric(string,Func{string,bool},string?,string?)"/>
        /// <exception cref="ArgumentException"></exception>
        public DynamicTokenizer AddNumeric(string type, string number, string? scope = null, string? toScope = null)
        {
            if (string.IsNullOrEmpty(number))
                throw new ArgumentException("Number must be non empty", nameof(number));

            return AddSymbol(type, IsNumeric, s => string.Equals(s, number, StringComparison.Ordinal), scope, toScope);
        }

        /// <inheritdoc cref="AddNumeric(string,Func{string,bool},string?,string?)"/>
        public DynamicTokenizer AddNumeric(string type, int number, string? scope = null, string? toScope = null)
        {
            return AddSymbol(type, IsNumeric, s => string.Equals(s, number.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), scope, toScope);
        }

        /// <summary>
        /// Add block symbol (like JSON string)
        /// </summary>
        public DynamicTokenizer AddBlock(string type, string blockBegin, string blockEnd, string? escape = null, string? scope = null, string? toScope = null)
        {
            if (string.IsNullOrEmpty(blockBegin))
                throw new ArgumentException("Block begin must be non empty", nameof(blockBegin));

            if (string.IsNullOrEmpty(blockEnd))
                throw new ArgumentException("Block end must be non empty", nameof(blockEnd));

            return AddSymbol(type, (_, b) =>
            {
                var text = b.ToString();
                if (b.Length <= blockBegin.Length && !blockBegin.StartsWith(text))
                    return false;

                if (b.Length > blockBegin.Length + blockEnd.Length && text.EndsWith(blockEnd))
                {
                    return !IsBlockEnd(text);
                }

                return true;
            }, s => s.StartsWith(blockBegin) && IsBlockEnd(s), scope, toScope);

            bool IsBlockEnd(string text)
            {
                if (escape == null)
                    return true;

                var esc = false;
                var index = text.Length - blockEnd.Length - escape.Length;
                while (index >= blockBegin.Length && string.Equals(text.Substring(index, escape.Length), escape, StringComparison.Ordinal))
                {
                    esc = !esc;
                    index -= escape.Length;
                }

                return !esc;
            }
        }

        /// <inheritdoc cref="AddBlock(string,string,string,string?,string?,string?)"/>
        public DynamicTokenizer AddBlock(string type, int blockBegin, int blockEnd, int? escape = null, string? scope = null, string? toScope = null)
        {
            return AddBlock(type, char.ConvertFromUtf32(blockBegin), char.ConvertFromUtf32(blockEnd), escape == null ? null : char.ConvertFromUtf32((int)escape), scope, toScope);
        }

        /// <inheritdoc cref="AddBlock(string,string,string,string?,string?,string?)"/>
        public DynamicTokenizer AddBlock(string type, char blockBegin, char blockEnd, char? escape = null, string? scope = null, string? toScope = null)
        {
            return AddBlock(type, (int)blockBegin, blockEnd, escape, scope, toScope);
        }

        /// <summary>
        /// Ignore character symbols
        /// </summary>
        public DynamicTokenizer IgnoreSymbols(int[] utf32, string? scope = null)
        {
            return AddToScope(scope, s => s.Ignore.AddRange(utf32.Select(c => new CharSymbol("__ignore__", null, c))));
        }

        /// <inheritdoc cref="IgnoreSymbols(int[],string?)"/>
        public DynamicTokenizer IgnoreSymbols(char[] utf32, string? scope = null)
        {
            return IgnoreSymbols(utf32.Select(c => (int)c).ToArray(), scope);
        }

        /// <summary>
        /// Ignore a character symbol
        /// </summary>
        public DynamicTokenizer IgnoreSymbol(int utf32, string? scope = null)
        {
            return IgnoreSymbols(new[] { utf32 }, scope);
        }

        /// <inheritdoc cref="IgnoreSymbol(int,string?)"/>
        public DynamicTokenizer IgnoreSymbol(char utf32, string? scope = null)
        {
            return IgnoreSymbol((int)utf32, scope);
        }


        private DynamicTokenizer AddToScope(string? scopeName, Action<Scope> func)
        {
            if (!_scopes.TryGetValue(scopeName ?? string.Empty, out var scope))
                throw new InvalidOperationException($"Scope '{scopeName}' not found");

            func(scope);
            return this;
        }

        /// <inheritdoc />
        public async Task<ParseResult> ReadAsync(Stream stream)
        {
            var tokens = new List<Token>();
            var errors = new List<string>();
            var readStatus = new ReadStatus(_defaultScope ?? _scopes[string.Empty]);
            do
            {
                var error = await ReadTokenAsync(stream, tokens, readStatus).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(error))
                    errors.Add(error!);

            } while (!readStatus.IsEof);

            return readStatus.IsEof
                ? ParseResult.Success(tokens.AsReadOnly(), errors.AsReadOnly())
                : ParseResult.Fail(errors.AsReadOnly());
        }

        private async Task<string?> ReadTokenAsync(Stream stream, List<Token> tokens, ReadStatus readStatus)
        {
            if (readStatus.Queue.Count == 0)
            {
                var read = await StreamHelpers.ReadUtf8Async(stream).ConfigureAwait(false);
                readStatus.Update(read);
            }

            if (readStatus.IsEof && readStatus.Queue.Count == 0)
                return null;

            var position = readStatus.Position - 1;
            foreach (var symbol in readStatus.Scope.Ignore)
            {
                var token = await symbol.TryReadAsync(position, stream, readStatus).ConfigureAwait(false);
                if (token != null)
                    return null;
            }

            foreach (var symbol in readStatus.Scope.Symbols)
            {
                var token = await symbol.TryReadAsync(position, stream, readStatus).ConfigureAwait(false);
                if (token != null)
                {
                    if (!string.IsNullOrEmpty(symbol.ToScope))
                    {
                        if (!_scopes.TryGetValue(symbol.ToScope!, out var scope))
                            throw new InvalidOperationException($"No scope '{scope}' found");

                        readStatus.Scope = scope;
                    }

                    tokens.Add(token);
                    return token is InvalidToken invalidToken ? invalidToken.Error : null;
                }
            }

            var text = char.ConvertFromUtf32((int)readStatus.Current!);
            readStatus.Next();
            var error = $"Unexpected character '{text}' at position '{position}'";
            tokens.Add(new InvalidToken(text, error, position, 1));
            return error;
        }

        private static bool IsLiteral(int character) => character is >= '0' and <= '9' or >= '@' and <= 'Z' or >= 'a' and <= 'z' or '_' or > 127;

        private static bool IsNumeric(int character, StringBuilder builder) => character is >= '0' and <= '9' or '.' || (builder.Length == 0 && character == '-');

        private class ReadStatus
        {
            public ReadStatus(Scope scope)
            {
                Scope = scope;
            }

            public bool IsEof { get; private set; }

            public Queue<int> Queue { get; } = new Queue<int>();

            public int? Current { get; private set; }

            public long Position { get; private set; }

            public Scope Scope { get; set; }

            public void Update(ReadResult read)
            {
                if (read.Eof)
                {
                    IsEof = true;
                    return;
                }

                Queue.Enqueue(read.Utf32);
                Position++;
                Current ??= read.Utf32;
            }

            public void Next()
            {
                Queue.Dequeue();
                Current = Queue.Count > 0 ? Queue.Peek() : null;
            }
        }

        private class Scope
        {
            public List<DynamicSymbol> Symbols { get; } = new List<DynamicSymbol>();

            public List<DynamicSymbol> Ignore { get; } = new List<DynamicSymbol>();
        }

        private abstract class DynamicSymbol
        {
            protected DynamicSymbol(string type, string? toScope)
            {
                Type = type;
                ToScope = toScope;
            }

            protected string Type { get; }

            public string? ToScope { get; }

            public abstract Task<Token?> TryReadAsync(long position, Stream stream, ReadStatus readStatus);
        }

        private class CharSymbol : DynamicSymbol
        {
            private readonly int _utf32;

            public CharSymbol(string type, string? toScope, int utf32)
                : base(type, toScope)
            {
                _utf32 = utf32;
            }

            public override Task<Token?> TryReadAsync(long position, Stream stream, ReadStatus readStatus)
            {
                if (readStatus.Current == _utf32)
                {
                    readStatus.Next();
                    return Task.FromResult<Token?>(new SymbolToken(Type, char.ConvertFromUtf32(_utf32), position, 1));
                }

                return Task.FromResult<Token?>(null);
            }
        }

        private class PredicableSymbol : DynamicSymbol
        {
            private readonly Func<int, StringBuilder, bool> _whileFunc;
            private readonly Func<string, bool> _validateFunc;

            public PredicableSymbol(string type, string? toScope, Func<int, StringBuilder, bool> whileFunc, Func<string, bool> validateFunc)
                : base(type, toScope)
            {
                _whileFunc = whileFunc;
                _validateFunc = validateFunc;
            }

            public override async Task<Token?> TryReadAsync(long position, Stream stream, ReadStatus readStatus)
            {
                var bld = new StringBuilder();
                var ended = false;
                var count = 0;
                foreach (var currentChar in readStatus.Queue)
                {
                    if (!_whileFunc(currentChar, bld))
                    {
                        ended = true;
                        break;
                    }

                    bld.Append(char.ConvertFromUtf32(currentChar));
                    count++;
                }

                if (!ended && !readStatus.IsEof)
                {
                    while (true)
                    {
                        var read = await StreamHelpers.ReadUtf8Async(stream).ConfigureAwait(false);
                        readStatus.Update(read);
                        if (read.Eof)
                            break;

                        if (!_whileFunc(read.Utf32, bld))
                            break;

                        bld.Append(char.ConvertFromUtf32(read.Utf32));
                        count++;
                    }
                }

                if (bld.Length == 0)
                    return null;

                var text = bld.ToString();
                if (!_validateFunc(text))
                    return null;

                for (var i = 0; i < count; i++)
                    readStatus.Next();

                return new SymbolToken(Type, text, position, count);
            }
        }
    }
}
