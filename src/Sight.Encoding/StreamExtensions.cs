using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sight.Encoding.Internal;

namespace Sight.Encoding;

/// <summary>
/// Read and write binary values from/into a <see cref="Stream"/>
/// </summary>
public static partial class StreamExtensions
{
    /// <summary>
    /// Read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static uint ReadVarUInt32(this Stream stream) => (uint)ReadVarInteger(stream, 32);

    /// <summary>
    /// Read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static ulong ReadVarUInt64(this Stream stream) => ReadVarInteger(stream, 64);

    /// <summary>
    /// Read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static int ReadVarInt32(this Stream stream) => ZigZag.Decode(stream.ReadVarUInt32());

    /// <summary>
    /// Read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static long ReadVarInt64(this Stream stream) => ZigZag.Decode(stream.ReadVarUInt64());

    /// <summary>
    /// Write a variable-length unsigned integer (LEB128)
    /// </summary>
    public static void WriteVarUInt32(this Stream stream, uint value) => stream.WriteVarUInt64(value);

    /// <summary>
    /// Write a variable-length unsigned integer (LEB128)
    /// </summary>
    public static void WriteVarUInt64(this Stream stream, ulong value)
    {
        Span<byte> buffer = stackalloc byte[BinaryEncoding.MaxVarInt64Size];
        var size = BinaryEncoding.WriteVarUInt64(buffer, value);
        StreamHelper.Write(stream, buffer.Slice(0, size));
    }

    /// <summary>
    /// Write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    public static void WriteVarInt32(this Stream stream, int value) => stream.WriteVarUInt32(ZigZag.Encode(value));

    /// <summary>
    /// Write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    public static void WriteVarInt64(this Stream stream, long value) => stream.WriteVarUInt64(ZigZag.Encode(value));

    /// <summary>
    /// Read a string written by <see cref="WriteString"/>
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="maxByteCount">Maximum accepted size of the UTF-8 bytes, protects against corrupted or malicious length prefixes</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="InvalidDataException">The string is longer than <paramref name="maxByteCount"/>.</exception>
    public static string ReadString(this Stream stream, int maxByteCount = int.MaxValue)
    {
        CheckMaxByteCount(maxByteCount);
        var byteCount = CheckStringByteCount(stream.ReadVarUInt32(), maxByteCount);
        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            StreamHelper.ReadExactly(stream, buffer, byteCount);
            return BinaryEncoding.Utf8.GetString(buffer, 0, byteCount);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a string as UTF-8 bytes prefixed by their count as a variable-length integer
    /// </summary>
    public static void WriteString(this Stream stream, string value)
    {
        var size = BinaryEncoding.GetStringSize(value);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            BinaryEncoding.WriteString(buffer, value);
            stream.Write(buffer, 0, size);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static async ValueTask<uint> ReadVarUInt32Async(this Stream stream, CancellationToken cancellationToken = default)
    {
        return (uint)await ReadVarIntegerAsync(stream, 32, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Read a variable-length unsigned integer (LEB128)
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static ValueTask<ulong> ReadVarUInt64Async(this Stream stream, CancellationToken cancellationToken = default)
    {
        return ReadVarIntegerAsync(stream, 64, cancellationToken);
    }

    /// <summary>
    /// Read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static async ValueTask<int> ReadVarInt32Async(this Stream stream, CancellationToken cancellationToken = default)
    {
        return ZigZag.Decode(await stream.ReadVarUInt32Async(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Read a variable-length signed integer (LEB128 then ZigZag)
    /// </summary>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="FormatException">The encoded value is too long or overflows.</exception>
    public static async ValueTask<long> ReadVarInt64Async(this Stream stream, CancellationToken cancellationToken = default)
    {
        return ZigZag.Decode(await stream.ReadVarUInt64Async(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Write a variable-length unsigned integer (LEB128)
    /// </summary>
    public static ValueTask WriteVarUInt32Async(this Stream stream, uint value, CancellationToken cancellationToken = default)
    {
        return stream.WriteVarUInt64Async(value, cancellationToken);
    }

    /// <summary>
    /// Write a variable-length unsigned integer (LEB128)
    /// </summary>
    public static async ValueTask WriteVarUInt64Async(this Stream stream, ulong value, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BinaryEncoding.MaxVarInt64Size);
        try
        {
            var size = BinaryEncoding.WriteVarUInt64(buffer, value);
            await StreamHelper.WriteAsync(stream, buffer, size, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    public static ValueTask WriteVarInt32Async(this Stream stream, int value, CancellationToken cancellationToken = default)
    {
        return stream.WriteVarUInt32Async(ZigZag.Encode(value), cancellationToken);
    }

    /// <summary>
    /// Write a variable-length signed integer (ZigZag then LEB128)
    /// </summary>
    public static ValueTask WriteVarInt64Async(this Stream stream, long value, CancellationToken cancellationToken = default)
    {
        return stream.WriteVarUInt64Async(ZigZag.Encode(value), cancellationToken);
    }

    /// <summary>
    /// Read a string written by <see cref="WriteString"/>
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="maxByteCount">Maximum accepted size of the UTF-8 bytes, protects against corrupted or malicious length prefixes</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <exception cref="EndOfStreamException">The end of the stream is reached before the value is fully read.</exception>
    /// <exception cref="InvalidDataException">The string is longer than <paramref name="maxByteCount"/>.</exception>
    public static async ValueTask<string> ReadStringAsync(this Stream stream, int maxByteCount = int.MaxValue, CancellationToken cancellationToken = default)
    {
        CheckMaxByteCount(maxByteCount);
        var byteCount = CheckStringByteCount(await stream.ReadVarUInt32Async(cancellationToken).ConfigureAwait(false), maxByteCount);
        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            await StreamHelper.ReadExactlyAsync(stream, buffer, byteCount, cancellationToken).ConfigureAwait(false);
            return BinaryEncoding.Utf8.GetString(buffer, 0, byteCount);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Write a string as UTF-8 bytes prefixed by their count as a variable-length integer
    /// </summary>
    public static async ValueTask WriteStringAsync(this Stream stream, string value, CancellationToken cancellationToken = default)
    {
        var size = BinaryEncoding.GetStringSize(value);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            BinaryEncoding.WriteString(buffer, value);
            await StreamHelper.WriteAsync(stream, buffer, size, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static ulong ReadVarInteger(Stream stream, int bits)
    {
        ulong value = 0;
        for (var index = 0; ; index++)
        {
            if (BinaryEncoding.AccumulateVarInteger(ref value, index, StreamHelper.ReadByte(stream), bits))
                return value;
        }
    }

    private static async ValueTask<ulong> ReadVarIntegerAsync(Stream stream, int bits, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            ulong value = 0;
            for (var index = 0; ; index++)
            {
                await StreamHelper.ReadExactlyAsync(stream, buffer, 1, cancellationToken).ConfigureAwait(false);
                if (BinaryEncoding.AccumulateVarInteger(ref value, index, buffer[0], bits))
                    return value;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void CheckMaxByteCount(int maxByteCount)
    {
        if (maxByteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maxByteCount));
    }

    private static int CheckStringByteCount(uint byteCount, int maxByteCount)
    {
        if (byteCount > (uint)maxByteCount)
            throw new InvalidDataException($"The string is {byteCount} bytes long, the limit is {maxByteCount} bytes.");

        return (int)byteCount;
    }
}
