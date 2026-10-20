using System.IO;

namespace Sight.Encoding.Tests;

public class BinaryReaderWriterExtensionsTests
{
    [TestCase(Endianness.Little)]
    [TestCase(Endianness.Big)]
    public void SameBytesAsBinaryEncoding(Endianness endianness)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        writer.WriteInt16(-1, endianness);
        writer.WriteUInt16(2, endianness);
        writer.WriteInt32(-3, endianness);
        writer.WriteUInt32(4, endianness);
        writer.WriteInt64(-5, endianness);
        writer.WriteUInt64(6, endianness);
        writer.WriteSingle(7.5f, endianness);
        writer.WriteDouble(8.25, endianness);
#if NET8_0_OR_GREATER
        writer.WriteHalf((Half)9, endianness);
#endif
        writer.Flush();

        var expected = new MemoryStream();
        expected.WriteInt16(-1, endianness);
        expected.WriteUInt16(2, endianness);
        expected.WriteInt32(-3, endianness);
        expected.WriteUInt32(4, endianness);
        expected.WriteInt64(-5, endianness);
        expected.WriteUInt64(6, endianness);
        expected.WriteSingle(7.5f, endianness);
        expected.WriteDouble(8.25, endianness);
#if NET8_0_OR_GREATER
        expected.WriteHalf((Half)9, endianness);
#endif
        Assert.AreEqual(expected.ToArray(), stream.ToArray());

        stream.Position = 0;
        var reader = new BinaryReader(stream);
        Assert.AreEqual(-1, reader.ReadInt16(endianness));
        Assert.AreEqual(2, reader.ReadUInt16(endianness));
        Assert.AreEqual(-3, reader.ReadInt32(endianness));
        Assert.AreEqual(4u, reader.ReadUInt32(endianness));
        Assert.AreEqual(-5L, reader.ReadInt64(endianness));
        Assert.AreEqual(6ul, reader.ReadUInt64(endianness));
        Assert.AreEqual(7.5f, reader.ReadSingle(endianness));
        Assert.AreEqual(8.25, reader.ReadDouble(endianness));
#if NET8_0_OR_GREATER
        Assert.AreEqual((Half)9, reader.ReadHalf(endianness));
#endif
    }

    [Test]
    public void BigEndianBytes()
    {
        var stream = new MemoryStream();
        new BinaryWriter(stream).WriteUInt32(0x01020304, Endianness.Big);
        Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 }, stream.ToArray());
    }
}
