using System;
using System.IO;

namespace Sight.Serialization.Internal;

/// <summary>
/// Read-only stream that reads a first stream, then a second one
/// </summary>
internal sealed class ConcatStream : Stream
{
    private readonly Stream _first;
    private readonly Stream _second;
    private bool _firstDone;

    public ConcatStream(Stream first, Stream second)
    {
        _first = first;
        _second = second;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!_firstDone)
        {
            var read = _first.Read(buffer, offset, count);
            if (read > 0)
                return read;

            _firstDone = true;
        }

        return _second.Read(buffer, offset, count);
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
