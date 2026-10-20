using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Sight.Serialization.Internal;

namespace Sight.Serialization.Text;

internal enum TokenKind
{
    EndOfFile,
    Identifier,
    Number,
    String,
    Base64,
    Directive,
    Punctuation,
}

internal readonly struct Token
{
    public Token(TokenKind kind, string text, int line, int column, bool afterNewLine, string? value = null)
    {
        Kind = kind;
        Text = text;
        Line = line;
        Column = column;
        AfterNewLine = afterNewLine;
        Value = value;
    }

    public TokenKind Kind { get; }

    /// <summary>
    /// Source text (identifier, number, punctuation, directive name without '@')
    /// </summary>
    public string Text { get; }

    public int Line { get; }

    public int Column { get; }

    /// <summary>
    /// Indicates whether a line break separates this token from the previous one
    /// </summary>
    public bool AfterNewLine { get; }

    /// <summary>
    /// Decoded value of strings and Base64 literals
    /// </summary>
    public string? Value { get; }

    public bool Is(char punctuation) => Kind == TokenKind.Punctuation && Text.Length == 1 && Text[0] == punctuation;

    public bool IsIdentifier(string text) => Kind == TokenKind.Identifier && string.Equals(Text, text, StringComparison.Ordinal);

    public override string ToString() => Kind switch
    {
        TokenKind.EndOfFile => "end of file",
        TokenKind.String => "string",
        TokenKind.Base64 => "Base64 literal",
        TokenKind.Directive => "'@" + Text + "'",
        _ => "'" + Text + "'",
    };
}

/// <summary>
/// Lexer of the schema language and the text data format (text specification §2)
/// </summary>
internal sealed class Lexer
{
    private readonly string _text;
    private readonly List<Token> _tokens = new();
    private int _position;
    private int _line = 1;
    private int _lineStart;
    private bool _newLine = true;

    private Lexer(string text)
    {
        _text = text;
    }

    public static IReadOnlyList<Token> Tokenize(string text)
    {
        var lexer = new Lexer(text);
        lexer.Run();
        return lexer._tokens;
    }

    /// <summary>
    /// Read a whole UTF-8 text stream, rejecting BOM and invalid data
    /// </summary>
    public static string ReadText(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return DecodeText(memory.GetBuffer(), (int)memory.Length);
    }

