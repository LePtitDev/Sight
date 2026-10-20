using System;

namespace Sight.Tokenize;

/// <summary>
/// Describe a range of a text, in UTF-16 code units (like <see cref="string.Substring(int,int)"/>)
/// </summary>
public readonly struct TextSpan : IEquatable<TextSpan>
{
    /// <summary>
    /// Initialize a new instance of the struct <see cref="TextSpan"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public TextSpan(int start, int length)
    {
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), "Start must be positive");

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive");

        Start = start;
        Length = length;
    }

    /// <summary>
    /// Index of the first character
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Number of characters
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Index after the last character
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// Indicates if the span contains no character
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// Indicates if the character at the given index is in the span
    /// </summary>
    public bool Contains(int index) => index >= Start && index < End;

    /// <inheritdoc />
    public bool Equals(TextSpan other) => Start == other.Start && Length == other.Length;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TextSpan other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((Start * 397) ^ Length);

    /// <inheritdoc />
    public override string ToString() => $"[{Start}..{End})";

    /// <summary>
    /// Compare two spans
    /// </summary>
    public static bool operator ==(TextSpan left, TextSpan right) => left.Equals(right);

    /// <summary>
    /// Compare two spans
    /// </summary>
    public static bool operator !=(TextSpan left, TextSpan right) => !left.Equals(right);
}
