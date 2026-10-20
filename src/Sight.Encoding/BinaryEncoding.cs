using System;
using System.Buffers.Binary;
using Sight.Encoding.Internal;

namespace Sight.Encoding;

/// <summary>
/// Read and write binary values from/into spans of bytes
/// </summary>
public static partial class BinaryEncoding
{
    internal const int GuidSize = 16;
    internal const int HalfSize = 2;
    internal const int Int128Size = 16;

    /// <summary>
    /// Byte order of the current platform
    /// </summary>
    public static Endianness NativeEndianness => BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big;

    /// <summary>
    /// Read an 8-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static sbyte ReadInt8(ReadOnlySpan<byte> source)
    {
        return (sbyte)ReadUInt8(source);
    }

    /// <summary>
    /// Try to read an 8-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadInt8(ReadOnlySpan<byte> source, out sbyte value)
    {
        if (source.Length < sizeof(sbyte))
        {
            value = default;
            return false;
        }

        value = ReadInt8(source);
        return true;
    }

    /// <summary>
    /// Write an 8-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteInt8(Span<byte> destination, sbyte value)
    {
        WriteUInt8(destination, (byte)value);
    }

    /// <summary>
    /// Try to write an 8-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteInt8(Span<byte> destination, sbyte value)
    {
        if (destination.Length < sizeof(sbyte))
            return false;

        WriteInt8(destination, value);
        return true;
    }

    /// <summary>
    /// Read an 8-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static byte ReadUInt8(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
            ThrowTooSmall(nameof(source));

