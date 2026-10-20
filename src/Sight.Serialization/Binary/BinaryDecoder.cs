using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using Sight.Serialization.Internal;

namespace Sight.Serialization.Binary;

/// <summary>
/// Low-level reader of SBDL primitives, from a stream or a buffer
/// </summary>
public sealed class BinaryDecoder : IDisposable
{
    private const int DefaultBufferSize = 64 * 1024;

    private readonly List<string> _strings = new();
    private readonly Stream? _stream;
    private readonly bool _pooled;
    private byte[] _buffer;
    private int _position;
    private int _end;
    private long _consumed;
    private bool _endOfStream;

    /// <summary>
    /// Initialize a decoder reading a stream
    /// </summary>
    public BinaryDecoder(Stream stream, int bufferSize = DefaultBufferSize)
    {
        _stream = stream;
        _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(bufferSize, 64));
        _pooled = true;
    }

    /// <summary>
    /// Initialize a decoder reading a buffer
    /// </summary>
    public BinaryDecoder(byte[] buffer, int offset, int count)
    {
        _buffer = buffer;
        _position = offset;
        _end = offset + count;
        _consumed = -offset;
        _endOfStream = true;
    }

    /// <summary>
    /// Initialize a decoder reading a buffer
    /// </summary>
    public BinaryDecoder(byte[] buffer)
        : this(buffer, 0, buffer.Length)
    {
    }

    /// <summary>
    /// Maximum length in bytes of deduplicated strings (0 disables deduplication)
    /// </summary>
    public byte DedupMaxLength { get; set; }

    /// <summary>
    /// Maximum accepted length of a string in bytes
    /// </summary>
    public int MaxStringLength { get; set; } = 1 << 24;

    /// <summary>
    /// Number of bytes read
    /// </summary>
    public long Position => _consumed + _position;

    /// <summary>
    /// Indicates whether the end of the input is reached
    /// </summary>
    public bool IsAtEnd => !TryEnsure(1);

    /// <summary>
    /// Read a raw byte
    /// </summary>
    public byte ReadByte()
    {
        Ensure(1);
        return _buffer[_position++];
    }

    /// <summary>
    /// Read raw bytes into a destination span
    /// </summary>
    public void ReadBytes(Span<byte> destination)
    {
        while (destination.Length > 0)
        {
            if (_position == _end && !TryFill(1))
                throw EndOfData();

            var count = Math.Min(destination.Length, _end - _position);
            new ReadOnlySpan<byte>(_buffer, _position, count).CopyTo(destination);
            _position += count;
            destination = destination.Slice(count);
        }
    }

    /// <summary>
    /// Skip raw bytes
    /// </summary>
    public void Skip(long count)
    {
        while (count > 0)
        {
            if (_position == _end && !TryFill(1))
                throw EndOfData();

            var step = (int)Math.Min(count, _end - _position);
            _position += step;
            count -= step;
        }
    }

    /// <summary>
    /// Read a boolean (0x00 / 0x01)
    /// </summary>
    public bool ReadBoolean()
    {
        var value = ReadByte();
        return value switch
        {
            0 => false,
            1 => true,
            _ => throw new SerializationException($"Invalid boolean value 0x{value:X2} at position {Position - 1}"),
        };
    }

    /// <summary>
    /// Read a variable-length unsigned integer
    /// </summary>
    public ulong ReadVarUInt()
    {
        Ensure(1);
        var first = _buffer[_position];
        if (first < 0x80)
        {
            _position++;
            return first;
        }

        var size = VarInt.GetSizeFromFirstByte(first);
        Ensure(size);
        _position += VarInt.Read(new ReadOnlySpan<byte>(_buffer, _position, size), out var value);
        return value;
    }

    /// <summary>
    /// Read a variable-length unsigned integer that must fit in an <see cref="int"/>
    /// </summary>
    public int ReadVarUInt32AsInt(string what)
    {
        var value = ReadVarUInt();
        if (value > int.MaxValue)
            throw new SerializationException($"Invalid {what} {value} at position {Position}");

        return (int)value;
    }

    /// <summary>
    /// Read a variable-length signed integer (ZigZag)
    /// </summary>
    public long ReadVarInt() => VarInt.ZigZagDecode(ReadVarUInt());

    /// <summary>
    /// Read a fixed 16-bit unsigned integer
    /// </summary>
    public ushort ReadFixedUInt16()
    {
        Ensure(2);
        var value = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(_buffer, _position, 2));
        _position += 2;
        return value;
    }

    /// <summary>
    /// Read a fixed 32-bit unsigned integer
    /// </summary>
    public uint ReadFixedUInt32()
    {
        Ensure(4);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(_buffer, _position, 4));
        _position += 4;
        return value;
    }

    /// <summary>
    /// Read a fixed 64-bit unsigned integer
    /// </summary>
    public ulong ReadFixedUInt64()
    {
        Ensure(8);
        var value = BinaryPrimitives.ReadUInt64LittleEndian(new ReadOnlySpan<byte>(_buffer, _position, 8));
        _position += 8;
        return value;
    }

    /// <summary>
    /// Read a 32-bit float
    /// </summary>
    public float ReadSingle() => BitHelper.Int32BitsToSingle((int)ReadFixedUInt32());

    /// <summary>
    /// Read a 64-bit float
    /// </summary>
    public double ReadDouble() => BitConverter.Int64BitsToDouble((long)ReadFixedUInt64());

    /// <summary>
    /// Read a string, resolving deduplicated references
    /// </summary>
    public string ReadString()
    {
        var x = ReadVarUInt();
        if ((x & 1) == 1)
        {
            var index = x >> 1;
            if (index >= (ulong)_strings.Count)
                throw new SerializationException($"Invalid string table index {index} at position {Position}");

            return _strings[(int)index];
        }

        var length = x >> 1;
        if (length > (ulong)MaxStringLength)
            throw new SerializationException($"String length {length} exceeds the limit of {MaxStringLength} bytes");

        var value = ReadUtf8((int)length);
        if (length > 0 && length <= DedupMaxLength)
            _strings.Add(value);

        return value;
    }

    /// <summary>
    /// Number of deduplicated strings read so far
    /// </summary>
    internal int DedupStringCount => _strings.Count;

    /// <summary>
    /// Deduplicated strings read since an index of the string table
    /// </summary>
    internal string[] GetDedupStrings(int start) => _strings.GetRange(start, _strings.Count - start).ToArray();

    /// <summary>
    /// Register strings as if they had been read, in order (decoder state after a cached prefix)
    /// </summary>
    internal void AddDedupStrings(IEnumerable<string> strings) => _strings.AddRange(strings);

    /// <summary>
    /// Consume the next bytes if they are equal to <paramref name="expected"/>; nothing is consumed otherwise
    /// </summary>
    internal bool TrySkipMatching(ReadOnlySpan<byte> expected)
    {
        if (!TryEnsure(expected.Length) || !new ReadOnlySpan<byte>(_buffer, _position, expected.Length).SequenceEqual(expected))
            return false;

        _position += expected.Length;
        return true;
    }

    /// <summary>
    /// Bytes read since a position, if they are still buffered
    /// </summary>
    internal byte[]? GetBytesReadSince(long position)
    {
        var start = position - _consumed;
        if (start < 0 || start > _position)
            return null;

        return new ReadOnlySpan<byte>(_buffer, (int)start, _position - (int)start).ToArray();
    }

    /// <summary>
    /// Get a stream returning the remaining unread bytes (buffered ones first, then the underlying stream)
    /// </summary>
    public Stream GetRemainingStream()
    {
        var remaining = new MemoryStream(_buffer, _position, _end - _position, writable: false);
        _consumed += _end - _position;
        _position = _end;
        return _stream == null || _endOfStream ? remaining : new ConcatStream(remaining, _stream);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_pooled && _buffer.Length > 0)
            ArrayPool<byte>.Shared.Return(_buffer);

        _buffer = Array.Empty<byte>();
        _position = _end = 0;
    }

    private string ReadUtf8(int length)
    {
        if (length == 0)
            return string.Empty;

        try
        {
            if (TryEnsure(length))
            {
                var value = Utf8.Strict.GetString(_buffer, _position, length);
                _position += length;
                return value;
            }

            var bytes = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                ReadBytes(new Span<byte>(bytes, 0, length));
                return Utf8.Strict.GetString(bytes, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }
        catch (System.Text.DecoderFallbackException e)
        {
            throw new SerializationException($"Invalid UTF-8 string at position {Position}", e);
        }
    }

    private void Ensure(int count)
    {
        if (!TryEnsure(count))
            throw EndOfData();
    }

    private bool TryEnsure(int count)
    {
        return _end - _position >= count || TryFill(count);
    }

    private bool TryFill(int count)
    {
        if (_stream == null || _endOfStream || count > _buffer.Length)
            return false;

        var remaining = _end - _position;
        if (remaining > 0 && _position > 0)
            Buffer.BlockCopy(_buffer, _position, _buffer, 0, remaining);

        _consumed += _position;
        _position = 0;
        _end = remaining;
        while (_end < count)
        {
            var read = _stream.Read(_buffer, _end, _buffer.Length - _end);
            if (read <= 0)
            {
                _endOfStream = true;
                return false;
            }

            _end += read;
        }

        return true;
    }

    private SerializationException EndOfData() => new($"Unexpected end of data at position {Position}");
}
