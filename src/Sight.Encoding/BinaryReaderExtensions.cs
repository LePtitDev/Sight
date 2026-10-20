using System;
using System.Buffers.Binary;
using System.IO;
using Sight.Encoding.Internal;

namespace Sight.Encoding;

/// <summary>
/// Read values with an explicit byte order from a <see cref="BinaryReader"/> (which always reads little endian)
/// </summary>
public static class BinaryReaderExtensions
{
    /// <summary>
    /// Read a 16-bit signed integer
    /// </summary>
    public static short ReadInt16(this BinaryReader reader, Endianness endianness)
    {
        var value = reader.ReadInt16();
        return endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    /// Read a 16-bit unsigned integer
    /// </summary>
    public static ushort ReadUInt16(this BinaryReader reader, Endianness endianness)
    {
        var value = reader.ReadUInt16();
        return endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    /// Read a 32-bit signed integer
    /// </summary>
    public static int ReadInt32(this BinaryReader reader, Endianness endianness)
    {
        var value = reader.ReadInt32();
        return endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    /// Read a 32-bit unsigned integer
    /// </summary>
    public static uint ReadUInt32(this BinaryReader reader, Endianness endianness)
    {
        var value = reader.ReadUInt32();
        return endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    /// Read a 64-bit signed integer
    /// </summary>
    public static long ReadInt64(this BinaryReader reader, Endianness endianness)
    {
        var value = reader.ReadInt64();
        return endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    /// Read a 64-bit unsigned integer
    /// </summary>
    public static ulong ReadUInt64(this BinaryReader reader, Endianness endianness)
    {
        var value = reader.ReadUInt64();
        return endianness == Endianness.Little ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    /// Read a single-precision floating point value
    /// </summary>
    public static float ReadSingle(this BinaryReader reader, Endianness endianness)
    {
        return endianness == Endianness.Little
            ? reader.ReadSingle()
            : BitHelper.UInt32BitsToSingle(reader.ReadUInt32(endianness));
    }

    /// <summary>
    /// Read a double-precision floating point value
    /// </summary>
    public static double ReadDouble(this BinaryReader reader, Endianness endianness)
    {
        return endianness == Endianness.Little
            ? reader.ReadDouble()
            : BitConverter.Int64BitsToDouble(reader.ReadInt64(endianness));
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a half-precision floating point value
    /// </summary>
    public static Half ReadHalf(this BinaryReader reader, Endianness endianness)
    {
        return endianness == Endianness.Little
            ? reader.ReadHalf()
            : BitConverter.UInt16BitsToHalf(reader.ReadUInt16(endianness));
    }

#endif
}
