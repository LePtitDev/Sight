using System;
using System.Diagnostics.CodeAnalysis;

namespace Sight.Encoding;

/// <summary>
/// Read binary values from a span of bytes, advancing a cursor after each read
/// </summary>
public ref partial struct SpanReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    /// <summary>
    /// Create a reader over <paramref name="buffer"/>
    /// </summary>
    /// <param name="buffer">Bytes to read from</param>
    /// <param name="endianness">Byte order of the multi-byte values</param>
    public SpanReader(ReadOnlySpan<byte> buffer, Endianness endianness = Endianness.Little)
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
    /// Number of bytes already read
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
    /// Number of bytes left to read
    /// </summary>
    public readonly int Remaining => _buffer.Length - _position;

    /// <summary>
    /// Whether all the bytes have been read
    /// </summary>
    public readonly bool IsAtEnd => _position == _buffer.Length;

    /// <summary>
    /// Bytes left to read
    /// </summary>
    public readonly ReadOnlySpan<byte> RemainingSpan => _buffer.Slice(_position);

    /// <summary>
    /// Skip bytes
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public void Skip(int count)
    {
        if ((uint)count > (uint)Remaining)
            throw new ArgumentOutOfRangeException(nameof(count));

        _position += count;
    }

    /// <summary>
    /// Read raw bytes, the returned span points into the underlying buffer
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        if ((uint)count > (uint)Remaining)
            throw new ArgumentOutOfRangeException(nameof(count));

        var bytes = _buffer.Slice(_position, count);
        _position += count;
        return bytes;
    }

    /// <summary>
    /// Try to read raw bytes, the returned span points into the underlying buffer
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadBytes(int count, out ReadOnlySpan<byte> bytes)
    {
        if ((uint)count > (uint)Remaining)
        {
            bytes = default;
            return false;
        }

        bytes = _buffer.Slice(_position, count);
        _position += count;
        return true;
    }

    /// <summary>
    /// Read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public uint ReadVarUInt32()
    {
        var value = BinaryEncoding.ReadVarUInt32(RemainingSpan, out var bytesRead);
        _position += bytesRead;
        return value;
    }

    /// <summary>
    /// Read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public ulong ReadVarUInt64()
    {
        var value = BinaryEncoding.ReadVarUInt64(RemainingSpan, out var bytesRead);
        _position += bytesRead;
        return value;
    }

    /// <summary>
    /// Read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public int ReadVarInt32()
    {
        var value = BinaryEncoding.ReadVarInt32(RemainingSpan, out var bytesRead);
        _position += bytesRead;
        return value;
    }

    /// <summary>
    /// Read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public long ReadVarInt64()
    {
        var value = BinaryEncoding.ReadVarInt64(RemainingSpan, out var bytesRead);
        _position += bytesRead;
        return value;
    }

    /// <summary>
    /// Try to read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public bool TryReadVarUInt32(out uint value)
    {
        if (!BinaryEncoding.TryReadVarUInt32(RemainingSpan, out value, out var bytesRead))
            return false;

        _position += bytesRead;
        return true;
    }

    /// <summary>
    /// Try to read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public bool TryReadVarUInt64(out ulong value)
    {
        if (!BinaryEncoding.TryReadVarUInt64(RemainingSpan, out value, out var bytesRead))
            return false;

        _position += bytesRead;
        return true;
    }

    /// <summary>
    /// Try to read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public bool TryReadVarInt32(out int value)
    {
        if (!BinaryEncoding.TryReadVarInt32(RemainingSpan, out value, out var bytesRead))
            return false;

        _position += bytesRead;
        return true;
    }

    /// <summary>
    /// Try to read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public bool TryReadVarInt64(out long value)
    {
        if (!BinaryEncoding.TryReadVarInt64(RemainingSpan, out value, out var bytesRead))
            return false;

        _position += bytesRead;
        return true;
    }

    /// <summary>
    /// Read a string written by <see cref="SpanWriter.WriteString"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    /// <exception cref="FormatException">The length prefix is invalid.</exception>
    public string ReadString()
    {
        var value = BinaryEncoding.ReadString(RemainingSpan, out var bytesRead);
        _position += bytesRead;
        return value;
    }

    /// <summary>
    /// Try to read a string written by <see cref="SpanWriter.WriteString"/>
    /// </summary>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    /// <exception cref="FormatException">The length prefix is invalid.</exception>
    public bool TryReadString([NotNullWhen(true)] out string? value)
    {
        if (!BinaryEncoding.TryReadString(RemainingSpan, out value, out var bytesRead))
            return false;

        _position += bytesRead;
        return true;
    }
}