    public static string DecodeText(byte[] buffer, int length)
    {
        if (length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            throw new SerializationException("Byte order mark is not allowed", 1, 1);

        try
        {
            return Utf8.Strict.GetString(buffer, 0, length);
        }
        catch (DecoderFallbackException e)
        {
            throw new SerializationException("Invalid UTF-8 text", e);
        }
    }

    private void Run()
    {
        while (true)
        {
            SkipWhitespaceAndComments();
            if (_position >= _text.Length)
            {
                Add(TokenKind.EndOfFile, string.Empty, _position);
                return;
            }

            var c = _text[_position];
            var start = _position;
            if (c == '"')
            {
                ReadString(start);
            }
            else if (c == '@')
            {
                _position++;
                var name = ReadIdentifierText();
                if (name.Length == 0)
                    throw Error("Expected a directive name after '@'", start);

                Add(TokenKind.Directive, name, start);
            }
            else if (IsDigit(c) || (c == '-' && _position + 1 < _text.Length && (IsDigit(_text[_position + 1]) || Matches(_position + 1, "inf"))))
            {
                ReadNumber(start);
            }
            else if (Names.IsIdentifierStart(c))
            {
                var name = ReadIdentifierText();
                if (name == "b64" && _position < _text.Length && _text[_position] == '"')
                    ReadBase64(start);
                else
                    Add(TokenKind.Identifier, name, start);
            }
            else if ("{}[]<>():,=?&*#.".IndexOf(c) >= 0)
            {
                _position++;
                Add(TokenKind.Punctuation, c.ToString(), start);
            }
            else
            {
                throw Error($"Unexpected character '{c}'", start);
            }
        }
    }

    private void SkipWhitespaceAndComments()
    {
        while (_position < _text.Length)
        {
            var c = _text[_position];
            if (c is ' ' or '\t')
            {
                _position++;
            }
            else if (c == '\n')
            {
                NewLine(_position + 1);
            }
            else if (c == '\r')
            {
                if (_position + 1 >= _text.Length || _text[_position + 1] != '\n')
                    throw Error("Lone carriage return is not allowed", _position);

                _position++;
            }
            else if (c == '/' && _position + 1 < _text.Length && _text[_position + 1] == '/')
            {
                while (_position < _text.Length && _text[_position] != '\n' && _text[_position] != '\r')
                    _position++;
            }
            else if (c == '/' && _position + 1 < _text.Length && _text[_position + 1] == '*')
            {
                var start = _position;
                _position += 2;
                while (true)
                {
                    if (_position + 1 >= _text.Length)
                        throw Error("Unterminated block comment", start);

                    if (_text[_position] == '*' && _text[_position + 1] == '/')
                    {
                        _position += 2;
                        break;
                    }

                    if (_text[_position] == '\n')
                        NewLine(_position + 1);
                    else
                        _position++;
                }
            }
            else
            {
                return;
            }
        }
    }

    private void NewLine(int next)
    {
        _position = next;
        _line++;
        _lineStart = next;
        _newLine = true;
    }

    private string ReadIdentifierText()
    {
        var start = _position;
        if (_position < _text.Length && Names.IsIdentifierStart(_text[_position]))
        {
            _position++;
            while (_position < _text.Length && Names.IsIdentifierPart(_text[_position]))
                _position++;
        }

        return _text.Substring(start, _position - start);
    }

    private void ReadNumber(int start)
    {
        if (_text[_position] == '-')
            _position++;

        if (Matches(_position, "inf"))
        {
            _position += 3;
        }
        else if (Matches(_position, "0x"))
        {
            _position += 2;
            ReadDigits(start, IsHexDigit);
        }
        else if (Matches(_position, "0b"))
        {
            _position += 2;
            ReadDigits(start, x => x is '0' or '1');
        }
        else
        {
            ReadDigits(start, IsDigit);
            if (_position + 1 < _text.Length && _text[_position] == '.' && IsDigit(_text[_position + 1]))
            {
                _position++;
                ReadDigits(start, IsDigit);
            }

            if (_position < _text.Length && _text[_position] is 'e' or 'E')
            {
                _position++;
                if (_position < _text.Length && _text[_position] is '+' or '-')
                    _position++;

                ReadDigits(start, IsDigit);
            }
        }

        // Suffix (u8, s32, f64...)
        while (_position < _text.Length && Names.IsIdentifierPart(_text[_position]))
            _position++;

        Add(TokenKind.Number, _text.Substring(start, _position - start), start);
    }

    private void ReadDigits(int start, Func<char, bool> isDigit)
    {
        if (_position >= _text.Length || !isDigit(_text[_position]))
            throw Error("Invalid number", start);

        while (_position < _text.Length)
        {
            var c = _text[_position];
            if (isDigit(c))
            {
                _position++;
            }
            else if (c == '_' && _position + 1 < _text.Length && isDigit(_text[_position + 1]))
            {
                _position++;
            }
            else
            {
                break;
            }
        }
    }

    private void ReadString(int start)
    {
        if (Matches(_position, "\"\"\""))
        {
            ReadMultiLineString(start);
            return;
        }

        _position++;
        var builder = new StringBuilder();
        while (true)
        {
            if (_position >= _text.Length)
                throw Error("Unterminated string", start);

            var c = _text[_position];
            if (c == '"')
            {
                _position++;
                break;
            }

            if (c == '\\')
            {
                ReadEscape(builder);
                continue;
            }

            if (c < 0x20 && c != '\t')
                throw Error("Control characters and line breaks are not allowed in strings", _position);

            builder.Append(c);
            _position++;
        }

        Add(TokenKind.String, "\"", start, builder.ToString());
    }

    private void ReadMultiLineString(int start)
    {
        _position += 3;
        if (Matches(_position, "\r\n"))
            _position += 2;
        else if (_position < _text.Length && _text[_position] == '\n')
            _position++;
        else
            throw Error("A multi-line string must start with '\"\"\"' followed by a line break", start);

        var startLine = _line;
        _line++;
        _lineStart = _position;

        // Collect raw lines up to the closing line
        var lines = new List<(int Start, int End)>();
        string? indentation = null;
        while (true)
        {
            if (_position >= _text.Length)
                throw Error("Unterminated multi-line string", start);

            var lineStart = _position;
            var end = _text.IndexOf('\n', _position);
            var lineEnd = end < 0 ? _text.Length : end;
            var contentEnd = lineEnd > lineStart && _text[lineEnd - 1] == '\r' ? lineEnd - 1 : lineEnd;
            var trimmed = _text.Substring(lineStart, contentEnd - lineStart).TrimStart(' ', '\t');
            if (trimmed.StartsWith("\"\"\"", StringComparison.Ordinal))
            {
                indentation = _text.Substring(lineStart, contentEnd - lineStart - trimmed.Length);
                _position = lineStart + indentation.Length + 3;
                break;
            }

            lines.Add((lineStart, contentEnd));
            if (end < 0)
                throw Error("Unterminated multi-line string", start);

            _position = end + 1;
            _line++;
            _lineStart = _position;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            var (lineStart, lineEnd) = lines[i];
            if (i > 0)
                builder.Append('\n');

            if (lineEnd == lineStart)
                continue;

            if (string.CompareOrdinal(_text, lineStart, indentation, 0, indentation.Length) != 0 || lineEnd - lineStart < indentation.Length)
            {
                if (string.IsNullOrWhiteSpace(_text.Substring(lineStart, lineEnd - lineStart)))
                    continue;

                throw new SerializationException("Multi-line string line does not start with the closing indentation", startLine + i + 1, 1);
            }

            var saved = _position;
            _position = lineStart + indentation.Length;
            while (_position < lineEnd)
            {
                var c = _text[_position];
                if (c == '\\')
                {
                    ReadEscape(builder);
                    continue;
                }

                if (c < 0x20 && c != '\t')
                    throw Error("Control characters are not allowed in strings", _position);

                builder.Append(c);
                _position++;
            }

            _position = saved;
        }

        AddAt(TokenKind.String, "\"\"\"", startLine, start, builder.ToString());
    }

    private void ReadEscape(StringBuilder builder)
    {
        var start = _position;
        _position++;
        if (_position >= _text.Length)
            throw Error("Invalid escape sequence", start);

        var c = _text[_position++];
        switch (c)
        {
            case '"':
                builder.Append('"');
                break;
            case '\\':
                builder.Append('\\');
                break;
            case 'n':
                builder.Append('\n');
                break;
            case 'r':
                builder.Append('\r');
                break;
            case 't':
                builder.Append('\t');
                break;
            case '0':
                builder.Append('\0');
                break;
            case 'u':
                if (_position >= _text.Length || _text[_position] != '{')
                    throw Error("Invalid unicode escape", start);

                var close = _text.IndexOf('}', _position);
                var digits = close < 0 ? string.Empty : _text.Substring(_position + 1, close - _position - 1);
                if (digits.Length is < 1 or > 6 || !int.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code)
                    || code > 0x10FFFF || code is >= 0xD800 and <= 0xDFFF)
                    throw Error("Invalid unicode escape", start);

                builder.Append(char.ConvertFromUtf32(code));
                _position = close + 1;
                break;
            default:
                throw Error($"Invalid escape sequence '\\{c}'", start);
        }
    }

