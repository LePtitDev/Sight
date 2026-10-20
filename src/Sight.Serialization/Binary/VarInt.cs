using System;
using System.Buffers.Binary;
using Sight.Serialization.Internal;

namespace Sight.Serialization.Binary;

/// <summary>
/// Variable-length integer encoding used by the SBDL binary format.
/// The number of leading 1 bits of the first byte gives the number of bytes that follow.
/// </summary>
public static class VarInt
{
    /// <summary>
    /// Maximum size in bytes of an encoded value
    /// </summary>
    public const int MaxSize = 9;

    /// <summary>
    /// Get the size in bytes of the encoded value
    /// </summary>
    public static int GetSize(ulong value)
    {
        var bits = 64 - BitHelper.LeadingZeroCount(value);
        if (bits <= 7)
            return 1;

        var extra = (bits - 1) / 7;
        return extra >= 8 ? MaxSize : extra + 1;
    }

    /// <summary>
    /// Get the size in bytes of an encoded value from its first byte
    /// </summary>
    public static int GetSizeFromFirstByte(byte first)
    {
        return BitHelper.LeadingOneCount(first) + 1;
    }

    /// <summary>
    /// Encode an unsigned value into a buffer of at least <see cref="GetSize"/> bytes
    /// </summary>
    /// <returns>Number of bytes written</returns>
    public static int Write(Span<byte> buffer, ulong value)
    {
        var size = GetSize(value);
        var extra = size - 1;
        if (extra == 0)
        {
            buffer[0] = (byte)value;
        }
        else if (extra == 8)
        {
            buffer[0] = 0xFF;
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(1, 8), value);
        }
        else
        {
            var prefix = (byte)(0xFF << (8 - extra));
            buffer[0] = (byte)(prefix | (byte)(value >> (8 * extra)));
            for (var i = 0; i < extra; i++)
                buffer[1 + i] = (byte)(value >> (8 * i));
        }

        return size;
    }

    /// <summary>
    /// Decode an unsigned value from a buffer
    /// </summary>
    /// <returns>Number of bytes read</returns>
    public static int Read(ReadOnlySpan<byte> buffer, out ulong value)
    {
        var first = buffer[0];
        var extra = BitHelper.LeadingOneCount(first);
        if (extra == 0)
        {
            value = first;
        }
        else if (extra == 8)
        {
            value = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(1, 8));
        }
        else
        {
            ulong low = 0;
            for (var i = 0; i < extra; i++)
                low |= (ulong)buffer[1 + i] << (8 * i);

            var high = (ulong)(first & (0x7F >> extra));
            value = low | (high << (8 * extra));
        }

        return extra + 1;
    }

    /// <summary>
    /// ZigZag transform of a signed value
    /// </summary>
    public static ulong ZigZagEncode(long value) => (ulong)((value << 1) ^ (value >> 63));

    /// <summary>
    /// Inverse ZigZag transform
    /// </summary>
    public static long ZigZagDecode(ulong value) => (long)(value >> 1) ^ -(long)(value & 1);
}
