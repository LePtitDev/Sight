using System;

namespace Sight.Encoding;

/// <summary>
/// Write binary values into a span of bytes, advancing a cursor after each write
/// </summary>
public ref partial struct SpanWriter
{
    private readonly Span<byte> _buffer;
    private int _position;

    /// <summary>
    /// Create a writer over <paramref name="buffer"/>
    /// </summary>
    /// <param name="buffer">Bytes to write into</param>
    /// <param name="endianness">Byte order of the multi-byte values</param>
    public SpanWriter(Span<byte> buffer, Endianness endianness = Endianness.Little)
    {
        _buffer = buffer;
        _position = 0;
        Endianness = endianness;
    }

    /// <summary>
    /// Byte order of the multi-byte values
    /// </summary>
    public Endianness Endianness { get; set; }

    /// <summary>
    /// Number of bytes already written
    /// </summary>
    public int Position
    {
        readonly get => _position;
        set
        {
            if ((uint)value > (uint)_buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(value));

            _position = value;
        }
    }

    /// <summary>
    /// Total number of bytes
    /// </summary>
    public readonly int Length => _buffer.Length;

    /// <summary>
    /// Number of bytes left to write
    /// </summary>
    public readonly int Remaining => _buffer.Length - _position;

    /// <summary>
    /// Bytes already written
    /// </summary>
    public readonly Span<byte> WrittenSpan => _buffer.Slice(0, _position);

    /// <summary>
    /// Bytes left to write
    /// </summary>
    public readonly Span<byte> RemainingSpan => _buffer.Slice(_position);

    /// <summary>
    /// Write raw bytes
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (!TryWriteBytes(bytes))
            throw new ArgumentOutOfRangeException(nameof(bytes), "The buffer is too small to contain the value.");
    }

    /// <summary>
    /// Try to write raw bytes
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (!bytes.TryCopyTo(RemainingSpan))
            return false;

        _position += bytes.Length;
        return true;
    }

    /// <summary>
    /// Write a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteVarUInt32(uint value)
    {
        _position += BinaryEncoding.WriteVarUInt32(RemainingSpan, value);
    }

    /// <summary>
    /// Write a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteVarUInt64(ulong value)
    {
        _position += BinaryEncoding.WriteVarUInt64(RemainingSpan, value);
    }

    /// <summary>
    /// Write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteVarInt32(int value)
    {
        _position += BinaryEncoding.WriteVarInt32(RemainingSpan, value);
    }

    /// <summary>
    /// Write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteVarInt64(long value)
    {
        _position += BinaryEncoding.WriteVarInt64(RemainingSpan, value);
    }

    /// <summary>
    /// Try to write a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteVarUInt32(uint value)
    {
        if (!BinaryEncoding.TryWriteVarUInt32(RemainingSpan, value, out var bytesWritten))
            return false;

        _position += bytesWritten;
        return true;
    }

    /// <summary>
    /// Try to write a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteVarUInt64(ulong value)
    {
        if (!BinaryEncoding.TryWriteVarUInt64(RemainingSpan, value, out var bytesWritten))
            return false;

        _position += bytesWritten;
        return true;
    }

    /// <summary>
    /// Try to write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteVarInt32(int value)
    {
        if (!BinaryEncoding.TryWriteVarInt32(RemainingSpan, value, out var bytesWritten))
            return false;

        _position += bytesWritten;
        return true;
    }

    /// <summary>
    /// Try to write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteVarInt64(long value)
    {
        if (!BinaryEncoding.TryWriteVarInt64(RemainingSpan, value, out var bytesWritten))
            return false;

        _position += bytesWritten;
        return true;
    }

    /// <summary>
    /// Write a string as UTF-8 bytes prefixed by their count as a variable-length integer
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteString(string value)
    {
        _position += BinaryEncoding.WriteString(RemainingSpan, value);
    }

    /// <summary>
    /// Try to write a string as UTF-8 bytes prefixed by their count as a variable-length integer
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteString(string value)
    {
        if (!BinaryEncoding.TryWriteString(RemainingSpan, value, out var bytesWritten))
            return false;

        _position += bytesWritten;
        return true;
    }
}
