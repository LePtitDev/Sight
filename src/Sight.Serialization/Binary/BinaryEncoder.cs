using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sight.Serialization.Internal;

namespace Sight.Serialization.Binary;

/// <summary>
/// Low-level writer of SBDL primitives into an in-memory buffer
/// </summary>
public sealed class BinaryEncoder : IDisposable
{
    private readonly Dictionary<string, int> _strings = new(StringComparer.Ordinal);
    private byte[] _buffer;
    private int _length;

    /// <summary>
    /// Initialize a new instance of <see cref="BinaryEncoder"/>
    /// </summary>
    public BinaryEncoder(int initialCapacity = 4096)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity, 16));
    }

    /// <summary>
    /// Maximum length in bytes of deduplicated strings (0 disables deduplication)
    /// </summary>
    public byte DedupMaxLength { get; set; }

    /// <summary>
    /// Number of bytes written
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Bytes written so far
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan => new(_buffer, 0, _length);

    /// <summary>
    /// Bytes written so far
    /// </summary>
    public ArraySegment<byte> WrittenSegment => new(_buffer, 0, _length);

    /// <summary>
    /// Write a raw byte
    /// </summary>
    public void WriteByte(byte value)
    {
        Ensure(1);
        _buffer[_length++] = value;
    }

    /// <summary>
    /// Write raw bytes
    /// </summary>
    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        Ensure(bytes.Length);
        bytes.CopyTo(new Span<byte>(_buffer, _length, bytes.Length));
        _length += bytes.Length;
    }

    /// <summary>
    /// Write a boolean (0x00 / 0x01)
    /// </summary>
    public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    /// <summary>
    /// Write a variable-length unsigned integer
    /// </summary>
    public void WriteVarUInt(ulong value)
    {
        if (value < 0x80)
        {
            WriteByte((byte)value);
            return;
        }

        Ensure(VarInt.MaxSize);
        _length += VarInt.Write(new Span<byte>(_buffer, _length, VarInt.MaxSize), value);
    }

    /// <summary>
    /// Write a variable-length signed integer (ZigZag)
    /// </summary>
    public void WriteVarInt(long value) => WriteVarUInt(VarInt.ZigZagEncode(value));

    /// <summary>
    /// Write a fixed 16-bit unsigned integer
    /// </summary>
    public void WriteFixedUInt16(ushort value)
    {
        Ensure(2);
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(_buffer, _length, 2), value);
        _length += 2;
    }

    /// <summary>
    /// Write a fixed 32-bit unsigned integer
    /// </summary>
    public void WriteFixedUInt32(uint value)
    {
        Ensure(4);
        BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(_buffer, _length, 4), value);
        _length += 4;
    }

    /// <summary>
    /// Write a fixed 64-bit unsigned integer
    /// </summary>
    public void WriteFixedUInt64(ulong value)
    {
        Ensure(8);
        BinaryPrimitives.WriteUInt64LittleEndian(new Span<byte>(_buffer, _length, 8), value);
        _length += 8;
    }

    /// <summary>
    /// Write a 32-bit float
    /// </summary>
    public void WriteSingle(float value) => WriteFixedUInt32((uint)BitHelper.SingleToInt32Bits(value));

    /// <summary>
    /// Write a 64-bit float
    /// </summary>
    public void WriteDouble(double value) => WriteFixedUInt64((ulong)BitConverter.DoubleToInt64Bits(value));

    /// <summary>
    /// Write a string, deduplicated if possible
    /// </summary>
    public void WriteString(string value)
    {
        if (DedupMaxLength > 0 && _strings.TryGetValue(value, out var index))
        {
            WriteVarUInt(((ulong)index << 1) | 1);
            return;
        }

        var byteCount = Utf8.Strict.GetByteCount(value);
        WriteVarUInt((ulong)byteCount << 1);
        Ensure(byteCount);
        Utf8.Strict.GetBytes(value, 0, value.Length, _buffer, _length);
        _length += byteCount;

        if (byteCount > 0 && byteCount <= DedupMaxLength)
            _strings.Add(value, _strings.Count);
    }

    /// <summary>
    /// Copy written bytes into a stream
    /// </summary>
    public void CopyTo(Stream stream) => stream.Write(_buffer, 0, _length);

    /// <summary>
    /// Copy written bytes into a stream
    /// </summary>
    public Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(_buffer, 0, _length, cancellationToken);
    }

    /// <summary>
    /// Copy written bytes into a new array
    /// </summary>
    public byte[] ToArray() => WrittenSpan.ToArray();

    /// <inheritdoc />
    public void Dispose()
    {
        var buffer = _buffer;
        _buffer = Array.Empty<byte>();
        _length = 0;
        if (buffer.Length > 0)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    private void Ensure(int count)
    {
        if (_length + count <= _buffer.Length)
            return;

        var size = Math.Max(_buffer.Length * 2, _length + count);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        Buffer.BlockCopy(_buffer, 0, buffer, 0, _length);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = buffer;
    }
}
