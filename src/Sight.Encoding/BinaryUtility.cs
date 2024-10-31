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
    }
}
