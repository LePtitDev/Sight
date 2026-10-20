using System;
using System.IO;

namespace Sight.Serialization.Internal;

/// <summary>
/// Read-only stream failing when more than a given number of bytes is read
/// </summary>
internal sealed class LimitedStream : Stream
{
    private readonly Stream _inner;
    private readonly long _limit;
    private long _read;

    public LimitedStream(Stream inner, long limit)
    {
        _inner = inner;
        _limit = limit;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _read;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        _read += read;
        if (_read > _limit)
            throw new SerializationException($"Decompressed size exceeds the limit of {_limit} bytes");

        return read;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
