using System;
using System.Diagnostics.CodeAnalysis;

namespace Sight.Encoding;

public static partial class BinaryEncoding
{
    // Fully qualified: "Encoding" alone resolves to this namespace
    internal static System.Text.Encoding Utf8 { get; } = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Get the size in bytes of an encoded string
    /// </summary>
    public static int GetStringSize(string value)
    {
        var byteCount = Utf8.GetByteCount(value);
        return GetVarUInt32Size((uint)byteCount) + byteCount;
    }

    /// <summary>
    /// Write a string as UTF-8 bytes prefixed by their count as a variable-length integer.
    /// Same format as <c>BinaryWriter.Write(string)</c> with the UTF-8 encoding.
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <returns>Number of bytes written</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small, nothing is written.</exception>
    public static int WriteString(Span<byte> destination, string value)
    {
        if (!TryWriteString(destination, value, out var bytesWritten))
            ThrowTooSmall(nameof(destination));

        return bytesWritten;
    }

    /// <summary>
    /// Try to write a string as UTF-8 bytes prefixed by their count as a variable-length integer
    /// </summary>
    /// <param name="destination">Bytes to write into</param>
    /// <param name="value">Value to write</param>
    /// <param name="bytesWritten">Number of bytes written</param>
    /// <returns><c>false</c> if <paramref name="destination"/> is too small, nothing is written</returns>
    public static bool TryWriteString(Span<byte> destination, string value, out int bytesWritten)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var byteCount = Utf8.GetByteCount(value);
        var prefixSize = GetVarUInt32Size((uint)byteCount);
        if (destination.Length < prefixSize + byteCount)
        {
            bytesWritten = 0;
            return false;
        }

        WriteVarUInt32(destination, (uint)byteCount);
#if NET8_0_OR_GREATER
        Utf8.GetBytes(value, destination.Slice(prefixSize));
#else
        Utf8.GetBytes(value).CopyTo(destination.Slice(prefixSize));
#endif
        bytesWritten = prefixSize + byteCount;
        return true;
    }

    /// <summary>
    /// Read a string written by <see cref="WriteString"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> ends before the value.</exception>
    /// <exception cref="FormatException">The length prefix is invalid.</exception>
    public static string ReadString(ReadOnlySpan<byte> source, out int bytesRead)
    {
        if (!TryReadString(source, out var value, out bytesRead))
            ThrowTooSmall(nameof(source));

        return value!;
    }

    /// <summary>
    /// Try to read a string written by <see cref="WriteString"/>
    /// </summary>
    /// <param name="source">Bytes to read from</param>
    /// <param name="value">Read value</param>
    /// <param name="bytesRead">Number of bytes read</param>
    /// <returns><c>false</c> if <paramref name="source"/> ends before the value</returns>
    /// <exception cref="FormatException">The length prefix is invalid.</exception>
    public static bool TryReadString(ReadOnlySpan<byte> source, [NotNullWhen(true)] out string? value, out int bytesRead)
    {
        if (!TryReadVarUInt32(source, out var byteCount, out var prefixSize) || byteCount > (uint)(source.Length - prefixSize))
        {
            value = null;
            bytesRead = 0;
            return false;
        }

        var bytes = source.Slice(prefixSize, (int)byteCount);
#if NET8_0_OR_GREATER
        value = Utf8.GetString(bytes);
#else
        value = Utf8.GetString(bytes.ToArray());
#endif
        bytesRead = prefixSize + (int)byteCount;
        return true;
    }
}