        return source[0];
    }

    /// <summary>
    /// Try to read an 8-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadUInt8(ReadOnlySpan<byte> source, out byte value)
    {
        if (source.Length < sizeof(byte))
        {
            value = default;
            return false;
        }

        value = ReadUInt8(source);
        return true;
    }

    /// <summary>
    /// Write an 8-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteUInt8(Span<byte> destination, byte value)
    {
        if (destination.IsEmpty)
            ThrowTooSmall(nameof(destination));

        destination[0] = value;
    }

    /// <summary>
    /// Try to write an 8-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteUInt8(Span<byte> destination, byte value)
    {
        if (destination.Length < sizeof(byte))
            return false;

        WriteUInt8(destination, value);
        return true;
    }

    /// <summary>
    /// Read a boolean (1 byte, any non-zero value is true) from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static bool ReadBoolean(ReadOnlySpan<byte> source)
    {
        return ReadUInt8(source) != 0;
    }

    /// <summary>
    /// Try to read a boolean (1 byte, any non-zero value is true) from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadBoolean(ReadOnlySpan<byte> source, out bool value)
    {
        if (source.Length < sizeof(bool))
        {
            value = default;
            return false;
        }

        value = ReadBoolean(source);
        return true;
    }

    /// <summary>
    /// Write a boolean (1 byte, any non-zero value is true) at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteBoolean(Span<byte> destination, bool value)
    {
        WriteUInt8(destination, value ? (byte)1 : (byte)0);
    }

    /// <summary>
    /// Try to write a boolean (1 byte, any non-zero value is true) at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteBoolean(Span<byte> destination, bool value)
    {
        if (destination.Length < sizeof(bool))
            return false;

        WriteBoolean(destination, value);
        return true;
    }

    /// <summary>
    /// Read a 16-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static short ReadInt16(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadInt16LittleEndian(source)
            : BinaryPrimitives.ReadInt16BigEndian(source);
    }

    /// <summary>
    /// Try to read a 16-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadInt16(ReadOnlySpan<byte> source, out short value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(short))
        {
            value = default;
            return false;
        }

        value = ReadInt16(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a 16-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteInt16(Span<byte> destination, short value, Endianness endianness = Endianness.Little)
    {
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteInt16LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteInt16BigEndian(destination, value);
    }

    /// <summary>
    /// Try to write a 16-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteInt16(Span<byte> destination, short value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(short))
            return false;

        WriteInt16(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a 16-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static ushort ReadUInt16(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadUInt16LittleEndian(source)
            : BinaryPrimitives.ReadUInt16BigEndian(source);
    }

    /// <summary>
    /// Try to read a 16-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadUInt16(ReadOnlySpan<byte> source, out ushort value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(ushort))
        {
            value = default;
            return false;
        }

        value = ReadUInt16(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a 16-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteUInt16(Span<byte> destination, ushort value, Endianness endianness = Endianness.Little)
    {
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteUInt16BigEndian(destination, value);
    }

    /// <summary>
    /// Try to write a 16-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteUInt16(Span<byte> destination, ushort value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(ushort))
            return false;

        WriteUInt16(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a 32-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static int ReadInt32(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadInt32LittleEndian(source)
            : BinaryPrimitives.ReadInt32BigEndian(source);
    }

    /// <summary>
    /// Try to read a 32-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadInt32(ReadOnlySpan<byte> source, out int value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(int))
        {
            value = default;
            return false;
        }

        value = ReadInt32(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a 32-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteInt32(Span<byte> destination, int value, Endianness endianness = Endianness.Little)
    {
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteInt32LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteInt32BigEndian(destination, value);
    }

    /// <summary>
    /// Try to write a 32-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteInt32(Span<byte> destination, int value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(int))
            return false;

        WriteInt32(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a 32-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static uint ReadUInt32(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadUInt32LittleEndian(source)
            : BinaryPrimitives.ReadUInt32BigEndian(source);
    }

    /// <summary>
    /// Try to read a 32-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadUInt32(ReadOnlySpan<byte> source, out uint value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(uint))
        {
            value = default;
            return false;
        }

        value = ReadUInt32(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a 32-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteUInt32(Span<byte> destination, uint value, Endianness endianness = Endianness.Little)
    {
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteUInt32BigEndian(destination, value);
    }

    /// <summary>
    /// Try to write a 32-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteUInt32(Span<byte> destination, uint value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(uint))
            return false;

        WriteUInt32(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a 64-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static long ReadInt64(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadInt64LittleEndian(source)
            : BinaryPrimitives.ReadInt64BigEndian(source);
    }

    /// <summary>
    /// Try to read a 64-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadInt64(ReadOnlySpan<byte> source, out long value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(long))
        {
            value = default;
            return false;
        }

        value = ReadInt64(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a 64-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteInt64(Span<byte> destination, long value, Endianness endianness = Endianness.Little)
    {
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteInt64LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteInt64BigEndian(destination, value);
    }

    /// <summary>
    /// Try to write a 64-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteInt64(Span<byte> destination, long value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(long))
            return false;

        WriteInt64(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a 64-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static ulong ReadUInt64(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadUInt64LittleEndian(source)
            : BinaryPrimitives.ReadUInt64BigEndian(source);
    }

    /// <summary>
    /// Try to read a 64-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadUInt64(ReadOnlySpan<byte> source, out ulong value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(ulong))
        {
            value = default;
            return false;
        }

        value = ReadUInt64(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a 64-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteUInt64(Span<byte> destination, ulong value, Endianness endianness = Endianness.Little)
    {
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteUInt64BigEndian(destination, value);
    }

    /// <summary>
    /// Try to write a 64-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteUInt64(Span<byte> destination, ulong value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(ulong))
            return false;

        WriteUInt64(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a single-precision floating point value from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static float ReadSingle(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
#if NET8_0_OR_GREATER
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadSingleLittleEndian(source)
            : BinaryPrimitives.ReadSingleBigEndian(source);
#else
        return BitHelper.UInt32BitsToSingle(ReadUInt32(source, endianness));
#endif
    }

    /// <summary>
    /// Try to read a single-precision floating point value from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadSingle(ReadOnlySpan<byte> source, out float value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(float))
        {
            value = default;
            return false;
        }

        value = ReadSingle(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a single-precision floating point value at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteSingle(Span<byte> destination, float value, Endianness endianness = Endianness.Little)
    {
#if NET8_0_OR_GREATER
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteSingleLittleEndian(destination, value);
        else
            BinaryPrimitives.WriteSingleBigEndian(destination, value);
#else
        WriteUInt32(destination, BitHelper.SingleToUInt32Bits(value), endianness);
#endif
    }

    /// <summary>
    /// Try to write a single-precision floating point value at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteSingle(Span<byte> destination, float value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(float))
            return false;

        WriteSingle(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a double-precision floating point value from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static double ReadDouble(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
#if NET8_0_OR_GREATER
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadDoubleLittleEndian(source)
            : BinaryPrimitives.ReadDoubleBigEndian(source);
#else
        return BitConverter.Int64BitsToDouble(ReadInt64(source, endianness));
#endif
    }

    /// <summary>
    /// Try to read a double-precision floating point value from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadDouble(ReadOnlySpan<byte> source, out double value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(double))
        {
            value = default;
            return false;
        }

        value = ReadDouble(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a double-precision floating point value at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteDouble(Span<byte> destination, double value, Endianness endianness = Endianness.Little)
    {
#if NET8_0_OR_GREATER
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteDoubleLittleEndian(destination, value);
        else
            BinaryPrimitives.WriteDoubleBigEndian(destination, value);
#else
        WriteInt64(destination, BitConverter.DoubleToInt64Bits(value), endianness);
#endif
    }

    /// <summary>
    /// Try to write a double-precision floating point value at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteDouble(Span<byte> destination, double value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(double))
            return false;

        WriteDouble(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a UTF-16 code unit (2 bytes) from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static char ReadChar(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return (char)ReadUInt16(source, endianness);
    }

    /// <summary>
    /// Try to read a UTF-16 code unit (2 bytes) from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadChar(ReadOnlySpan<byte> source, out char value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(char))
        {
            value = default;
            return false;
        }

        value = ReadChar(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a UTF-16 code unit (2 bytes) at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteChar(Span<byte> destination, char value, Endianness endianness = Endianness.Little)
    {
        WriteUInt16(destination, value, endianness);
    }

    /// <summary>
    /// Try to write a UTF-16 code unit (2 bytes) at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteChar(Span<byte> destination, char value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(char))
            return false;

        WriteChar(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static decimal ReadDecimal(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(decimal))
            ThrowTooSmall(nameof(source));

        var lo = ReadInt32(source, endianness);
        var mid = ReadInt32(source.Slice(4), endianness);
        var hi = ReadInt32(source.Slice(8), endianness);
        var flags = ReadInt32(source.Slice(12), endianness);
        try
        {
#if NET8_0_OR_GREATER
            return new decimal(stackalloc int[] { lo, mid, hi, flags });
#else
            return new decimal(new[] { lo, mid, hi, flags });
#endif
        }
        catch (ArgumentException ex)
        {
            throw new FormatException("Invalid decimal representation.", ex);
        }
    }

    /// <summary>
    /// Try to read a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadDecimal(ReadOnlySpan<byte> source, out decimal value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < sizeof(decimal))
        {
            value = default;
            return false;
        }

        value = ReadDecimal(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteDecimal(Span<byte> destination, decimal value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(decimal))
            ThrowTooSmall(nameof(destination));

#if NET8_0_OR_GREATER
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
#else
        var bits = decimal.GetBits(value);
#endif
        WriteInt32(destination, bits[0], endianness);
        WriteInt32(destination.Slice(4), bits[1], endianness);
        WriteInt32(destination.Slice(8), bits[2], endianness);
        WriteInt32(destination.Slice(12), bits[3], endianness);
    }

    /// <summary>
    /// Try to write a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteDecimal(Span<byte> destination, decimal value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < sizeof(decimal))
            return false;

        WriteDecimal(destination, value, endianness);
        return true;
    }

    /// <summary>
    /// Read a GUID (16 bytes, big endian follows RFC 4122) from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static Guid ReadGuid(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        if (source.Length < GuidSize)
            ThrowTooSmall(nameof(source));

#if NET8_0_OR_GREATER
        return new Guid(source.Slice(0, GuidSize), endianness == Endianness.Big);
#else
        var bytes = source.Slice(0, GuidSize).ToArray();
        if (endianness == Endianness.Big)
            SwapGuidByteOrder(bytes);

        return new Guid(bytes);
#endif
    }

    /// <summary>
    /// Try to read a GUID (16 bytes, big endian follows RFC 4122) from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadGuid(ReadOnlySpan<byte> source, out Guid value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < GuidSize)
        {
            value = default;
            return false;
        }

        value = ReadGuid(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a GUID (16 bytes, big endian follows RFC 4122) at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteGuid(Span<byte> destination, Guid value, Endianness endianness = Endianness.Little)
    {
#if NET8_0_OR_GREATER
        if (!value.TryWriteBytes(destination, endianness == Endianness.Big, out _))
            ThrowTooSmall(nameof(destination));
#else
        if (destination.Length < GuidSize)
            ThrowTooSmall(nameof(destination));

        var bytes = value.ToByteArray();
        if (endianness == Endianness.Big)
            SwapGuidByteOrder(bytes);

        bytes.CopyTo(destination);
#endif
    }

    /// <summary>
    /// Try to write a GUID (16 bytes, big endian follows RFC 4122) at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteGuid(Span<byte> destination, Guid value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < GuidSize)
            return false;

        WriteGuid(destination, value, endianness);
        return true;
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a half-precision floating point value from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static Half ReadHalf(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadHalfLittleEndian(source)
            : BinaryPrimitives.ReadHalfBigEndian(source);
    }

    /// <summary>
    /// Try to read a half-precision floating point value from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadHalf(ReadOnlySpan<byte> source, out Half value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < HalfSize)
        {
            value = default;
            return false;
        }

        value = ReadHalf(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a half-precision floating point value at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteHalf(Span<byte> destination, Half value, Endianness endianness = Endianness.Little)
    {
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteHalfLittleEndian(destination, value);
        else
            BinaryPrimitives.WriteHalfBigEndian(destination, value);
    }

    /// <summary>
    /// Try to write a half-precision floating point value at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteHalf(Span<byte> destination, Half value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < HalfSize)
            return false;

        WriteHalf(destination, value, endianness);
        return true;
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a 128-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static Int128 ReadInt128(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadInt128LittleEndian(source)
            : BinaryPrimitives.ReadInt128BigEndian(source);
    }

    /// <summary>
    /// Try to read a 128-bit signed integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadInt128(ReadOnlySpan<byte> source, out Int128 value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < Int128Size)
        {
            value = default;
            return false;
        }

        value = ReadInt128(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a 128-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteInt128(Span<byte> destination, Int128 value, Endianness endianness = Endianness.Little)
    {
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteInt128LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteInt128BigEndian(destination, value);
    }

    /// <summary>
    /// Try to write a 128-bit signed integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteInt128(Span<byte> destination, Int128 value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < Int128Size)
            return false;

        WriteInt128(destination, value, endianness);
        return true;
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a 128-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is too small.</exception>
    public static UInt128 ReadUInt128(ReadOnlySpan<byte> source, Endianness endianness = Endianness.Little)
    {
        return endianness == Endianness.Little
            ? BinaryPrimitives.ReadUInt128LittleEndian(source)
            : BinaryPrimitives.ReadUInt128BigEndian(source);
    }

    /// <summary>
    /// Try to read a 128-bit unsigned integer from the start of <paramref name="source"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="source"/> is too small</returns>
    public static bool TryReadUInt128(ReadOnlySpan<byte> source, out UInt128 value, Endianness endianness = Endianness.Little)
    {
        if (source.Length < Int128Size)
        {
            value = default;
            return false;
        }

        value = ReadUInt128(source, endianness);
        return true;
    }

    /// <summary>
    /// Write a 128-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static void WriteUInt128(Span<byte> destination, UInt128 value, Endianness endianness = Endianness.Little)
    {
        if (endianness == Endianness.Little)
            BinaryPrimitives.WriteUInt128LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteUInt128BigEndian(destination, value);
    }

    /// <summary>
    /// Try to write a 128-bit unsigned integer at the start of <paramref name="destination"/>
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteUInt128(Span<byte> destination, UInt128 value, Endianness endianness = Endianness.Little)
    {
        if (destination.Length < Int128Size)
            return false;

        WriteUInt128(destination, value, endianness);
        return true;
    }

#endif

    internal static void ThrowTooSmall(string paramName)
    {
        throw new ArgumentOutOfRangeException(paramName, "The buffer is too small to contain the value.");
    }

#if !NET8_0_OR_GREATER
    private static void SwapGuidByteOrder(byte[] bytes)
    {
        // Guid.ToByteArray stores the three first fields in little endian
        Array.Reverse(bytes, 0, 4);
        Array.Reverse(bytes, 4, 2);
        Array.Reverse(bytes, 6, 2);
    }
#endif
}
