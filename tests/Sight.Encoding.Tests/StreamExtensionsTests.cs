using System.IO;
using System.Threading;

namespace Sight.Encoding.Tests;

public class StreamExtensionsTests
{
    [TestCase(Endianness.Little)]
    [TestCase(Endianness.Big)]
    public void RoundTrip(Endianness endianness)
    {
        var guid = Guid.NewGuid();
        var stream = new MemoryStream();
        stream.WriteInt8(-1);
        stream.WriteUInt8(2);
        stream.WriteBoolean(true);
        stream.WriteInt16(-3, endianness);
        stream.WriteUInt16(4, endianness);
        stream.WriteInt32(-5, endianness);
        stream.WriteUInt32(6, endianness);
        stream.WriteInt64(-7, endianness);
        stream.WriteUInt64(8, endianness);
        stream.WriteSingle(9.5f, endianness);
        stream.WriteDouble(10.25, endianness);
        stream.WriteChar('€', endianness);
        stream.WriteDecimal(11.125m, endianness);
        stream.WriteGuid(guid, endianness);
#if NET8_0_OR_GREATER
        stream.WriteHalf((Half)12, endianness);
        stream.WriteInt128(Int128.MinValue, endianness);
        stream.WriteUInt128(UInt128.MaxValue, endianness);
#endif
        stream.WriteVarUInt32(13);
        stream.WriteVarUInt64(ulong.MaxValue);
        stream.WriteVarInt32(-14);
        stream.WriteVarInt64(long.MinValue);
        stream.WriteString("Ünïcödé");

        stream.Position = 0;
        Assert.AreEqual(-1, stream.ReadInt8());
        Assert.AreEqual(2, stream.ReadUInt8());
        Assert.IsTrue(stream.ReadBoolean());
        Assert.AreEqual(-3, stream.ReadInt16(endianness));
        Assert.AreEqual(4, stream.ReadUInt16(endianness));
        Assert.AreEqual(-5, stream.ReadInt32(endianness));
        Assert.AreEqual(6u, stream.ReadUInt32(endianness));
        Assert.AreEqual(-7L, stream.ReadInt64(endianness));
        Assert.AreEqual(8ul, stream.ReadUInt64(endianness));
        Assert.AreEqual(9.5f, stream.ReadSingle(endianness));
        Assert.AreEqual(10.25, stream.ReadDouble(endianness));
        Assert.AreEqual('€', stream.ReadChar(endianness));
        Assert.AreEqual(11.125m, stream.ReadDecimal(endianness));
        Assert.AreEqual(guid, stream.ReadGuid(endianness));
#if NET8_0_OR_GREATER
        Assert.AreEqual((Half)12, stream.ReadHalf(endianness));
        Assert.AreEqual(Int128.MinValue, stream.ReadInt128(endianness));
        Assert.AreEqual(UInt128.MaxValue, stream.ReadUInt128(endianness));
#endif
        Assert.AreEqual(13u, stream.ReadVarUInt32());
        Assert.AreEqual(ulong.MaxValue, stream.ReadVarUInt64());
        Assert.AreEqual(-14, stream.ReadVarInt32());
        Assert.AreEqual(long.MinValue, stream.ReadVarInt64());
        Assert.AreEqual("Ünïcödé", stream.ReadString());
        Assert.AreEqual(stream.Length, stream.Position);
    }