    private void ReadBase64(int start)
    {
        _position++;
        var builder = new StringBuilder();
        while (true)
        {
            if (_position >= _text.Length)
                throw Error("Unterminated Base64 literal", start);

            var c = _text[_position];
            if (c == '"')
            {
                _position++;
                break;
            }

            if (c == '\n')
            {
                NewLine(_position + 1);
                _newLine = false;
                continue;
            }

            if (c is not ' ' and not '\t' and not '\r')
                builder.Append(c);

            _position++;
        }

        Add(TokenKind.Base64, "b64", start, builder.ToString());
    }

    private bool Matches(int position, string text) => string.CompareOrdinal(_text, position, text, 0, text.Length) == 0 && position + text.Length <= _text.Length;

    private void Add(TokenKind kind, string text, int start, string? value = null)
    {
        AddAt(kind, text, _line, start, value);
    }

    private void AddAt(TokenKind kind, string text, int line, int start, string? value)
    {
        var column = ColumnOf(start, line);
        _tokens.Add(new Token(kind, text, line, column, _newLine, value));
        _newLine = false;
    }

    private int ColumnOf(int position, int line)
    {
        if (line == _line)
            return position - _lineStart + 1;

        var lineStart = _text.LastIndexOf('\n', Math.Max(0, position - 1)) + 1;
        return position - lineStart + 1;
    }

    private SerializationException Error(string message, int position)
    {
        var lineStart = position <= 0 ? 0 : _text.LastIndexOf('\n', position - 1) + 1;
        var line = 1;
        for (var i = 0; i < lineStart; i++)
        {
            if (_text[i] == '\n')
                line++;
        }

        return new SerializationException(message, line, position - lineStart + 1);
    }

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsHexDigit(char c) => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
}
