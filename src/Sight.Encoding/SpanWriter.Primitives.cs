using System;

namespace Sight.Encoding;

public ref partial struct SpanWriter
{
    /// <summary>
    /// Write an 8-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteInt8(sbyte value)
    {
        BinaryEncoding.WriteInt8(RemainingSpan, value);
        _position += sizeof(sbyte);
    }

    /// <summary>
    /// Try to write an 8-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteInt8(sbyte value)
    {
        if (!BinaryEncoding.TryWriteInt8(RemainingSpan, value))
            return false;

        _position += sizeof(sbyte);
        return true;
    }

    /// <summary>
    /// Write an 8-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteUInt8(byte value)
    {
        BinaryEncoding.WriteUInt8(RemainingSpan, value);
        _position += sizeof(byte);
    }

    /// <summary>
    /// Try to write an 8-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteUInt8(byte value)
    {
        if (!BinaryEncoding.TryWriteUInt8(RemainingSpan, value))
            return false;

        _position += sizeof(byte);
        return true;
    }

    /// <summary>
    /// Write a boolean (1 byte, any non-zero value is true) and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteBoolean(bool value)
    {
        BinaryEncoding.WriteBoolean(RemainingSpan, value);
        _position += sizeof(bool);
    }

    /// <summary>
    /// Try to write a boolean (1 byte, any non-zero value is true) and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteBoolean(bool value)
    {
        if (!BinaryEncoding.TryWriteBoolean(RemainingSpan, value))
            return false;

        _position += sizeof(bool);
        return true;
    }

    /// <summary>
    /// Write a 16-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteInt16(short value)
    {
        BinaryEncoding.WriteInt16(RemainingSpan, value, Endianness);
        _position += sizeof(short);
    }

    /// <summary>
    /// Try to write a 16-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteInt16(short value)
    {
        if (!BinaryEncoding.TryWriteInt16(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(short);
        return true;
    }

    /// <summary>
    /// Write a 16-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteUInt16(ushort value)
    {
        BinaryEncoding.WriteUInt16(RemainingSpan, value, Endianness);
        _position += sizeof(ushort);
    }

    /// <summary>
    /// Try to write a 16-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteUInt16(ushort value)
    {
        if (!BinaryEncoding.TryWriteUInt16(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(ushort);
        return true;
    }

    /// <summary>
    /// Write a 32-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteInt32(int value)
    {
        BinaryEncoding.WriteInt32(RemainingSpan, value, Endianness);
        _position += sizeof(int);
    }

    /// <summary>
    /// Try to write a 32-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteInt32(int value)
    {
        if (!BinaryEncoding.TryWriteInt32(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(int);
        return true;
    }

    /// <summary>
    /// Write a 32-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteUInt32(uint value)
    {
        BinaryEncoding.WriteUInt32(RemainingSpan, value, Endianness);
        _position += sizeof(uint);
    }

    /// <summary>
    /// Try to write a 32-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteUInt32(uint value)
    {
        if (!BinaryEncoding.TryWriteUInt32(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(uint);
        return true;
    }

    /// <summary>
    /// Write a 64-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteInt64(long value)
    {
        BinaryEncoding.WriteInt64(RemainingSpan, value, Endianness);
        _position += sizeof(long);
    }

    /// <summary>
    /// Try to write a 64-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteInt64(long value)
    {
        if (!BinaryEncoding.TryWriteInt64(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(long);
        return true;
    }

    /// <summary>
    /// Write a 64-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteUInt64(ulong value)
    {
        BinaryEncoding.WriteUInt64(RemainingSpan, value, Endianness);
        _position += sizeof(ulong);
    }

    /// <summary>
    /// Try to write a 64-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteUInt64(ulong value)
    {
        if (!BinaryEncoding.TryWriteUInt64(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(ulong);
        return true;
    }

    /// <summary>
    /// Write a single-precision floating point value and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteSingle(float value)
    {
        BinaryEncoding.WriteSingle(RemainingSpan, value, Endianness);
        _position += sizeof(float);
    }

    /// <summary>
    /// Try to write a single-precision floating point value and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteSingle(float value)
    {
        if (!BinaryEncoding.TryWriteSingle(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(float);
        return true;
    }

    /// <summary>
    /// Write a double-precision floating point value and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteDouble(double value)
    {
        BinaryEncoding.WriteDouble(RemainingSpan, value, Endianness);
        _position += sizeof(double);
    }

    /// <summary>
    /// Try to write a double-precision floating point value and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteDouble(double value)
    {
        if (!BinaryEncoding.TryWriteDouble(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(double);
        return true;
    }

    /// <summary>
    /// Write a UTF-16 code unit (2 bytes) and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteChar(char value)
    {
        BinaryEncoding.WriteChar(RemainingSpan, value, Endianness);
        _position += sizeof(char);
    }

    /// <summary>
    /// Try to write a UTF-16 code unit (2 bytes) and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteChar(char value)
    {
        if (!BinaryEncoding.TryWriteChar(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(char);
        return true;
    }

    /// <summary>
    /// Write a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteDecimal(decimal value)
    {
        BinaryEncoding.WriteDecimal(RemainingSpan, value, Endianness);
        _position += sizeof(decimal);
    }

    /// <summary>
    /// Try to write a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteDecimal(decimal value)
    {
        if (!BinaryEncoding.TryWriteDecimal(RemainingSpan, value, Endianness))
            return false;

        _position += sizeof(decimal);
        return true;
    }

    /// <summary>
    /// Write a GUID (16 bytes, big endian follows RFC 4122) and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteGuid(Guid value)
    {
        BinaryEncoding.WriteGuid(RemainingSpan, value, Endianness);
        _position += BinaryEncoding.GuidSize;
    }

    /// <summary>
    /// Try to write a GUID (16 bytes, big endian follows RFC 4122) and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteGuid(Guid value)
    {
        if (!BinaryEncoding.TryWriteGuid(RemainingSpan, value, Endianness))
            return false;

        _position += BinaryEncoding.GuidSize;
        return true;
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Write a half-precision floating point value and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteHalf(Half value)
    {
        BinaryEncoding.WriteHalf(RemainingSpan, value, Endianness);
        _position += BinaryEncoding.HalfSize;
    }

    /// <summary>
    /// Try to write a half-precision floating point value and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteHalf(Half value)
    {
        if (!BinaryEncoding.TryWriteHalf(RemainingSpan, value, Endianness))
            return false;

        _position += BinaryEncoding.HalfSize;
        return true;
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Write a 128-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteInt128(Int128 value)
    {
        BinaryEncoding.WriteInt128(RemainingSpan, value, Endianness);
        _position += BinaryEncoding.Int128Size;
    }

    /// <summary>
    /// Try to write a 128-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteInt128(Int128 value)
    {
        if (!BinaryEncoding.TryWriteInt128(RemainingSpan, value, Endianness))
            return false;

        _position += BinaryEncoding.Int128Size;
        return true;
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Write a 128-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes, nothing is written.</exception>
    public void WriteUInt128(UInt128 value)
    {
        BinaryEncoding.WriteUInt128(RemainingSpan, value, Endianness);
        _position += BinaryEncoding.Int128Size;
    }

    /// <summary>
    /// Try to write a 128-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Value to write</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, nothing is written</returns>
    public bool TryWriteUInt128(UInt128 value)
    {
        if (!BinaryEncoding.TryWriteUInt128(RemainingSpan, value, Endianness))
            return false;

        _position += BinaryEncoding.Int128Size;
        return true;
    }

#endif
}
