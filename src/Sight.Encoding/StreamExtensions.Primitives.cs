using System;
using System.IO;
using Sight.Encoding.Internal;

namespace Sight.Encoding;

public static partial class StreamExtensions
{
    /// <summary>
    /// Read an 8-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static sbyte ReadInt8(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadInt8(buffer);
    }

    /// <summary>
    /// Write an 8-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    public static void WriteInt8(this Stream stream, sbyte value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
        BinaryEncoding.WriteInt8(buffer, value);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read an 8-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static byte ReadUInt8(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(byte)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadUInt8(buffer);
    }

    /// <summary>
    /// Write an 8-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    public static void WriteUInt8(this Stream stream, byte value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(byte)];
        BinaryEncoding.WriteUInt8(buffer, value);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a boolean (1 byte, any non-zero value is true) from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static bool ReadBoolean(this Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(bool)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadBoolean(buffer);
    }

    /// <summary>
    /// Write a boolean (1 byte, any non-zero value is true) into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    public static void WriteBoolean(this Stream stream, bool value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(bool)];
        BinaryEncoding.WriteBoolean(buffer, value);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a 16-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static short ReadInt16(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadInt16(buffer, endianness);
    }

    /// <summary>
    /// Write a 16-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteInt16(this Stream stream, short value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryEncoding.WriteInt16(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a 16-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static ushort ReadUInt16(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadUInt16(buffer, endianness);
    }

    /// <summary>
    /// Write a 16-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteUInt16(this Stream stream, ushort value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        BinaryEncoding.WriteUInt16(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a 32-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static int ReadInt32(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadInt32(buffer, endianness);
    }

    /// <summary>
    /// Write a 32-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteInt32(this Stream stream, int value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryEncoding.WriteInt32(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a 32-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static uint ReadUInt32(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadUInt32(buffer, endianness);
    }

    /// <summary>
    /// Write a 32-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteUInt32(this Stream stream, uint value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryEncoding.WriteUInt32(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a 64-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static long ReadInt64(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadInt64(buffer, endianness);
    }

    /// <summary>
    /// Write a 64-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteInt64(this Stream stream, long value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryEncoding.WriteInt64(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a 64-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static ulong ReadUInt64(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadUInt64(buffer, endianness);
    }

    /// <summary>
    /// Write a 64-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteUInt64(this Stream stream, ulong value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryEncoding.WriteUInt64(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a single-precision floating point value from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static float ReadSingle(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadSingle(buffer, endianness);
    }

    /// <summary>
    /// Write a single-precision floating point value into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteSingle(this Stream stream, float value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        BinaryEncoding.WriteSingle(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a double-precision floating point value from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static double ReadDouble(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadDouble(buffer, endianness);
    }

    /// <summary>
    /// Write a double-precision floating point value into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteDouble(this Stream stream, double value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        BinaryEncoding.WriteDouble(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a UTF-16 code unit (2 bytes) from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static char ReadChar(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(char)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadChar(buffer, endianness);
    }

    /// <summary>
    /// Write a UTF-16 code unit (2 bytes) into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteChar(this Stream stream, char value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(char)];
        BinaryEncoding.WriteChar(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static decimal ReadDecimal(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(decimal)];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadDecimal(buffer, endianness);
    }

    /// <summary>
    /// Write a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteDecimal(this Stream stream, decimal value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[sizeof(decimal)];
        BinaryEncoding.WriteDecimal(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

    /// <summary>
    /// Read a GUID (16 bytes, big endian follows RFC 4122) from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static Guid ReadGuid(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[BinaryEncoding.GuidSize];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadGuid(buffer, endianness);
    }

    /// <summary>
    /// Write a GUID (16 bytes, big endian follows RFC 4122) into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteGuid(this Stream stream, Guid value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[BinaryEncoding.GuidSize];
        BinaryEncoding.WriteGuid(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a half-precision floating point value from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static Half ReadHalf(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[BinaryEncoding.HalfSize];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadHalf(buffer, endianness);
    }

    /// <summary>
    /// Write a half-precision floating point value into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteHalf(this Stream stream, Half value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[BinaryEncoding.HalfSize];
        BinaryEncoding.WriteHalf(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a 128-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static Int128 ReadInt128(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[BinaryEncoding.Int128Size];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadInt128(buffer, endianness);
    }

    /// <summary>
    /// Write a 128-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteInt128(this Stream stream, Int128 value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[BinaryEncoding.Int128Size];
        BinaryEncoding.WriteInt128(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a 128-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static UInt128 ReadUInt128(this Stream stream, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[BinaryEncoding.Int128Size];
        StreamHelper.ReadExactly(stream, buffer);
        return BinaryEncoding.ReadUInt128(buffer, endianness);
    }

    /// <summary>
    /// Write a 128-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    public static void WriteUInt128(this Stream stream, UInt128 value, Endianness endianness = Endianness.Little)
    {
        Span<byte> buffer = stackalloc byte[BinaryEncoding.Int128Size];
        BinaryEncoding.WriteUInt128(buffer, value, endianness);
        StreamHelper.Write(stream, buffer);
    }

#endif
}