    [TestCase(Endianness.Little)]
    [TestCase(Endianness.Big)]
    public async Task RoundTripAsync(Endianness endianness)
    {
        var guid = Guid.NewGuid();
        var stream = new MemoryStream();
        await stream.WriteInt8Async(-1);
        await stream.WriteUInt8Async(2);
        await stream.WriteBooleanAsync(true);
        await stream.WriteInt16Async(-3, endianness);
        await stream.WriteUInt16Async(4, endianness);
        await stream.WriteInt32Async(-5, endianness);
        await stream.WriteUInt32Async(6, endianness);
        await stream.WriteInt64Async(-7, endianness);
        await stream.WriteUInt64Async(8, endianness);
        await stream.WriteSingleAsync(9.5f, endianness);
        await stream.WriteDoubleAsync(10.25, endianness);
        await stream.WriteCharAsync('€', endianness);
        await stream.WriteDecimalAsync(11.125m, endianness);
        await stream.WriteGuidAsync(guid, endianness);
#if NET8_0_OR_GREATER
        await stream.WriteHalfAsync((Half)12, endianness);
        await stream.WriteInt128Async(Int128.MinValue, endianness);
        await stream.WriteUInt128Async(UInt128.MaxValue, endianness);
#endif
        await stream.WriteVarUInt32Async(13);
        await stream.WriteVarUInt64Async(ulong.MaxValue);
        await stream.WriteVarInt32Async(-14);
        await stream.WriteVarInt64Async(long.MinValue);
        await stream.WriteStringAsync("Ünïcödé");

        stream.Position = 0;
        Assert.AreEqual(-1, await stream.ReadInt8Async());
        Assert.AreEqual(2, await stream.ReadUInt8Async());
        Assert.IsTrue(await stream.ReadBooleanAsync());
        Assert.AreEqual(-3, await stream.ReadInt16Async(endianness));
        Assert.AreEqual(4, await stream.ReadUInt16Async(endianness));
        Assert.AreEqual(-5, await stream.ReadInt32Async(endianness));
        Assert.AreEqual(6u, await stream.ReadUInt32Async(endianness));
        Assert.AreEqual(-7L, await stream.ReadInt64Async(endianness));
        Assert.AreEqual(8ul, await stream.ReadUInt64Async(endianness));
        Assert.AreEqual(9.5f, await stream.ReadSingleAsync(endianness));
        Assert.AreEqual(10.25, await stream.ReadDoubleAsync(endianness));
        Assert.AreEqual('€', await stream.ReadCharAsync(endianness));
        Assert.AreEqual(11.125m, await stream.ReadDecimalAsync(endianness));
        Assert.AreEqual(guid, await stream.ReadGuidAsync(endianness));
#if NET8_0_OR_GREATER
        Assert.AreEqual((Half)12, await stream.ReadHalfAsync(endianness));
        Assert.AreEqual(Int128.MinValue, await stream.ReadInt128Async(endianness));
        Assert.AreEqual(UInt128.MaxValue, await stream.ReadUInt128Async(endianness));
#endif
        Assert.AreEqual(13u, await stream.ReadVarUInt32Async());
        Assert.AreEqual(ulong.MaxValue, await stream.ReadVarUInt64Async());
        Assert.AreEqual(-14, await stream.ReadVarInt32Async());
        Assert.AreEqual(long.MinValue, await stream.ReadVarInt64Async());
        Assert.AreEqual("Ünïcödé", await stream.ReadStringAsync());
        Assert.AreEqual(stream.Length, stream.Position);
    }

    [Test]
    public void SameBytesAsBinaryEncoding()
    {
        var stream = new MemoryStream();
        stream.WriteUInt32(0x01020304, Endianness.Big);
        Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, stream.ToArray());
    }

    [Test]
    public void ReadsAcrossPartialReads()
    {
        var stream = new OneByteStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });
        Assert.AreEqual(0x0102030405060708ul, stream.ReadUInt64(Endianness.Big));
    }

    [Test]
    public async Task ReadsAcrossPartialReadsAsync()
    {
        var stream = new OneByteStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });
        Assert.AreEqual(0x0102030405060708ul, await stream.ReadUInt64Async(Endianness.Big));
    }

    [Test]
    public void EndOfStream()
    {
        Assert.Throws<EndOfStreamException>(() => new MemoryStream(new byte[3]).ReadUInt32());
        Assert.Throws<EndOfStreamException>(() => new MemoryStream().ReadUInt8());
        Assert.Throws<EndOfStreamException>(() => new MemoryStream(new byte[] { 0x80 }).ReadVarUInt32());
        Assert.Throws<EndOfStreamException>(() => new MemoryStream(new byte[] { 0x05, (byte)'a' }).ReadString());
    }

    [Test]
    public void EndOfStreamAsync()
    {
        Assert.ThrowsAsync<EndOfStreamException>(async () => await new MemoryStream(new byte[3]).ReadUInt32Async());
        Assert.ThrowsAsync<EndOfStreamException>(async () => await new MemoryStream(new byte[] { 0x80 }).ReadVarUInt32Async());
        Assert.ThrowsAsync<EndOfStreamException>(async () => await new MemoryStream(new byte[] { 0x05, (byte)'a' }).ReadStringAsync());
    }

    [Test]
    public void OverflowingVarInt()
    {
        Assert.Throws<FormatException>(() => new MemoryStream(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x1F }).ReadVarUInt32());
        Assert.ThrowsAsync<FormatException>(async () => await new MemoryStream(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x1F }).ReadVarUInt32Async());
    }

    [Test]
    public void StringLengthLimit()
    {
        var stream = new MemoryStream();
        stream.WriteString("Hello");

        stream.Position = 0;
        Assert.Throws<InvalidDataException>(() => stream.ReadString(maxByteCount: 4));
        stream.Position = 0;
        Assert.ThrowsAsync<InvalidDataException>(async () => await stream.ReadStringAsync(maxByteCount: 4));
        stream.Position = 0;
        Assert.AreEqual("Hello", stream.ReadString(maxByteCount: 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.ReadString(maxByteCount: -1));
    }

    [Test]
    public void Cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () => await new MemoryStream(new byte[4]).ReadUInt32Async(cancellationToken: cancellation.Token));
    }

    /// <summary>
    /// Stream returning at most one byte per read
    /// </summary>
    private class OneByteStream : MemoryStream
    {
        public OneByteStream(byte[] buffer)
            : base(buffer)
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));

#if NET8_0_OR_GREATER
        public override int Read(Span<byte> buffer) => base.Read(buffer.Slice(0, Math.Min(buffer.Length, 1)));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.ReadAsync(buffer, offset, Math.Min(count, 1), cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => base.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, 1)), cancellationToken);
#endif
    }
}
