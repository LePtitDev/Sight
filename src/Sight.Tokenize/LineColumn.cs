using System;

namespace Sight.Tokenize;

/// <summary>
/// Describe a position in a text as line and column (both start at 1, the column is expressed in UTF-16 code units)
/// </summary>
public readonly struct LineColumn : IEquatable<LineColumn>
{
    /// <summary>
    /// Initialize a new instance of the struct <see cref="LineColumn"/>
    /// </summary>
    public LineColumn(int line, int column)
    {
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Line number (starts at 1)
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number (starts at 1)
    /// </summary>
    public int Column { get; }

    /// <inheritdoc />
    public bool Equals(LineColumn other) => Line == other.Line && Column == other.Column;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LineColumn other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((Line * 397) ^ Column);

    /// <inheritdoc />
    public override string ToString() => $"{Line}:{Column}";

    /// <summary>
    /// Compare two positions
    /// </summary>
    public static bool operator ==(LineColumn left, LineColumn right) => left.Equals(right);

    /// <summary>
    /// Compare two positions
    /// </summary>
    public static bool operator !=(LineColumn left, LineColumn right) => !left.Equals(right);
}
