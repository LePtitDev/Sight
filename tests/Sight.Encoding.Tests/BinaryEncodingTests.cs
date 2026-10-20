using System.IO;

namespace Sight.Encoding.Tests;

public class BinaryEncodingTests
{
    [Test]
    public void NativeEndianness()
    {
        Assert.AreEqual(BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big, BinaryEncoding.NativeEndianness);
    }

    [Test]
    public void UInt8()
    {
        var buffer = new byte[1];
        BinaryEncoding.WriteUInt8(buffer, 0xAB);
        Assert.AreEqual(new byte[] { 0xAB }, buffer);
        Assert.AreEqual(0xAB, BinaryEncoding.ReadUInt8(buffer));
    }

    [Test]
    public void Int8()
    {
        var buffer = new byte[1];
        BinaryEncoding.WriteInt8(buffer, -2);
        Assert.AreEqual(new byte[] { 0xFE }, buffer);
        Assert.AreEqual(-2, BinaryEncoding.ReadInt8(buffer));
    }

    [Test]
    public void Boolean()
    {
        var buffer = new byte[1];
        BinaryEncoding.WriteBoolean(buffer, true);
        Assert.AreEqual(new byte[] { 1 }, buffer);
        Assert.IsTrue(BinaryEncoding.ReadBoolean(buffer));
        Assert.IsTrue(BinaryEncoding.ReadBoolean(new byte[] { 42 }));
        Assert.IsFalse(BinaryEncoding.ReadBoolean(new byte[] { 0 }));
    }

    [TestCase(Endianness.Little, new byte[] { 0x02, 0x01 })]
    [TestCase(Endianness.Big, new byte[] { 0x01, 0x02 })]
    public void UInt16(Endianness endianness, byte[] expected)
    {
        AssertEncoding<ushort>(0x0102, expected, endianness, BinaryEncoding.WriteUInt16, BinaryEncoding.ReadUInt16);
    }

    [TestCase(Endianness.Little, new byte[] { 0xFE, 0xFF })]
    [TestCase(Endianness.Big, new byte[] { 0xFF, 0xFE })]
    public void Int16(Endianness endianness, byte[] expected)
    {
        AssertEncoding<short>(-2, expected, endianness, BinaryEncoding.WriteInt16, BinaryEncoding.ReadInt16);
    }

    [TestCase(Endianness.Little, new byte[] { 0x04, 0x03, 0x02, 0x01 })]
    [TestCase(Endianness.Big, new byte[] { 0x01, 0x02, 0x03, 0x04 })]
    public void UInt32(Endianness endianness, byte[] expected)
    {
        AssertEncoding<uint>(0x01020304, expected, endianness, BinaryEncoding.WriteUInt32, BinaryEncoding.ReadUInt32);
    }

    [TestCase(Endianness.Little, new byte[] { 0xFE, 0xFF, 0xFF, 0xFF })]
    [TestCase(Endianness.Big, new byte[] { 0xFF, 0xFF, 0xFF, 0xFE })]
    public void Int32(Endianness endianness, byte[] expected)
    {
        AssertEncoding(-2, expected, endianness, BinaryEncoding.WriteInt32, BinaryEncoding.ReadInt32);
    }

