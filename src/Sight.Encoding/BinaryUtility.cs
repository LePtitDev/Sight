using System.Runtime.CompilerServices;

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
        public static void WriteByteToBuffer(byte value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            buffer[offset] = value;
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShortToBuffer(short value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            WriteUShortToBuffer((ushort)value, buffer, offset, littleEndian);
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        public static void WriteUShortToBuffer(ushort value, byte[] buffer, int offset = 0, bool littleEndian = true)
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
        public static void WriteIntToBuffer(int value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            WriteUIntToBuffer((uint)value, buffer, offset, littleEndian);
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        public static void WriteUIntToBuffer(uint value, byte[] buffer, int offset = 0, bool littleEndian = true)
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
        public static void WriteLongToBuffer(long value, byte[] buffer, int offset = 0, bool littleEndian = true)
        {
            WriteULongToBuffer((ulong)value, buffer, offset, littleEndian);
        }

        /// <summary>
        /// Write bytes into a buffer
        /// </summary>
        public static void WriteULongToBuffer(ulong value, byte[] buffer, int offset = 0, bool littleEndian = true)
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

        #endregion
    }
}
