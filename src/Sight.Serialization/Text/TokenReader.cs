using System;
using System.Collections.Generic;
using System.Text;

namespace Sight.Serialization.Text;

/// <summary>
/// Cursor over tokens with helpers shared by the schema and data parsers
/// </summary>
internal sealed class TokenReader
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _index;

    public TokenReader(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
    }

    public Token Current => _tokens[_index];

    public Token PeekAt(int offset) => _tokens[Math.Min(_index + offset, _tokens.Count - 1)];

    public bool IsAtEnd => Current.Kind == TokenKind.EndOfFile;

    public Token Next()
    {
        var token = _tokens[_index];
        if (_index < _tokens.Count - 1)
            _index++;

        return token;
    }

    public bool TryConsume(char punctuation)
    {
        if (!Current.Is(punctuation))
            return false;

        Next();
        return true;
    }

    public Token Expect(char punctuation)
    {
        if (!Current.Is(punctuation))
            throw Error($"Expected '{punctuation}', found {Current}");

        return Next();
    }

    public Token ExpectIdentifier(string? text = null)
    {
        if (Current.Kind != TokenKind.Identifier || (text != null && Current.Text != text))
            throw Error(text != null ? $"Expected '{text}', found {Current}" : $"Expected an identifier, found {Current}");

        return Next();
    }

    /// <summary>
    /// Read a qualified name: identifiers joined by '.' without whitespace
    /// </summary>
    public string ExpectQualifiedName()
    {
        var first = ExpectIdentifier();
        var builder = new StringBuilder(first.Text);
        var previous = first;
        var previousEnd = first.Column + first.Text.Length;
        while (Current.Is('.') && Current.Line == previous.Line && Current.Column == previousEnd)
        {
            var dot = Next();
            if (Current.Kind != TokenKind.Identifier || Current.Line != dot.Line || Current.Column != dot.Column + 1)
                throw Error("Expected an identifier after '.'");

            previous = Next();
            previousEnd = previous.Column + previous.Text.Length;
            builder.Append('.').Append(previous.Text);
        }

        return builder.ToString();
    }

    public uint ExpectUInt32()
    {
        var token = Current;
        if (token.Kind != TokenKind.Number)
            throw Error($"Expected a version number, found {token}");

        Next();
        var number = NumberLiteral.Parse(token);
        if (number.IsFloat || number.IsNegative || number.Suffix.HasValue || number.Magnitude > uint.MaxValue || !number.IsDecimalInteger)
            throw new SerializationException("Expected a decimal integer between 0 and 4294967295", token.Line, token.Column);

        return (uint)number.Magnitude;
    }

    /// <summary>
    /// Parse items until a closing punctuation, separated by commas or line breaks (text specification §2.6)
    /// </summary>
    public void ParseItems(char closing, Action parseItem)
    {
        var first = true;
        while (!Current.Is(closing))
        {
            if (!first && !Current.AfterNewLine)
                throw Error($"Expected ',' or a line break before {Current}");

            parseItem();
            first = false;
            if (TryConsume(','))
            {
                first = true;
                if (Current.Is(','))
                    throw Error("Unexpected ','");
            }
        }

        Next();
    }

    public SerializationException Error(string message) => Error(Current, message);

    public static SerializationException Error(Token token, string message) => new(message, token.Line, token.Column);
}
