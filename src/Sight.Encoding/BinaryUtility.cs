﻿using System.IO;
using System.Runtime.CompilerServices;
using Sight.Encoding.Internal;

namespace Sight.Encoding
{
    /// <summary>
    /// Helpers for binary operations
    /// </summary>
    public static class BinaryUtility
    {
        #region ReverseEndianness

        /// <summary>
        /// Reverse big/little bytes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReverseEndianness(byte value) => value;

        /// <summary>
        /// Reverse big/little bytes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReverseEndianness(ushort value)
        {
            return (ushort)((value << 8) | (value >> 8));
        }

        /// <summary>
        /// Reverse big/little bytes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReverseEndianness(uint value)
        {
            return (value << 24) | ((value & 0x00FF00u) << 8) | ((value & 0x00FF0000) >> 8) | (value >> 24);
        }

        /// <summary>
        /// Reverse big/little bytes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReverseEndianness(ulong value)
        {
            return ReverseEndianness((uint)(value >> 32)) | (ulong)ReverseEndianness((uint)(value & 0xFFFFFFFF)) << 32;
        }

        #endregion

        #region WriteToBuffer

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt8ToBuffer(sbyte value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            WriteUInt8ToBuffer((byte)value, buffer, offset, littleEndian);
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        public static void WriteUInt8ToBuffer(byte value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            buffer[offset] = value;
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16ToBuffer(short value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            WriteUInt16ToBuffer((ushort)value, buffer, offset, littleEndian);
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        public static void WriteUInt16ToBuffer(ushort value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            if (littleEndian)
            {
                buffer[offset++] = (byte)(value & 0xFF);
                buffer[offset] = (byte)(value >> 8);
            }
            else
            {
                buffer[offset++] = (byte)(value >> 8);
                buffer[offset] = (byte)(value & 0xFF);
            }
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32ToBuffer(int value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            WriteUInt32ToBuffer((uint)value, buffer, offset, littleEndian);
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        public static void WriteUInt32ToBuffer(uint value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            if (littleEndian)
            {
                buffer[offset++] = (byte)(value & 0xFF);
                buffer[offset++] = (byte)((value >> 8) & 0xFF);
                buffer[offset++] = (byte)((value >> 16) & 0xFF);
                buffer[offset] = (byte)(value >> 24);
            }
            else
            {
                buffer[offset++] = (byte)(value >> 24);
                buffer[offset++] = (byte)((value >> 16) & 0xFF);
                buffer[offset++] = (byte)((value >> 8) & 0xFF);
                buffer[offset] = (byte)(value & 0xFF);
            }
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64ToBuffer(long value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            WriteUInt64ToBuffer((ulong)value, buffer, offset, littleEndian);
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        public static void WriteUInt64ToBuffer(ulong value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            if (littleEndian)
            {
                buffer[offset++] = (byte)(value & 0xFF);
                buffer[offset++] = (byte)((value >> 8) & 0xFF);
                buffer[offset++] = (byte)((value >> 16) & 0xFF);
                buffer[offset++] = (byte)((value >> 24) & 0xFF);
                buffer[offset++] = (byte)((value >> 32) & 0xFF);
                buffer[offset++] = (byte)((value >> 40) & 0xFF);
                buffer[offset++] = (byte)((value >> 48) & 0xFF);
                buffer[offset] = (byte)((value >> 56) & 0xFF);
            }
            else
            {
                buffer[offset++] = (byte)((value >> 56) & 0xFF);
                buffer[offset++] = (byte)((value >> 48) & 0xFF);
                buffer[offset++] = (byte)((value >> 40) & 0xFF);
                buffer[offset++] = (byte)((value >> 32) & 0xFF);
                buffer[offset++] = (byte)((value >> 24) & 0xFF);
                buffer[offset++] = (byte)((value >> 16) & 0xFF);
                buffer[offset++] = (byte)((value >> 8) & 0xFF);
                buffer[offset] = (byte)(value & 0xFF);
            }
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleToBuffer(float value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            var union = new SingleToUInt32Union { Single = value };
            WriteUInt32ToBuffer(union.UInt32, buffer, offset, littleEndian);
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleToBuffer(double value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            var union = new DoubleToUInt64Union { Double = value };
            WriteUInt64ToBuffer(union.UInt64, buffer, offset, littleEndian);
        }

        #endregion

        #region WriteToStream

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt8ToStream(sbyte value, Stream stream, bool littleEndian = true)
        {
            WriteUInt8ToStream((byte)value, stream, littleEndian);
        }

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        public static void WriteUInt8ToStream(byte value, Stream stream, bool littleEndian = true)
        {
            stream.WriteByte(value);
        }

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16ToStream(short value, Stream stream, bool littleEndian = true)
        {
            WriteUInt16ToStream((ushort)value, stream, littleEndian);
        }

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        public static void WriteUInt16ToStream(ushort value, Stream stream, bool littleEndian = true)
        {
            if (littleEndian)
            {
                stream.WriteByte((byte)(value & 0xFF));
                stream.WriteByte((byte)(value >> 8));
            }
            else
            {
                stream.WriteByte((byte)(value >> 8));
                stream.WriteByte((byte)(value & 0xFF));
            }
        }

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32ToStream(int value, Stream stream, bool littleEndian = true)
        {
            WriteUInt32ToStream((uint)value, stream, littleEndian);
        }

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        public static void WriteUInt32ToStream(uint value, Stream stream, bool littleEndian = true)
        {
            if (littleEndian)
            {
                stream.WriteByte((byte)(value & 0xFF));
                stream.WriteByte((byte)((value >> 8) & 0xFF));
                stream.WriteByte((byte)((value >> 16) & 0xFF));
                stream.WriteByte((byte)(value >> 24));
            }
            else
            {
                stream.WriteByte((byte)(value >> 24));
                stream.WriteByte((byte)((value >> 16) & 0xFF));
                stream.WriteByte((byte)((value >> 8) & 0xFF));
                stream.WriteByte((byte)(value & 0xFF));
            }
        }

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64ToStream(long value, Stream stream, bool littleEndian = true)
        {
            WriteUInt64ToStream((ulong)value, stream, littleEndian);
        }

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        public static void WriteUInt64ToStream(ulong value, Stream stream, bool littleEndian = true)
        {
            if (littleEndian)
            {
                stream.WriteByte((byte)(value & 0xFF));
                stream.WriteByte((byte)((value >> 8) & 0xFF));
                stream.WriteByte((byte)((value >> 16) & 0xFF));
                stream.WriteByte((byte)((value >> 24) & 0xFF));
                stream.WriteByte((byte)((value >> 32) & 0xFF));
                stream.WriteByte((byte)((value >> 40) & 0xFF));
                stream.WriteByte((byte)((value >> 48) & 0xFF));
                stream.WriteByte((byte)((value >> 56) & 0xFF));
            }
            else
            {
                stream.WriteByte((byte)((value >> 56) & 0xFF));
                stream.WriteByte((byte)((value >> 48) & 0xFF));
                stream.WriteByte((byte)((value >> 40) & 0xFF));
                stream.WriteByte((byte)((value >> 32) & 0xFF));
                stream.WriteByte((byte)((value >> 24) & 0xFF));
                stream.WriteByte((byte)((value >> 16) & 0xFF));
                stream.WriteByte((byte)((value >> 8) & 0xFF));
                stream.WriteByte((byte)(value & 0xFF));
            }
        }

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleToStream(float value, Stream stream, bool littleEndian = true)
        {
            var union = new SingleToUInt32Union { Single = value };
            WriteUInt32ToStream(union.UInt32, stream, littleEndian);
        }

        /// <summary>
        /// Write bytes into a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleToStream(double value, Stream stream, bool littleEndian = true)
        {
            var union = new DoubleToUInt64Union { Double = value };
            WriteUInt64ToStream(union.UInt64, stream, littleEndian);
        }

        #endregion

        #region WriteToBinaryWriter

        /// <summary>
        /// Write bytes into a binary writer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt8ToBinaryWriter(sbyte value, BinaryWriter writer, bool littleEndian = true)
        {
            WriteUInt8ToBinaryWriter((byte)value, writer, littleEndian);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16ToBinaryWriter(short value, BinaryWriter writer, bool littleEndian = true)
        {
            WriteUInt16ToBinaryWriter((ushort)value, writer, littleEndian);
        }

        /// <summary>
        /// Write bytes into a binary writer
        /// </summary>
        public static void WriteUInt16ToBinaryWriter(ushort value, BinaryWriter writer, bool littleEndian = true)
        {
            if (littleEndian)
            {
                writer.Write((byte)(value & 0xFF));
                writer.Write((byte)(value >> 8));
            }
            else
            {
                writer.Write((byte)(value >> 8));
                writer.Write((byte)(value & 0xFF));
            }
        }

        /// <summary>
        /// Write bytes into a binary writer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32ToBinaryWriter(int value, BinaryWriter writer, bool littleEndian = true)
        {
            WriteUInt32ToBinaryWriter((uint)value, writer, littleEndian);
        }

        /// <summary>
        /// Write bytes into a binary writer
        /// </summary>
        public static void WriteUInt32ToBinaryWriter(uint value, BinaryWriter writer, bool littleEndian = true)
        {
            if (littleEndian)
            {
                writer.Write((byte)(value & 0xFF));
                writer.Write((byte)((value >> 8) & 0xFF));
                writer.Write((byte)((value >> 16) & 0xFF));
                writer.Write((byte)(value >> 24));
            }
            else
            {
                writer.Write((byte)(value >> 24));
                writer.Write((byte)((value >> 16) & 0xFF));
                writer.Write((byte)((value >> 8) & 0xFF));
                writer.Write((byte)(value & 0xFF));
            }
        }

        /// <summary>
        /// Write bytes into a binary writer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64ToBinaryWriter(long value, BinaryWriter writer, bool littleEndian = true)
        {
            WriteUInt64ToBinaryWriter((ulong)value, writer, littleEndian);
        }

        /// <summary>
        /// Write bytes into a binary writer
        /// </summary>
        public static void WriteUInt64ToBinaryWriter(ulong value, BinaryWriter writer, bool littleEndian = true)
        {
            if (littleEndian)
            {
                writer.Write((byte)(value & 0xFF));
                writer.Write((byte)((value >> 8) & 0xFF));
                writer.Write((byte)((value >> 16) & 0xFF));
                writer.Write((byte)((value >> 24) & 0xFF));
                writer.Write((byte)((value >> 32) & 0xFF));
                writer.Write((byte)((value >> 40) & 0xFF));
                writer.Write((byte)((value >> 48) & 0xFF));
                writer.Write((byte)((value >> 56) & 0xFF));
            }
            else
            {
                writer.Write((byte)((value >> 56) & 0xFF));
                writer.Write((byte)((value >> 48) & 0xFF));
                writer.Write((byte)((value >> 40) & 0xFF));
                writer.Write((byte)((value >> 32) & 0xFF));
                writer.Write((byte)((value >> 24) & 0xFF));
                writer.Write((byte)((value >> 16) & 0xFF));
                writer.Write((byte)((value >> 8) & 0xFF));
                writer.Write((byte)(value & 0xFF));
            }
        }

        /// <summary>
        /// Write bytes into a binary writer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingleToBinaryWriter(float value, BinaryWriter writer, bool littleEndian = true)
        {
            var union = new SingleToUInt32Union { Single = value };
            WriteUInt32ToBinaryWriter(union.UInt32, writer, littleEndian);
        }

        /// <summary>
        /// Write bytes into a binary writer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDoubleToBinaryWriter(double value, BinaryWriter writer, bool littleEndian = true)
        {
            var union = new DoubleToUInt64Union { Double = value };
            WriteUInt64ToBinaryWriter(union.UInt64, writer, littleEndian);
        }

        #endregion

        #region ReadFromBuffer

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadInt8FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            return (sbyte)ReadUInt8FromBuffer(buffer, offset, littleEndian);
        }

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        public static byte ReadUInt8FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            return buffer[offset];
        }

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            return (short)ReadUInt16FromBuffer(buffer, offset, littleEndian);
        }

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        public static ushort ReadUInt16FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            return littleEndian
                ? (ushort)(buffer[offset++] | ((uint)buffer[offset] << 8))
                : (ushort)(((uint)buffer[offset++] << 8) | buffer[offset]);
        }

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            return (int)ReadUInt32FromBuffer(buffer, offset, littleEndian);
        }

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        public static uint ReadUInt32FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            return littleEndian
                ? buffer[offset++] | ((uint)buffer[offset++] << 8) | ((uint)buffer[offset++] << 16) | ((uint)buffer[offset] << 24)
                : ((uint)buffer[offset++] << 24) | ((uint)buffer[offset++] << 16) | ((uint)buffer[offset++] << 8) | buffer[offset];
        }

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            return (long)ReadUInt64FromBuffer(buffer, offset, littleEndian);
        }

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        public static ulong ReadUInt64FromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            return littleEndian
                ? buffer[offset++] | ((ulong)buffer[offset++] << 8) | ((ulong)buffer[offset++] << 16) | ((ulong)buffer[offset++] << 24) |
                  ((ulong)buffer[offset++] << 32) | ((ulong)buffer[offset++] << 40) | ((ulong)buffer[offset++] << 48) | ((ulong)buffer[offset] << 56)
                : ((ulong)buffer[offset++] << 56) | ((ulong)buffer[offset++] << 48) | ((ulong)buffer[offset++] << 40) | ((ulong)buffer[offset++] << 32) |
                  ((ulong)buffer[offset++] << 24) | ((ulong)buffer[offset++] << 16) | ((ulong)buffer[offset++] << 8) | buffer[offset];
        }

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingleFromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            var union = new SingleToUInt32Union { UInt32 = ReadUInt32FromBuffer(buffer, offset, littleEndian) };
            return union.Single;
        }

        /// <summary>
        /// Read bytes from a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDoubleFromBuffer(byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            var union = new DoubleToUInt64Union { UInt64 = ReadUInt64FromBuffer(buffer, offset, littleEndian) };
            return union.Double;
        }

