using System;

namespace Sight.Encoding;

public ref partial struct SpanReader
{
    /// <summary>
    /// Read an 8-bit signed integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public sbyte ReadInt8()
    {
        var value = BinaryEncoding.ReadInt8(RemainingSpan);
        _position += sizeof(sbyte);
        return value;
    }

    /// <summary>
    /// Try to read an 8-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadInt8(out sbyte value)
    {
        if (!BinaryEncoding.TryReadInt8(RemainingSpan, out value))
            return false;

        _position += sizeof(sbyte);
        return true;
    }

    /// <summary>
    /// Read an 8-bit unsigned integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public byte ReadUInt8()
    {
        var value = BinaryEncoding.ReadUInt8(RemainingSpan);
        _position += sizeof(byte);
        return value;
    }

    /// <summary>
    /// Try to read an 8-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadUInt8(out byte value)
    {
        if (!BinaryEncoding.TryReadUInt8(RemainingSpan, out value))
            return false;

        _position += sizeof(byte);
        return true;
    }

    /// <summary>
    /// Read a boolean (1 byte, any non-zero value is true) and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public bool ReadBoolean()
    {
        var value = BinaryEncoding.ReadBoolean(RemainingSpan);
        _position += sizeof(bool);
        return value;
    }

    /// <summary>
    /// Try to read a boolean (1 byte, any non-zero value is true) and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadBoolean(out bool value)
    {
        if (!BinaryEncoding.TryReadBoolean(RemainingSpan, out value))
            return false;

        _position += sizeof(bool);
        return true;
    }

    /// <summary>
    /// Read a 16-bit signed integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public short ReadInt16()
    {
        var value = BinaryEncoding.ReadInt16(RemainingSpan, Endianness);
        _position += sizeof(short);
        return value;
    }

    /// <summary>
    /// Try to read a 16-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadInt16(out short value)
    {
        if (!BinaryEncoding.TryReadInt16(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(short);
        return true;
    }

    /// <summary>
    /// Read a 16-bit unsigned integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public ushort ReadUInt16()
    {
        var value = BinaryEncoding.ReadUInt16(RemainingSpan, Endianness);
        _position += sizeof(ushort);
        return value;
    }

    /// <summary>
    /// Try to read a 16-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadUInt16(out ushort value)
    {
        if (!BinaryEncoding.TryReadUInt16(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(ushort);
        return true;
    }

    /// <summary>
    /// Read a 32-bit signed integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public int ReadInt32()
    {
        var value = BinaryEncoding.ReadInt32(RemainingSpan, Endianness);
        _position += sizeof(int);
        return value;
    }

    /// <summary>
    /// Try to read a 32-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadInt32(out int value)
    {
        if (!BinaryEncoding.TryReadInt32(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(int);
        return true;
    }

    /// <summary>
    /// Read a 32-bit unsigned integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public uint ReadUInt32()
    {
        var value = BinaryEncoding.ReadUInt32(RemainingSpan, Endianness);
        _position += sizeof(uint);
        return value;
    }

    /// <summary>
    /// Try to read a 32-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadUInt32(out uint value)
    {
        if (!BinaryEncoding.TryReadUInt32(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(uint);
        return true;
    }

    /// <summary>
    /// Read a 64-bit signed integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public long ReadInt64()
    {
        var value = BinaryEncoding.ReadInt64(RemainingSpan, Endianness);
        _position += sizeof(long);
        return value;
    }

    /// <summary>
    /// Try to read a 64-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadInt64(out long value)
    {
        if (!BinaryEncoding.TryReadInt64(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(long);
        return true;
    }

    /// <summary>
    /// Read a 64-bit unsigned integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public ulong ReadUInt64()
    {
        var value = BinaryEncoding.ReadUInt64(RemainingSpan, Endianness);
        _position += sizeof(ulong);
        return value;
    }

    /// <summary>
    /// Try to read a 64-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadUInt64(out ulong value)
    {
        if (!BinaryEncoding.TryReadUInt64(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(ulong);
        return true;
    }

    /// <summary>
    /// Read a single-precision floating point value and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public float ReadSingle()
    {
        var value = BinaryEncoding.ReadSingle(RemainingSpan, Endianness);
        _position += sizeof(float);
        return value;
    }

    /// <summary>
    /// Try to read a single-precision floating point value and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadSingle(out float value)
    {
        if (!BinaryEncoding.TryReadSingle(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(float);
        return true;
    }

    /// <summary>
    /// Read a double-precision floating point value and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public double ReadDouble()
    {
        var value = BinaryEncoding.ReadDouble(RemainingSpan, Endianness);
        _position += sizeof(double);
        return value;
    }

    /// <summary>
    /// Try to read a double-precision floating point value and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadDouble(out double value)
    {
        if (!BinaryEncoding.TryReadDouble(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(double);
        return true;
    }

    /// <summary>
    /// Read a UTF-16 code unit (2 bytes) and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public char ReadChar()
    {
        var value = BinaryEncoding.ReadChar(RemainingSpan, Endianness);
        _position += sizeof(char);
        return value;
    }

    /// <summary>
    /// Try to read a UTF-16 code unit (2 bytes) and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadChar(out char value)
    {
        if (!BinaryEncoding.TryReadChar(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(char);
        return true;
    }

    /// <summary>
    /// Read a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public decimal ReadDecimal()
    {
        var value = BinaryEncoding.ReadDecimal(RemainingSpan, Endianness);
        _position += sizeof(decimal);
        return value;
    }

    /// <summary>
    /// Try to read a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadDecimal(out decimal value)
    {
        if (!BinaryEncoding.TryReadDecimal(RemainingSpan, out value, Endianness))
            return false;

        _position += sizeof(decimal);
        return true;
    }

    /// <summary>
    /// Read a GUID (16 bytes, big endian follows RFC 4122) and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public Guid ReadGuid()
    {
        var value = BinaryEncoding.ReadGuid(RemainingSpan, Endianness);
        _position += BinaryEncoding.GuidSize;
        return value;
    }

    /// <summary>
    /// Try to read a GUID (16 bytes, big endian follows RFC 4122) and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadGuid(out Guid value)
    {
        if (!BinaryEncoding.TryReadGuid(RemainingSpan, out value, Endianness))
            return false;

        _position += BinaryEncoding.GuidSize;
        return true;
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a half-precision floating point value and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public Half ReadHalf()
    {
        var value = BinaryEncoding.ReadHalf(RemainingSpan, Endianness);
        _position += BinaryEncoding.HalfSize;
        return value;
    }

    /// <summary>
    /// Try to read a half-precision floating point value and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadHalf(out Half value)
    {
        if (!BinaryEncoding.TryReadHalf(RemainingSpan, out value, Endianness))
            return false;

        _position += BinaryEncoding.HalfSize;
        return true;
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a 128-bit signed integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public Int128 ReadInt128()
    {
        var value = BinaryEncoding.ReadInt128(RemainingSpan, Endianness);
        _position += BinaryEncoding.Int128Size;
        return value;
    }

    /// <summary>
    /// Try to read a 128-bit signed integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadInt128(out Int128 value)
    {
        if (!BinaryEncoding.TryReadInt128(RemainingSpan, out value, Endianness))
            return false;

        _position += BinaryEncoding.Int128Size;
        return true;
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a 128-bit unsigned integer and advance the position
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not enough remaining bytes.</exception>
    public UInt128 ReadUInt128()
    {
        var value = BinaryEncoding.ReadUInt128(RemainingSpan, Endianness);
        _position += BinaryEncoding.Int128Size;
        return value;
    }

    /// <summary>
    /// Try to read a 128-bit unsigned integer and advance the position
    /// </summary>
    /// <param name="value">Read value</param>
    /// <returns><c>false</c> if there is not enough remaining bytes, the position is unchanged</returns>
    public bool TryReadUInt128(out UInt128 value)
    {
        if (!BinaryEncoding.TryReadUInt128(RemainingSpan, out value, Endianness))
            return false;

        _position += BinaryEncoding.Int128Size;
        return true;
    }

#endif
}
