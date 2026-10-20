using System;
using System.Collections.Generic;

namespace Sight.Tokenize;

/// <summary>
/// Describe the result of a tokenization (the tokenization never fails: problems are reported as diagnostics)
/// </summary>
/// <typeparam name="TKind">Enumeration of token kinds</typeparam>
public sealed class TokenizeResult<TKind>
    where TKind : struct, Enum
{
    private int[]? _lineStarts;

    internal TokenizeResult(string source, IReadOnlyList<Token<TKind>> tokens, IReadOnlyList<Diagnostic> diagnostics, bool hasErrors)
    {
        Source = source;
        Tokens = tokens;
        Diagnostics = diagnostics;
        HasErrors = hasErrors;
    }

    /// <summary>
    /// Tokenized text
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Tokens in the order of the text
    /// </summary>
    public IReadOnlyList<Token<TKind>> Tokens { get; }

    /// <summary>
    /// Problems found in the text (limited by <see cref="LexerOptions.MaxDiagnostics"/>)
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Indicates if at least one error was found (even when the diagnostics list is truncated)
    /// </summary>
    public bool HasErrors { get; }

    /// <summary>
    /// Number of lines of the text
    /// </summary>
    public int LineCount => LineStarts.Length;

    /// <summary>
    /// Get the text of a token
    /// </summary>
    public string GetText(Token<TKind> token) => GetText(token.Span);

    /// <summary>
    /// Get the text of a range
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public string GetText(TextSpan span)
    {
        CheckSpan(span);
        return Source.Substring(span.Start, span.Length);
    }

    /// <summary>
    /// Get the text of a token without allocation
    /// </summary>
    public ReadOnlySpan<char> GetSpan(Token<TKind> token) => GetSpan(token.Span);

    /// <summary>
    /// Get the text of a range without allocation
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ReadOnlySpan<char> GetSpan(TextSpan span)
    {
        CheckSpan(span);
        return Source.AsSpan(span.Start, span.Length);
    }

    /// <summary>
    /// Get the line and column of the start of a token
    /// </summary>
    public LineColumn GetLineColumn(Token<TKind> token) => GetLineColumn(token.Span.Start);

    /// <summary>
    /// Get the line and column of the start of a range
    /// </summary>
    public LineColumn GetLineColumn(TextSpan span) => GetLineColumn(span.Start);

    /// <summary>
    /// Get the line and column of a text index (the line starts table is built on the first call)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public LineColumn GetLineColumn(int index)
    {
        if (index < 0 || index > Source.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        var starts = LineStarts;
        var line = Array.BinarySearch(starts, index);
        if (line < 0)
            line = ~line - 1;

        return new LineColumn(line + 1, index - starts[line] + 1);
    }

    private int[] LineStarts => _lineStarts ??= ComputeLineStarts(Source);

    private void CheckSpan(TextSpan span)
    {
        if (span.End > Source.Length)
            throw new ArgumentOutOfRangeException(nameof(span), "The span is out of the text");
    }

    // Line breaks are "\n", "\r\n" and "\r"
    private static int[] ComputeLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                i++;
            else if (c != '\r' && c != '\n')
                continue;

            starts.Add(i + 1);
        }

        return starts.ToArray();
    }
}