        #endregion

        #region ReadFromStream

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadInt8FromStream(Stream stream, bool littleEndian = true)
        {
            return (sbyte)ReadUInt8FromStream(stream, littleEndian);
        }

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        public static byte ReadUInt8FromStream(Stream stream, bool littleEndian = true)
        {
            return (byte)stream.ReadByte();
        }

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16FromStream(Stream stream, bool littleEndian = true)
        {
            return (short)ReadUInt16FromStream(stream, littleEndian);
        }

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        public static ushort ReadUInt16FromStream(Stream stream, bool littleEndian = true)
        {
            return littleEndian
                ? (ushort)((uint)stream.ReadByte() | ((uint)stream.ReadByte() << 8))
                : (ushort)(((uint)stream.ReadByte() << 8) | (uint)stream.ReadByte());
        }

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32FromStream(Stream stream, bool littleEndian = true)
        {
            return (int)ReadUInt32FromStream(stream, littleEndian);
        }

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        public static uint ReadUInt32FromStream(Stream stream, bool littleEndian = true)
        {
            return littleEndian
                ? (uint)stream.ReadByte() | ((uint)stream.ReadByte() << 8) | ((uint)stream.ReadByte() << 16) | ((uint)stream.ReadByte() << 24)
                : ((uint)stream.ReadByte() << 24) | ((uint)stream.ReadByte() << 16) | ((uint)stream.ReadByte() << 8) | (uint)stream.ReadByte();
        }

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64FromStream(Stream stream, bool littleEndian = true)
        {
            return (long)ReadUInt64FromStream(stream, littleEndian);
        }

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        public static ulong ReadUInt64FromStream(Stream stream, bool littleEndian = true)
        {
            return littleEndian
                ? (uint)stream.ReadByte() | ((ulong)stream.ReadByte() << 8) | ((ulong)stream.ReadByte() << 16) | ((ulong)stream.ReadByte() << 24) |
                  ((ulong)stream.ReadByte() << 32) | ((ulong)stream.ReadByte() << 40) | ((ulong)stream.ReadByte() << 48) | ((ulong)stream.ReadByte() << 56)
                : ((ulong)stream.ReadByte() << 56) | ((ulong)stream.ReadByte() << 48) | ((ulong)stream.ReadByte() << 40) | ((ulong)stream.ReadByte() << 32) |
                  ((ulong)stream.ReadByte() << 24) | ((ulong)stream.ReadByte() << 16) | ((ulong)stream.ReadByte() << 8) | (uint)stream.ReadByte();
        }

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingleFromStream(Stream stream, bool littleEndian = true)
        {
            var union = new SingleToUInt32Union { UInt32 = ReadUInt32FromStream(stream, littleEndian) };
            return union.Single;
        }