    [TestCase(Endianness.Little, new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 })]
    [TestCase(Endianness.Big, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 })]
    public void UInt64(Endianness endianness, byte[] expected)
    {
        AssertEncoding<ulong>(0x0102030405060708, expected, endianness, BinaryEncoding.WriteUInt64, BinaryEncoding.ReadUInt64);
    }

    [TestCase(Endianness.Little, new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    [TestCase(Endianness.Big, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE })]
    public void Int64(Endianness endianness, byte[] expected)
    {
        AssertEncoding<long>(-2, expected, endianness, BinaryEncoding.WriteInt64, BinaryEncoding.ReadInt64);
    }

    [TestCase(Endianness.Little, new byte[] { 0x00, 0x00, 0x80, 0x3F })]
    [TestCase(Endianness.Big, new byte[] { 0x3F, 0x80, 0x00, 0x00 })]
    public void Single(Endianness endianness, byte[] expected)
    {
        AssertEncoding(1f, expected, endianness, BinaryEncoding.WriteSingle, BinaryEncoding.ReadSingle);
    }

    [TestCase(Endianness.Little, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F })]
    [TestCase(Endianness.Big, new byte[] { 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    public void Double(Endianness endianness, byte[] expected)
    {
        AssertEncoding(1d, expected, endianness, BinaryEncoding.WriteDouble, BinaryEncoding.ReadDouble);
    }

    [TestCase(Endianness.Little, new byte[] { 0xAC, 0x20 })]
    [TestCase(Endianness.Big, new byte[] { 0x20, 0xAC })]
    public void Char(Endianness endianness, byte[] expected)
    {
        AssertEncoding('€', expected, endianness, BinaryEncoding.WriteChar, BinaryEncoding.ReadChar);
    }

    [Test]
    public void GuidBigEndianFollowsRfc4122()
    {
        var guid = new Guid("00112233-4455-6677-8899-aabbccddeeff");
        var expected = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
        AssertEncoding(guid, expected, Endianness.Big, BinaryEncoding.WriteGuid, BinaryEncoding.ReadGuid);
    }

    [Test]
    public void GuidLittleEndianMatchesToByteArray()
    {
        var guid = new Guid("00112233-4455-6677-8899-aabbccddeeff");
        AssertEncoding(guid, guid.ToByteArray(), Endianness.Little, BinaryEncoding.WriteGuid, BinaryEncoding.ReadGuid);
    }

    [TestCase(Endianness.Little)]
    [TestCase(Endianness.Big)]
    public void Decimal(Endianness endianness)
    {
        foreach (var value in new[] { 0m, 1m, -1m, 123456.789m, decimal.MaxValue, decimal.MinValue, 0.0000000000000000000000000001m })
        {
            var buffer = new byte[16];
            BinaryEncoding.WriteDecimal(buffer, value, endianness);
            Assert.AreEqual(value, BinaryEncoding.ReadDecimal(buffer, endianness));
        }
    }

    [Test]
    public void DecimalLittleEndianMatchesBinaryWriter()
    {
        var value = -123456.789m;
        var stream = new MemoryStream();
        new BinaryWriter(stream).Write(value);

        var buffer = new byte[16];
        BinaryEncoding.WriteDecimal(buffer, value);
        Assert.AreEqual(stream.ToArray(), buffer);
    }

    [Test]
    public void DecimalWithInvalidFlags()
    {
        var buffer = new byte[16];
        buffer[12] = 0xFF;
        Assert.Throws<FormatException>(() => BinaryEncoding.ReadDecimal(buffer));
    }

#if NET8_0_OR_GREATER
    [TestCase(Endianness.Little, new byte[] { 0x00, 0x3C })]
    [TestCase(Endianness.Big, new byte[] { 0x3C, 0x00 })]
    public void Half(Endianness endianness, byte[] expected)
    {
        AssertEncoding((Half)1, expected, endianness, BinaryEncoding.WriteHalf, BinaryEncoding.ReadHalf);
    }

    [TestCase(Endianness.Little)]
    [TestCase(Endianness.Big)]
    public void Int128(Endianness endianness)
    {
        var value = new Int128(0x0102030405060708, 0x090A0B0C0D0E0F10);
        var expected = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        if (endianness == Endianness.Little)
            Array.Reverse(expected);

        AssertEncoding(value, expected, endianness, BinaryEncoding.WriteInt128, BinaryEncoding.ReadInt128);
        AssertEncoding((UInt128)value, expected, endianness, BinaryEncoding.WriteUInt128, BinaryEncoding.ReadUInt128);
    }
#endif

    [Test]
    public void ReadTooSmallBuffer()
    {
        var buffer = new byte[3];
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.ReadUInt32(buffer));
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.ReadUInt8(Array.Empty<byte>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.ReadGuid(new byte[15]));
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.ReadDecimal(new byte[15]));
    }

    [Test]
    public void WriteTooSmallBufferWritesNothing()
    {
        var buffer = new byte[7];
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.WriteUInt64(buffer, ulong.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.WriteDecimal(buffer, decimal.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.WriteGuid(buffer, Guid.NewGuid()));
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.WriteUInt8(Array.Empty<byte>(), 1));
        Assert.AreEqual(new byte[7], buffer);
    }

    [Test]
    public void TryReadTooSmallBuffer()
    {
        Assert.IsFalse(BinaryEncoding.TryReadUInt32(new byte[3], out var value));
        Assert.AreEqual(0u, value);
        Assert.IsFalse(BinaryEncoding.TryReadUInt8(Array.Empty<byte>(), out _));
        Assert.IsFalse(BinaryEncoding.TryReadGuid(new byte[15], out _));
        Assert.IsTrue(BinaryEncoding.TryReadUInt32(new byte[] { 1, 0, 0, 0, 9 }, out value));
        Assert.AreEqual(1u, value);
    }

    [Test]
    public void TryWriteTooSmallBufferWritesNothing()
    {
        var buffer = new byte[3];
        Assert.IsFalse(BinaryEncoding.TryWriteUInt32(buffer, uint.MaxValue));
        Assert.IsFalse(BinaryEncoding.TryWriteDouble(buffer, 1d, Endianness.Big));
        Assert.AreEqual(new byte[3], buffer);
        Assert.IsTrue(BinaryEncoding.TryWriteUInt16(buffer, ushort.MaxValue));
        Assert.AreEqual(new byte[] { 0xFF, 0xFF, 0x00 }, buffer);
    }

    [Test]
    public void ReadAtOffset()
    {
        var buffer = new byte[] { 0xAA, 0x01, 0x02, 0xBB };
        Assert.AreEqual(0x0102, BinaryEncoding.ReadUInt16(buffer.AsSpan(1), Endianness.Big));
    }

    private static void AssertEncoding<T>(T value, byte[] expected, Endianness endianness, WriteDelegate<T> write, ReadDelegate<T> read)
    {
        var buffer = new byte[expected.Length];
        write(buffer, value, endianness);
        Assert.AreEqual(expected, buffer);
        Assert.AreEqual(value, read(buffer, endianness));
    }

    private delegate void WriteDelegate<in T>(Span<byte> destination, T value, Endianness endianness);

    private delegate T ReadDelegate<out T>(ReadOnlySpan<byte> source, Endianness endianness);
}
