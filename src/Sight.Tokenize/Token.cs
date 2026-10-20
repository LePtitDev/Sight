using System;

namespace Sight.Tokenize;

/// <summary>
/// Describe a token of a text
/// </summary>
/// <typeparam name="TKind">Enumeration of token kinds</typeparam>
public readonly struct Token<TKind>
    where TKind : struct, Enum
{
    /// <summary>
    /// Initialize a new instance of the struct <see cref="Token{TKind}"/>
    /// </summary>
    public Token(TKind kind, TextSpan span, bool isError = false)
    {
        Kind = kind;
        Span = span;
        IsError = isError;
    }

    /// <summary>
    /// Kind of the token
    /// </summary>
    public TKind Kind { get; }

    /// <summary>
    /// Range of the token in the text
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// Indicates if the token is invalid (unexpected character, unterminated delimiter, etc.), see <see cref="TokenizeResult{TKind}.Diagnostics"/>
    /// </summary>
    public bool IsError { get; }

    /// <inheritdoc />
    public override string ToString() => IsError ? $"{Kind} {Span} (error)" : $"{Kind} {Span}";
}
