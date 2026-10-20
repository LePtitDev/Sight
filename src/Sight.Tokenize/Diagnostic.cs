namespace Sight.Tokenize;

/// <summary>
/// Describe a problem found while tokenizing a text
/// </summary>
public readonly struct Diagnostic
{
    /// <summary>
    /// Initialize a new instance of the struct <see cref="Diagnostic"/>
    /// </summary>
    public Diagnostic(string code, string message, TextSpan span)
    {
        Code = code;
        Message = message;
        Span = span;
    }

    /// <summary>
    /// Stable identifier of the problem (LEX... for the lexer itself)
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Human readable message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Range of the text concerned by the problem
    /// </summary>
    public TextSpan Span { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Code} {Span}: {Message}";
}