        /// <summary>
        /// Read bytes from a stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDoubleFromStream(Stream stream, bool littleEndian = true)
        {
            var union = new DoubleToUInt64Union { UInt64 = ReadUInt64FromStream(stream, littleEndian) };
            return union.Double;
        }

        #endregion

        #region ReadFromBinaryReader

        /// <summary>
        /// Read bytes from a binary reader
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadInt8FromBinaryReader(BinaryReader reader, bool littleEndian = true)
        {
            return (sbyte)ReadUInt8FromBinaryReader(reader, littleEndian);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16FromBinaryReader(BinaryReader reader, bool littleEndian = true)
        {
            return (short)ReadUInt16FromBinaryReader(reader, littleEndian);
        }

        /// <summary>
        /// Read bytes from a binary reader
        /// </summary>
        public static ushort ReadUInt16FromBinaryReader(BinaryReader reader, bool littleEndian = true)
        {
            return littleEndian
                ? (ushort)(reader.ReadByte() | ((uint)reader.ReadByte() << 8))
                : (ushort)(((uint)reader.ReadByte() << 8) | reader.ReadByte());
        }

        /// <summary>
        /// Read bytes from a binary reader
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32FromBinaryReader(BinaryReader reader, bool littleEndian = true)
        {
            return (int)ReadUInt32FromBinaryReader(reader, littleEndian);
        }

        /// <summary>
        /// Read bytes from a binary reader
        /// </summary>
        public static uint ReadUInt32FromBinaryReader(BinaryReader reader, bool littleEndian = true)
        {
            return littleEndian
                ? reader.ReadByte() | ((uint)reader.ReadByte() << 8) | ((uint)reader.ReadByte() << 16) | ((uint)reader.ReadByte() << 24)
                : ((uint)reader.ReadByte() << 24) | ((uint)reader.ReadByte() << 16) | ((uint)reader.ReadByte() << 8) | reader.ReadByte();
        }

        /// <summary>
        /// Read bytes from a binary reader
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64FromBinaryReader(BinaryReader reader, bool littleEndian = true)
        {
            return (long)ReadUInt64FromBinaryReader(reader, littleEndian);
        }

        /// <summary>
        /// Read bytes from a binary reader
        /// </summary>
        public static ulong ReadUInt64FromBinaryReader(BinaryReader reader, bool littleEndian = true)
        {
            return littleEndian
                ? reader.ReadByte() | ((ulong)reader.ReadByte() << 8) | ((ulong)reader.ReadByte() << 16) | ((ulong)reader.ReadByte() << 24) |
                  ((ulong)reader.ReadByte() << 32) | ((ulong)reader.ReadByte() << 40) | ((ulong)reader.ReadByte() << 48) | ((ulong)reader.ReadByte() << 56)
                : ((ulong)reader.ReadByte() << 56) | ((ulong)reader.ReadByte() << 48) | ((ulong)reader.ReadByte() << 40) | ((ulong)reader.ReadByte() << 32) |
                  ((ulong)reader.ReadByte() << 24) | ((ulong)reader.ReadByte() << 16) | ((ulong)reader.ReadByte() << 8) | reader.ReadByte();
        }

        /// <summary>
        /// Read bytes from a binary reader
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingleFromBinaryReader(BinaryReader reader, bool littleEndian = true)
        {
            var union = new SingleToUInt32Union { UInt32 = ReadUInt32FromBinaryReader(reader, littleEndian) };
            return union.Single;
        }

        /// <summary>
        /// Read bytes from a binary reader
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDoubleFromBinaryReader(BinaryReader reader, bool littleEndian = true)
        {
            var union = new DoubleToUInt64Union { UInt64 = ReadUInt64FromBinaryReader(reader, littleEndian) };
            return union.Double;
        }

        #endregion
    }
}
