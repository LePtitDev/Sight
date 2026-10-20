namespace Sight.Encoding.Tests;

public class SpanReaderWriterTests
{
    [TestCase(Endianness.Little)]
    [TestCase(Endianness.Big)]
    public void RoundTrip(Endianness endianness)
    {
        var guid = Guid.NewGuid();
        var buffer = new byte[256];

        var writer = new SpanWriter(buffer, endianness);
        writer.WriteUInt8(1);
        writer.WriteInt16(-2);
        writer.WriteUInt32(3);
        writer.WriteInt64(-4);
        writer.WriteSingle(5.5f);
        writer.WriteDouble(6.25);
        writer.WriteBoolean(true);
        writer.WriteChar('x');
        writer.WriteDecimal(7.125m);
        writer.WriteGuid(guid);
#if NET8_0_OR_GREATER
        writer.WriteHalf((Half)8);
        writer.WriteUInt128(UInt128.MaxValue);
#endif
        writer.WriteVarUInt32(300);
        writer.WriteVarInt64(-9);
        writer.WriteString("Hello");
        writer.WriteBytes(new byte[] { 0xAA, 0xBB });
        var written = writer.Position;
        Assert.AreEqual(buffer.Length - written, writer.Remaining);
        Assert.AreEqual(written, writer.WrittenSpan.Length);

        var reader = new SpanReader(buffer.AsSpan(0, written), endianness);
        Assert.AreEqual(1, reader.ReadUInt8());
        Assert.AreEqual(-2, reader.ReadInt16());
        Assert.AreEqual(3u, reader.ReadUInt32());
        Assert.AreEqual(-4L, reader.ReadInt64());
        Assert.AreEqual(5.5f, reader.ReadSingle());
        Assert.AreEqual(6.25, reader.ReadDouble());
        Assert.IsTrue(reader.ReadBoolean());
        Assert.AreEqual('x', reader.ReadChar());
        Assert.AreEqual(7.125m, reader.ReadDecimal());
        Assert.AreEqual(guid, reader.ReadGuid());
#if NET8_0_OR_GREATER
        Assert.AreEqual((Half)8, reader.ReadHalf());
        Assert.AreEqual(UInt128.MaxValue, reader.ReadUInt128());
#endif
        Assert.AreEqual(300u, reader.ReadVarUInt32());
        Assert.AreEqual(-9L, reader.ReadVarInt64());
        Assert.AreEqual("Hello", reader.ReadString());
        Assert.AreEqual(new byte[] { 0xAA, 0xBB }, reader.ReadBytes(2).ToArray());
        Assert.IsTrue(reader.IsAtEnd);
        Assert.AreEqual(0, reader.Remaining);
    }

    [Test]
    public void EndiannessCanChangeWhileWriting()
    {
        var buffer = new byte[4];
        var writer = new SpanWriter(buffer);
        writer.WriteUInt16(0x0102);
        writer.Endianness = Endianness.Big;
        writer.WriteUInt16(0x0102);
        Assert.AreEqual(new byte[] { 0x02, 0x01, 0x01, 0x02 }, buffer);
    }

    [Test]
    public void TryReadKeepsPositionOnFailure()
    {
        // The two last bytes are an unterminated variable-length integer
        var reader = new SpanReader(new byte[] { 1, 0x80, 0x80 });
        Assert.IsTrue(reader.TryReadUInt8(out var first));
        Assert.AreEqual(1, first);
        Assert.IsFalse(reader.TryReadUInt32(out _));
        Assert.IsFalse(reader.TryReadVarUInt32(out _));
        Assert.IsFalse(reader.TryReadBytes(3, out _));
        Assert.IsFalse(reader.TryReadString(out _));
        Assert.AreEqual(1, reader.Position);
        Assert.IsTrue(reader.TryReadUInt16(out var second));
        Assert.AreEqual(0x8080, second);
    }

    [Test]
    public void ReadPastEnd()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var reader = new SpanReader(new byte[] { 1, 2, 3 });
            reader.ReadUInt32();
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var reader = new SpanReader(new byte[] { 1, 2, 3 });
            reader.Skip(4);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var reader = new SpanReader(new byte[] { 1, 2, 3 });
            reader.Position = 4;
        });
    }

    [Test]
    public void SkipAndPosition()
    {
        var reader = new SpanReader(new byte[] { 1, 2, 3, 4 });
        reader.Skip(2);
        Assert.AreEqual(3, reader.ReadUInt8());
        reader.Position = 0;
        Assert.AreEqual(1, reader.ReadUInt8());
        Assert.AreEqual(new byte[] { 2, 3, 4 }, reader.RemainingSpan.ToArray());
    }

    [Test]
    public void TryWriteWritesNothingOnFailure()
    {
        var buffer = new byte[3];
        var writer = new SpanWriter(buffer);
        Assert.IsTrue(writer.TryWriteUInt8(0xFF));
        Assert.IsFalse(writer.TryWriteUInt32(uint.MaxValue));
        Assert.IsFalse(writer.TryWriteVarUInt32(uint.MaxValue));
        Assert.IsFalse(writer.TryWriteString("Hello"));
        Assert.IsFalse(writer.TryWriteBytes(new byte[3]));
        Assert.AreEqual(1, writer.Position);
        Assert.AreEqual(new byte[] { 0xFF, 0, 0 }, buffer);
    }

    [Test]
    public void WritePastEnd()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var writer = new SpanWriter(new byte[3]);
            writer.WriteUInt32(1);
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var writer = new SpanWriter(new byte[3]);
            writer.WriteBytes(new byte[4]);
        });
    }
}
