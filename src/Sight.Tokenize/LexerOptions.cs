using System;

namespace Sight.Tokenize;

/// <summary>
/// Describe the options and the safeguards of a <see cref="Lexer{TKind}"/>
/// </summary>
public sealed class LexerOptions
{
    private int _maxTokenLength = int.MaxValue;
    private int _maxDiagnostics = 1000;
    private int _maxModeDepth = 256;

    /// <summary>
    /// Indicates if the trivia tokens (whitespaces, comments) are emitted (default: false).
    /// When true, the concatenation of all tokens gives exactly the source text.
    /// </summary>
    public bool EmitTrivia { get; set; }

    /// <summary>
    /// Maximum length of a token (default: no limit). A longer match is cut and reported as an error token.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int MaxTokenLength
    {
        get => _maxTokenLength;
        set => _maxTokenLength = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Value must be greater than 0");
    }

    /// <summary>
    /// Maximum number of stored diagnostics (default: 1000). Over this limit errors are still reported by error tokens
    /// and <see cref="TokenizeResult{TKind}.HasErrors"/>, but diagnostics are dropped.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int MaxDiagnostics
    {
        get => _maxDiagnostics;
        set => _maxDiagnostics = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Value must be positive");
    }

    /// <summary>
    /// Maximum depth of the mode stack (default: 256). A deeper push is ignored and reported.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int MaxModeDepth
    {
        get => _maxModeDepth;
        set => _maxModeDepth = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Value must be greater than 0");
    }

    internal LexerOptions Clone() => new LexerOptions
    {
        EmitTrivia = EmitTrivia,
        MaxTokenLength = MaxTokenLength,
        MaxDiagnostics = MaxDiagnostics,
        MaxModeDepth = MaxModeDepth
    };
}
