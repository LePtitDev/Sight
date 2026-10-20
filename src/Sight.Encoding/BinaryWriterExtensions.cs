using System;
using System.Buffers.Binary;
using System.IO;
using Sight.Encoding.Internal;

namespace Sight.Encoding;

/// <summary>
/// Write values with an explicit byte order into a <see cref="BinaryWriter"/> (which always writes little endian)
/// </summary>
public static class BinaryWriterExtensions
{
    /// <summary>
    /// Write a 16-bit signed integer
    /// </summary>
    public static void WriteInt16(this BinaryWriter writer, short value, Endianness endianness)
    {
        writer.Write(endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value));
    }

    /// <summary>
    /// Write a 16-bit unsigned integer
    /// </summary>
    public static void WriteUInt16(this BinaryWriter writer, ushort value, Endianness endianness)
    {
        writer.Write(endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value));
    }

    /// <summary>
    /// Write a 32-bit signed integer
    /// </summary>
    public static void WriteInt32(this BinaryWriter writer, int value, Endianness endianness)
    {
        writer.Write(endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value));
    }

    /// <summary>
    /// Write a 32-bit unsigned integer
    /// </summary>
    public static void WriteUInt32(this BinaryWriter writer, uint value, Endianness endianness)
    {
        writer.Write(endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value));
    }

    /// <summary>
    /// Write a 64-bit signed integer
    /// </summary>
    public static void WriteInt64(this BinaryWriter writer, long value, Endianness endianness)
    {
        writer.Write(endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value));
    }

    /// <summary>
    /// Write a 64-bit unsigned integer
    /// </summary>
    public static void WriteUInt64(this BinaryWriter writer, ulong value, Endianness endianness)
    {
        writer.Write(endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value));
    }

    /// <summary>
    /// Write a single-precision floating point value
    /// </summary>
    public static void WriteSingle(this BinaryWriter writer, float value, Endianness endianness)
    {
        if (endianness == Endianness.Little)
            writer.Write(value);
        else
            writer.WriteUInt32(BitHelper.SingleToUInt32Bits(value), endianness);
    }

    /// <summary>
    /// Write a double-precision floating point value
    /// </summary>
    public static void WriteDouble(this BinaryWriter writer, double value, Endianness endianness)
    {
        if (endianness == Endianness.Little)
            writer.Write(value);
        else
            writer.WriteInt64(BitConverter.DoubleToInt64Bits(value), endianness);
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Write a half-precision floating point value
    /// </summary>
    public static void WriteHalf(this BinaryWriter writer, Half value, Endianness endianness)
    {
        if (endianness == Endianness.Little)
            writer.Write(value);
        else
            writer.WriteUInt16(BitConverter.HalfToUInt16Bits(value), endianness);
    }

#endif
}
