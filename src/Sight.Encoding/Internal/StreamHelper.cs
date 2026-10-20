using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sight.Encoding.Internal;

internal static class StreamHelper
{
    public static byte ReadByte(Stream stream)
    {
        var value = stream.ReadByte();
        if (value < 0)
            ThrowEndOfStream();

        return (byte)value;
    }

    public static void ReadExactly(Stream stream, Span<byte> buffer)
    {
#if NET8_0_OR_GREATER
        stream.ReadExactly(buffer);
#else
        var array = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            ReadExactly(stream, array, buffer.Length);
            array.AsSpan(0, buffer.Length).CopyTo(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
#endif
    }

    public static void ReadExactly(Stream stream, byte[] buffer, int count)
    {
#if NET8_0_OR_GREATER
        stream.ReadExactly(buffer, 0, count);
#else
        var offset = 0;
        while (offset < count)
        {
            var read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
                ThrowEndOfStream();

            offset += read;
        }
#endif
    }

    public static async ValueTask ReadExactlyAsync(Stream stream, byte[] buffer, int count, CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        await stream.ReadExactlyAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
#else
        var offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer, offset, count - offset, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                ThrowEndOfStream();

            offset += read;
        }
#endif
    }

    public static void Write(Stream stream, ReadOnlySpan<byte> buffer)
    {
#if NET8_0_OR_GREATER
        stream.Write(buffer);
#else
        var array = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            buffer.CopyTo(array);
            stream.Write(array, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
#endif
    }

    public static ValueTask WriteAsync(Stream stream, byte[] buffer, int count, CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        return stream.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
#else
        return new ValueTask(stream.WriteAsync(buffer, 0, count, cancellationToken));
#endif
    }

    private static void ThrowEndOfStream()
    {
        throw new EndOfStreamException("Unable to read beyond the end of the stream.");
    }
}
