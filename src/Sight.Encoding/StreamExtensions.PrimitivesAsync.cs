using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sight.Encoding.Internal;

namespace Sight.Encoding;

public static partial class StreamExtensions
{
    /// <summary>
    /// Read an 8-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<sbyte> ReadInt8Async(this Stream stream, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(sbyte));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(sbyte), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadInt8(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write an 8-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteInt8Async(this Stream stream, sbyte value, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(sbyte));
        try
        {
            BinaryEncoding.WriteInt8(buffer, value);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(sbyte), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read an 8-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<byte> ReadUInt8Async(this Stream stream, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(byte));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(byte), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadUInt8(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write an 8-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteUInt8Async(this Stream stream, byte value, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(byte));
        try
        {
            BinaryEncoding.WriteUInt8(buffer, value);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(byte), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a boolean (1 byte, any non-zero value is true) from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<bool> ReadBooleanAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(bool));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(bool), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadBoolean(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a boolean (1 byte, any non-zero value is true) into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteBooleanAsync(this Stream stream, bool value, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(bool));
        try
        {
            BinaryEncoding.WriteBoolean(buffer, value);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(bool), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a 16-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<short> ReadInt16Async(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(short));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(short), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadInt16(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a 16-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteInt16Async(this Stream stream, short value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(short));
        try
        {
            BinaryEncoding.WriteInt16(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(short), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a 16-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<ushort> ReadUInt16Async(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(ushort));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(ushort), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadUInt16(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a 16-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteUInt16Async(this Stream stream, ushort value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(ushort));
        try
        {
            BinaryEncoding.WriteUInt16(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(ushort), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a 32-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<int> ReadInt32Async(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(int));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(int), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadInt32(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a 32-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteInt32Async(this Stream stream, int value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(int));
        try
        {
            BinaryEncoding.WriteInt32(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(int), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a 32-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<uint> ReadUInt32Async(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(uint));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(uint), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadUInt32(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a 32-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteUInt32Async(this Stream stream, uint value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(uint));
        try
        {
            BinaryEncoding.WriteUInt32(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(uint), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a 64-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<long> ReadInt64Async(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(long));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(long), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadInt64(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a 64-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteInt64Async(this Stream stream, long value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(long));
        try
        {
            BinaryEncoding.WriteInt64(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(long), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a 64-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<ulong> ReadUInt64Async(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(ulong));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(ulong), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadUInt64(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a 64-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteUInt64Async(this Stream stream, ulong value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(ulong));
        try
        {
            BinaryEncoding.WriteUInt64(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(ulong), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a single-precision floating point value from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<float> ReadSingleAsync(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(float));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(float), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadSingle(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a single-precision floating point value into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteSingleAsync(this Stream stream, float value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(float));
        try
        {
            BinaryEncoding.WriteSingle(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(float), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a double-precision floating point value from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<double> ReadDoubleAsync(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(double));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(double), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadDouble(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a double-precision floating point value into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteDoubleAsync(this Stream stream, double value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(double));
        try
        {
            BinaryEncoding.WriteDouble(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(double), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a UTF-16 code unit (2 bytes) from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<char> ReadCharAsync(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(char));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(char), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadChar(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a UTF-16 code unit (2 bytes) into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteCharAsync(this Stream stream, char value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(char));
        try
        {
            BinaryEncoding.WriteChar(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(char), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<decimal> ReadDecimalAsync(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(decimal));
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, sizeof(decimal), cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadDecimal(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a decimal value (16 bytes, same layout as <see cref="System.IO.BinaryWriter"/>) into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteDecimalAsync(this Stream stream, decimal value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(decimal));
        try
        {
            BinaryEncoding.WriteDecimal(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, sizeof(decimal), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a GUID (16 bytes, big endian follows RFC 4122) from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<Guid> ReadGuidAsync(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BinaryEncoding.GuidSize);
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, BinaryEncoding.GuidSize, cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadGuid(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a GUID (16 bytes, big endian follows RFC 4122) into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteGuidAsync(this Stream stream, Guid value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BinaryEncoding.GuidSize);
        try
        {
            BinaryEncoding.WriteGuid(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, BinaryEncoding.GuidSize, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a half-precision floating point value from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<Half> ReadHalfAsync(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BinaryEncoding.HalfSize);
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, BinaryEncoding.HalfSize, cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadHalf(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a half-precision floating point value into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteHalfAsync(this Stream stream, Half value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BinaryEncoding.HalfSize);
        try
        {
            BinaryEncoding.WriteHalf(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, BinaryEncoding.HalfSize, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a 128-bit signed integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<Int128> ReadInt128Async(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BinaryEncoding.Int128Size);
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, BinaryEncoding.Int128Size, cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadInt128(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a 128-bit signed integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteInt128Async(this Stream stream, Int128 value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BinaryEncoding.Int128Size);
        try
        {
            BinaryEncoding.WriteInt128(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, BinaryEncoding.Int128Size, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// Read a 128-bit unsigned integer from the stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    public static async ValueTask<UInt128> ReadUInt128Async(this Stream stream, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BinaryEncoding.Int128Size);
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, BinaryEncoding.Int128Size, cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.ReadUInt128(buffer, endianness);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a 128-bit unsigned integer into the stream
    /// </summary>
    /// <param name="stream">Stream to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="endianness">Byte order</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    public static async ValueTask WriteUInt128Async(this Stream stream, UInt128 value, Endianness endianness = Endianness.Little, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BinaryEncoding.Int128Size);
        try
        {
            BinaryEncoding.WriteUInt128(buffer, value, endianness);
            await StreamHelper.WriteAsync(stream, buffer, BinaryEncoding.Int128Size, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

#endif
}
