using System;

namespace Sight.Encoding;

public static partial class BinaryEncoding
{
    /// <summary>
    /// Maximum size in bytes of an encoded 32-bit variable-length integer
    /// </summary>
    public const int MaxVarInt32Size = 5;

    /// <summary>
    /// Maximum size in bytes of an encoded 64-bit variable-length integer
    /// </summary>
    public const int MaxVarInt64Size = 10;

    /// <summary>
    /// Get the size in bytes of an encoded variable-length unsigned integer
    /// </summary>
    public static int GetVarUInt32Size(uint value) => GetVarUInt64Size(value);

    /// <summary>
    /// Get the size in bytes of an encoded variable-length unsigned integer
    /// </summary>
    public static int GetVarUInt64Size(ulong value)
    {
        var size = 1;
        while (value >= 0x80)
        {
            value >>= 7;
            size++;
        }

        return size;
    }

    /// <summary>
    /// Get the size in bytes of an encoded variable-length signed integer
    /// </summary>
    public static int GetVarInt32Size(int value) => GetVarUInt32Size(ZigZag.Encode(value));

    /// <summary>
    /// Get the size in bytes of an encoded variable-length signed integer
    /// </summary>
    public static int GetVarInt64Size(long value) => GetVarUInt64Size(ZigZag.Encode(value));

    /// <summary>
    /// Write a variable-length unsigned integer (LEB128: 7 bits per byte, the high bit flags a following byte).
    /// Same format as <c>BinaryWriter.Write7BitEncodedInt</c>.
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <returns>Number of bytes written</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static int WriteVarUInt32(Span<byte> destination, uint value) => WriteVarUInt64(destination, value);

    /// <summary>
    /// Write a variable-length unsigned integer (LEB128: 7 bits per byte, the high bit flags a following byte)
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <returns>Number of bytes written</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static int WriteVarUInt64(Span<byte> destination, ulong value)
    {
        if (!TryWriteVarUInt64(destination, value, out var bytesWritten))
            ThrowTooSmall(nameof(destination));

        return bytesWritten;
    }

    /// <summary>
    /// Write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <returns>Number of bytes written</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static int WriteVarInt32(Span<byte> destination, int value) => WriteVarUInt32(destination, ZigZag.Encode(value));

    /// <summary>
    /// Write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <returns>Number of bytes written</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static int WriteVarInt64(Span<byte> destination, long value) => WriteVarUInt64(destination, ZigZag.Encode(value));

    /// <summary>
    /// Try to write a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="bytesWritten">Number of bytes written</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteVarUInt32(Span<byte> destination, uint value, out int bytesWritten) => TryWriteVarUInt64(destination, value, out bytesWritten);

    /// <summary>
    /// Try to write a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="bytesWritten">Number of bytes written</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteVarUInt64(Span<byte> destination, ulong value, out int bytesWritten)
    {
        var size = GetVarUInt64Size(value);
        if (destination.Length < size)
        {
            bytesWritten = 0;
            return false;
        }

        var index = 0;
        while (value >= 0x80)
        {
            destination[index++] = (byte)(value | 0x80);
            value >>= 7;
        }

        destination[index] = (byte)value;
        bytesWritten = size;
        return true;
    }

    /// <summary>
    /// Try to write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="bytesWritten">Number of bytes written</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteVarInt32(Span<byte> destination, int value, out int bytesWritten) => TryWriteVarUInt32(destination, ZigZag.Encode(value), out bytesWritten);

    /// <summary>
    /// Try to write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="bytesWritten">Number of bytes written</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteVarInt64(Span<byte> destination, long value, out int bytesWritten) => TryWriteVarUInt64(destination, ZigZag.Encode(value), out bytesWritten);

    /// <summary>
    /// Read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> ends before the value.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static uint ReadVarUInt32(ReadOnlySpan<byte> source, out int bytesRead)
    {
        if (!TryReadVarUInt32(source, out var value, out bytesRead))
            ThrowTooSmall(nameof(source));

        return value;
    }

    /// <summary>
    /// Read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> ends before the value.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static ulong ReadVarUInt64(ReadOnlySpan<byte> source, out int bytesRead)
    {
        if (!TryReadVarUInt64(source, out var value, out bytesRead))
            ThrowTooSmall(nameof(source));

        return value;
    }

    /// <summary>
    /// Read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> ends before the value.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static int ReadVarInt32(ReadOnlySpan<byte> source, out int bytesRead) => ZigZag.Decode(ReadVarUInt32(source, out bytesRead));

    /// <summary>
    /// Read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> ends before the value.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static long ReadVarInt64(ReadOnlySpan<byte> source, out int bytesRead) => ZigZag.Decode(ReadVarUInt64(source, out bytesRead));

    /// <summary>
    /// Try to read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <returns><c>false</c> if <paramref name="source"/> ends before the value</returns>
    /// <exception cref="FormatException">The encoded value is too long or overflows, more bytes would not help.</exception>
    public static bool TryReadVarUInt32(ReadOnlySpan<byte> source, out uint value, out int bytesRead)
    {
        var result = TryReadVarInteger(source, 32, out var value64, out bytesRead);
        value = (uint)value64;
        return result;
    }

    /// <summary>
    /// Try to read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <returns><c>false</c> if <paramref name="source"/> ends before the value</returns>
    /// <exception cref="FormatException">The encoded value is too long or overflows, more bytes would not help.</exception>
    public static bool TryReadVarUInt64(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        return TryReadVarInteger(source, 64, out value, out bytesRead);
    }

    /// <summary>
    /// Try to read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <returns><c>false</c> if <paramref name="source"/> ends before the value</returns>
    /// <exception cref="FormatException">The encoded value is too long or overflows, more bytes would not help.</exception>
    public static bool TryReadVarInt32(ReadOnlySpan<byte> source, out int value, out int bytesRead)
    {
        var result = TryReadVarUInt32(source, out var encoded, out bytesRead);
        value = ZigZag.Decode(encoded);
        return result;
    }

    /// <summary>
    /// Try to read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <returns><c>false</c> if <paramref name="source"/> ends before the value</returns>
    /// <exception cref="FormatException">The encoded value is too long or overflows, more bytes would not help.</exception>
    public static bool TryReadVarInt64(ReadOnlySpan<byte> source, out long value, out int bytesRead)
    {
        var result = TryReadVarUInt64(source, out var encoded, out bytesRead);
        value = ZigZag.Decode(encoded);
        return result;
    }

    /// <summary>
    /// Add a byte to a variable-length integer being decoded
    /// </summary>
    /// <returns><c>true</c> if it was the last byte of the value</returns>
    internal static bool AccumulateVarInteger(ref ulong value, int index, byte encoded, int bits)
    {
        // The last possible byte only holds the remaining bits, without continuation flag
        var shift = 7 * index;
        if (bits - shift <= 7 && encoded >> (bits - shift) != 0)
            throw new FormatException("The variable-length integer is too long or overflows.");

        value |= (ulong)(encoded & 0x7F) << shift;
        return (encoded & 0x80) == 0;
    }

    private static bool TryReadVarInteger(ReadOnlySpan<byte> source, int bits, out ulong value, out int bytesRead)
    {
        value = 0;
        for (var index = 0; index < source.Length; index++)
        {
            if (AccumulateVarInteger(ref value, index, source[index], bits))
            {
                bytesRead = index + 1;
                return true;
            }
        }

        value = 0;
        bytesRead = 0;
        return false;
    }
}
