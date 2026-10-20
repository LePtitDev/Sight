using System;
using System.Buffers.Binary;
using System.IO;

namespace Sight.Encoding;

/// <summary>
/// Helpers for binary operations
/// </summary>
[Obsolete("Use BinaryEncoding (spans), SpanReader/SpanWriter, or the Stream/BinaryReader/BinaryWriter extension methods with Endianness instead.")]
public static class BinaryUtility
{
    #region ReverseEndianness

    /// <summary>
    /// Reverse big/little bytes
    /// </summary>
    public static byte ReverseEndianness(byte value)
    {
        return value;
    }

    /// <summary>
    /// Reverse big/little bytes
    /// </summary>
    public static ushort ReverseEndianness(ushort value)
    {
        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    /// Reverse big/little bytes
    /// </summary>
    public static uint ReverseEndianness(uint value)
    {
        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    /// Reverse big/little bytes
    /// </summary>
    public static ulong ReverseEndianness(ulong value)
    {
        return BinaryPrimitives.ReverseEndianness(value);
    }

    #endregion

    #region WriteToBuffer

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteInt8ToBuffer(sbyte value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteInt8(buffer.AsSpan(offset), value);
    }

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteUInt8ToBuffer(byte value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteUInt8(buffer.AsSpan(offset), value);
    }

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteInt16ToBuffer(short value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteInt16(buffer.AsSpan(offset), value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteUInt16ToBuffer(ushort value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteUInt16(buffer.AsSpan(offset), value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteInt32ToBuffer(int value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteInt32(buffer.AsSpan(offset), value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteUInt32ToBuffer(uint value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteUInt32(buffer.AsSpan(offset), value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteInt64ToBuffer(long value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteInt64(buffer.AsSpan(offset), value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteUInt64ToBuffer(ulong value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteUInt64(buffer.AsSpan(offset), value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteSingleToBuffer(float value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteSingle(buffer.AsSpan(offset), value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a buffer
    /// </summary>
    public static void WriteDoubleToBuffer(double value, byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        BinaryEncoding.WriteDouble(buffer.AsSpan(offset), value, ToEndianness(littleEndian));
    }

    #endregion

    #region WriteToStream

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteInt8ToStream(sbyte value, Stream stream, bool littleEndian = true)
    {
        stream.WriteInt8(value);
    }

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteUInt8ToStream(byte value, Stream stream, bool littleEndian = true)
    {
        stream.WriteUInt8(value);
    }

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteInt16ToStream(short value, Stream stream, bool littleEndian = true)
    {
        stream.WriteInt16(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteUInt16ToStream(ushort value, Stream stream, bool littleEndian = true)
    {
        stream.WriteUInt16(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteInt32ToStream(int value, Stream stream, bool littleEndian = true)
    {
        stream.WriteInt32(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteUInt32ToStream(uint value, Stream stream, bool littleEndian = true)
    {
        stream.WriteUInt32(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteInt64ToStream(long value, Stream stream, bool littleEndian = true)
    {
        stream.WriteInt64(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteUInt64ToStream(ulong value, Stream stream, bool littleEndian = true)
    {
        stream.WriteUInt64(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteSingleToStream(float value, Stream stream, bool littleEndian = true)
    {
        stream.WriteSingle(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a stream
    /// </summary>
    public static void WriteDoubleToStream(double value, Stream stream, bool littleEndian = true)
    {
        stream.WriteDouble(value, ToEndianness(littleEndian));
    }

    #endregion

    #region WriteToBinaryWriter

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteInt8ToBinaryWriter(sbyte value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.Write(value);
    }

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteUInt8ToBinaryWriter(byte value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.Write(value);
    }

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteInt16ToBinaryWriter(short value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.WriteInt16(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteUInt16ToBinaryWriter(ushort value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.WriteUInt16(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteInt32ToBinaryWriter(int value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.WriteInt32(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteUInt32ToBinaryWriter(uint value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.WriteUInt32(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteInt64ToBinaryWriter(long value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.WriteInt64(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteUInt64ToBinaryWriter(ulong value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.WriteUInt64(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteSingleToBinaryWriter(float value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.WriteSingle(value, ToEndianness(littleEndian));
    }

    /// <summary>
    /// Write bytes into a binary writer
    /// </summary>
    public static void WriteDoubleToBinaryWriter(double value, BinaryWriter writer, bool littleEndian = true)
    {
        writer.WriteDouble(value, ToEndianness(littleEndian));
    }

    #endregion

    #region ReadFromBuffer

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static sbyte ReadInt8FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadInt8(buffer.AsSpan(offset));
    }

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static byte ReadUInt8FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadUInt8(buffer.AsSpan(offset));
    }

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static short ReadInt16FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadInt16(buffer.AsSpan(offset), ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static ushort ReadUInt16FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadUInt16(buffer.AsSpan(offset), ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static int ReadInt32FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadInt32(buffer.AsSpan(offset), ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static uint ReadUInt32FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadUInt32(buffer.AsSpan(offset), ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static long ReadInt64FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadInt64(buffer.AsSpan(offset), ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static ulong ReadUInt64FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadUInt64(buffer.AsSpan(offset), ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static float ReadSingleFromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadSingle(buffer.AsSpan(offset), ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a buffer
    /// </summary>
    public static double ReadDoubleFromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
    {
        return BinaryEncoding.ReadDouble(buffer.AsSpan(offset), ToEndianness(littleEndian));
    }

    #endregion

    #region ReadFromStream

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static sbyte ReadInt8FromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadInt8();
    }

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static byte ReadUInt8FromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadUInt8();
    }

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static short ReadInt16FromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadInt16(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static ushort ReadUInt16FromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadUInt16(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static int ReadInt32FromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadInt32(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static uint ReadUInt32FromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadUInt32(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static long ReadInt64FromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadInt64(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static ulong ReadUInt64FromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadUInt64(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static float ReadSingleFromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadSingle(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a stream
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static double ReadDoubleFromStream(Stream stream, bool littleEndian = true)
    {
        return stream.ReadDouble(ToEndianness(littleEndian));
    }

    #endregion

    #region ReadFromBinaryReader

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static sbyte ReadInt8FromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadSByte();
    }

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static byte ReadUInt8FromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadByte();
    }

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static short ReadInt16FromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadInt16(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static ushort ReadUInt16FromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadUInt16(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static int ReadInt32FromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadInt32(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static uint ReadUInt32FromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadUInt32(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static long ReadInt64FromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadInt64(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static ulong ReadUInt64FromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadUInt64(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static float ReadSingleFromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadSingle(ToEndianness(littleEndian));
    }

    /// <summary>
    /// Read bytes from a binary reader
    /// </summary>
    public static double ReadDoubleFromBinaryReader(BinaryReader reader, bool littleEndian = true)
    {
        return reader.ReadDouble(ToEndianness(littleEndian));
    }

    #endregion

    private static Endianness ToEndianness(bool littleEndian) => littleEndian ? Endianness.Little : Endianness.Big;
}
