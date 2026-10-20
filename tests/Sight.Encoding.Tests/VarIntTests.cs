using System.IO;

namespace Sight.Encoding.Tests;

public class VarIntTests
{
    [TestCase(0u, new byte[] { 0x00 })]
    [TestCase(1u, new byte[] { 0x01 })]
    [TestCase(127u, new byte[] { 0x7F })]
    [TestCase(128u, new byte[] { 0x80, 0x01 })]
    [TestCase(300u, new byte[] { 0xAC, 0x02 })]
    [TestCase(16384u, new byte[] { 0x80, 0x80, 0x01 })]
    [TestCase(uint.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F })]
    public void VarUInt32(uint value, byte[] expected)
    {
        var buffer = new byte[BinaryEncoding.MaxVarInt32Size];
        Assert.AreEqual(expected.Length, BinaryEncoding.GetVarUInt32Size(value));
        Assert.AreEqual(expected.Length, BinaryEncoding.WriteVarUInt32(buffer, value));
        Assert.AreEqual(expected, buffer.AsSpan(0, expected.Length).ToArray());
        Assert.AreEqual(value, BinaryEncoding.ReadVarUInt32(buffer, out var bytesRead));
        Assert.AreEqual(expected.Length, bytesRead);
    }

    [TestCase(0ul, 1)]
    [TestCase(127ul, 1)]
    [TestCase(128ul, 2)]
    [TestCase(uint.MaxValue, 5)]
    [TestCase(ulong.MaxValue >> 1, 9)]
    [TestCase(ulong.MaxValue, 10)]
    public void VarUInt64(ulong value, int expectedSize)
    {
        var buffer = new byte[BinaryEncoding.MaxVarInt64Size];
        Assert.AreEqual(expectedSize, BinaryEncoding.GetVarUInt64Size(value));
        Assert.AreEqual(expectedSize, BinaryEncoding.WriteVarUInt64(buffer, value));
        Assert.AreEqual(value, BinaryEncoding.ReadVarUInt64(buffer, out var bytesRead));
        Assert.AreEqual(expectedSize, bytesRead);
    }

    [Test]
    public void VarUInt64MaxValueBytes()
    {
        var buffer = new byte[BinaryEncoding.MaxVarInt64Size];
        BinaryEncoding.WriteVarUInt64(buffer, ulong.MaxValue);
        Assert.AreEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 }, buffer);
    }

    [TestCase(0, new byte[] { 0x00 })]
    [TestCase(-1, new byte[] { 0x01 })]
    [TestCase(1, new byte[] { 0x02 })]
    [TestCase(-64, new byte[] { 0x7F })]
    [TestCase(64, new byte[] { 0x80, 0x01 })]
    [TestCase(int.MaxValue, new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0x0F })]
    [TestCase(int.MinValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F })]
    public void VarInt32(int value, byte[] expected)
    {
        var buffer = new byte[BinaryEncoding.MaxVarInt32Size];
        Assert.AreEqual(expected.Length, BinaryEncoding.GetVarInt32Size(value));
        Assert.AreEqual(expected.Length, BinaryEncoding.WriteVarInt32(buffer, value));
        Assert.AreEqual(expected, buffer.AsSpan(0, expected.Length).ToArray());
        Assert.AreEqual(value, BinaryEncoding.ReadVarInt32(buffer, out var bytesRead));
        Assert.AreEqual(expected.Length, bytesRead);
    }

    [TestCase(0L)]
    [TestCase(-1L)]
    [TestCase(long.MaxValue)]
    [TestCase(long.MinValue)]
    public void VarInt64(long value)
    {
        var buffer = new byte[BinaryEncoding.MaxVarInt64Size];
        var size = BinaryEncoding.WriteVarInt64(buffer, value);
        Assert.AreEqual(BinaryEncoding.GetVarInt64Size(value), size);
        Assert.AreEqual(value, BinaryEncoding.ReadVarInt64(buffer, out var bytesRead));
        Assert.AreEqual(size, bytesRead);
    }

#if NET8_0_OR_GREATER
    [TestCase(0)]
    [TestCase(127)]
    [TestCase(128)]
    [TestCase(int.MaxValue)]
    public void CompatibleWithBinaryWriter(int value)
    {
        var stream = new MemoryStream();
        new BinaryWriter(stream).Write7BitEncodedInt(value);

        var buffer = new byte[BinaryEncoding.MaxVarInt32Size];
        var size = BinaryEncoding.WriteVarUInt32(buffer, (uint)value);
        Assert.AreEqual(stream.ToArray(), buffer.AsSpan(0, size).ToArray());
    }
#endif

    [Test]
    public void TruncatedValue()
    {
        var buffer = new byte[] { 0x80, 0x80 };
        Assert.IsFalse(BinaryEncoding.TryReadVarUInt32(buffer, out var value, out var bytesRead));
        Assert.AreEqual(0u, value);
        Assert.AreEqual(0, bytesRead);
        Assert.IsFalse(BinaryEncoding.TryReadVarInt64(Array.Empty<byte>(), out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.ReadVarUInt64(buffer, out _));
    }

    [Test]
    public void OverflowingValue()
    {
        // 5th byte of a 32-bit value can only hold 4 bits
        Assert.Throws<FormatException>(() => BinaryEncoding.ReadVarUInt32(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x1F }, out _));
        // Too many bytes
        Assert.Throws<FormatException>(() => BinaryEncoding.ReadVarUInt32(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80, 0x00 }, out _));
        Assert.Throws<FormatException>(() => BinaryEncoding.TryReadVarUInt64(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02 }, out _, out _));
    }

    [Test]
    public void WriteTooSmallBufferWritesNothing()
    {
        var buffer = new byte[2];
        Assert.IsFalse(BinaryEncoding.TryWriteVarUInt32(buffer, 16384, out var bytesWritten));
        Assert.AreEqual(0, bytesWritten);
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.WriteVarUInt32(buffer, 16384));
        Assert.AreEqual(new byte[2], buffer);
    }

    [TestCase(0, 0u)]
    [TestCase(-1, 1u)]
    [TestCase(1, 2u)]
    [TestCase(-2, 3u)]
    [TestCase(int.MaxValue, uint.MaxValue - 1)]
    [TestCase(int.MinValue, uint.MaxValue)]
    public void ZigZag32(int value, uint encoded)
    {
        Assert.AreEqual(encoded, ZigZag.Encode(value));
        Assert.AreEqual(value, ZigZag.Decode(encoded));
    }

    [TestCase(0L, 0ul)]
    [TestCase(-1L, 1ul)]
    [TestCase(1L, 2ul)]
    [TestCase(long.MaxValue, ulong.MaxValue - 1)]
    [TestCase(long.MinValue, ulong.MaxValue)]
    public void ZigZag64(long value, ulong encoded)
    {
        Assert.AreEqual(encoded, ZigZag.Encode(value));
        Assert.AreEqual(value, ZigZag.Decode(encoded));
    }
}
