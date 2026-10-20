using System;

namespace Sight.Serialization;

/// <summary>
/// Error raised when data, a schema or a document is invalid or cannot be (de)serialized
/// </summary>
public class SerializationException : Exception
{
    /// <summary>
    /// Initialize a new instance of <see cref="SerializationException"/>
    /// </summary>
    public SerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initialize a new instance of <see cref="SerializationException"/>
    /// </summary>
    public SerializationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initialize a new instance of <see cref="SerializationException"/> located in a text document
    /// </summary>
    public SerializationException(string message, int line, int column)
        : base($"{message} (line {line}, column {column})")
    {
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Line of the error in a text document (1-based), 0 if not applicable
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column of the error in a text document (1-based), 0 if not applicable
    /// </summary>
    public int Column { get; }
}
