using System.IO;
using Sight.Serialization.Binary;
using Sight.Serialization.Internal;

namespace Sight.Serialization.Tests;

public class PrimitiveTests
{
    [TestCase(0UL, "00")]
    [TestCase(127UL, "7F")]
    [TestCase(128UL, "80 80")]
    [TestCase(300UL, "81 2C")]
    [TestCase(16383UL, "BF FF")]
    [TestCase(16384UL, "C0 00 40")]
    [TestCase(72057594037927935UL, "FE FF FF FF FF FF FF FF")]
    [TestCase(18446744073709551615UL, "FF FF FF FF FF FF FF FF FF")]
    public void VarIntEncoding(ulong value, string expected)
    {
        using var encoder = new BinaryEncoder();
        encoder.WriteVarUInt(value);
        Assert.AreEqual(expected, Hex(encoder.ToArray()));
        Assert.AreEqual(encoder.Length, VarInt.GetSize(value));

        var decoder = new BinaryDecoder(encoder.ToArray());
        Assert.AreEqual(value, decoder.ReadVarUInt());
        Assert.IsTrue(decoder.IsAtEnd);
    }

    [Test]
    public void VarIntRoundTripOnAllSizes()
    {
        for (var bits = 0; bits < 64; bits++)
        {
            foreach (var value in new[] { 1UL << bits, (1UL << bits) - 1, (1UL << bits) + 1 })
            {
                using var encoder = new BinaryEncoder();
                encoder.WriteVarUInt(value);
                Assert.AreEqual(value, new BinaryDecoder(encoder.ToArray()).ReadVarUInt(), $"value {value}");
            }
        }
    }

    [Test]
    public void VarIntAcceptsLongerEncodings()
    {
        Assert.AreEqual(5UL, new BinaryDecoder(new byte[] { 0x80, 0x05 }).ReadVarUInt());
    }

    [TestCase(0L, 0UL)]
    [TestCase(-1L, 1UL)]
    [TestCase(1L, 2UL)]
    [TestCase(-2L, 3UL)]
    [TestCase(long.MaxValue, ulong.MaxValue - 1)]
    [TestCase(long.MinValue, ulong.MaxValue)]
    public void ZigZag(long value, ulong expected)
    {
        Assert.AreEqual(expected, VarInt.ZigZagEncode(value));
        Assert.AreEqual(value, VarInt.ZigZagDecode(expected));
    }

    [Test]
    public void StringDeduplication()
    {
        using var encoder = new BinaryEncoder { DedupMaxLength = 4 };
        encoder.WriteString("ab");
        encoder.WriteString("ab");
        encoder.WriteString("toolong");
        encoder.WriteString("toolong");
        encoder.WriteString(string.Empty);
        Assert.AreEqual("04 61 62 01 0E 74 6F 6F 6C 6F 6E 67 0E 74 6F 6F 6C 6F 6E 67 00", Hex(encoder.ToArray()));

        var decoder = new BinaryDecoder(encoder.ToArray()) { DedupMaxLength = 4 };
        Assert.AreEqual("ab", decoder.ReadString());
        Assert.AreEqual("ab", decoder.ReadString());
        Assert.AreEqual("toolong", decoder.ReadString());
        Assert.AreEqual("toolong", decoder.ReadString());
        Assert.AreEqual(string.Empty, decoder.ReadString());
    }

    [Test]
    public void InvalidStringIndexIsRejected()
    {
        var decoder = new BinaryDecoder(new byte[] { 0x03 });
        Assert.Throws<SerializationException>(() => decoder.ReadString());
    }

    [Test]
    public void DecoderReadsFromSmallStreamBuffer()
    {
        using var encoder = new BinaryEncoder();
        var text = new string('x', 1000);
        for (var i = 0; i < 100; i++)
        {
            encoder.WriteVarUInt((ulong)i * 1000003);
            encoder.WriteString(text);
            encoder.WriteDouble(i * 0.5);
        }

        using var decoder = new BinaryDecoder(new MemoryStream(encoder.ToArray()), 64);
        for (var i = 0; i < 100; i++)
        {
            Assert.AreEqual((ulong)i * 1000003, decoder.ReadVarUInt());
            Assert.AreEqual(text, decoder.ReadString());
            Assert.AreEqual(i * 0.5, decoder.ReadDouble());
        }

        Assert.IsTrue(decoder.IsAtEnd);
    }

    [TestCase("")]
    [TestCase("a")]
    [TestCase("abc")]
    [TestCase("0123456789abcdefghijklmnopqrstuvwxyz")]
    [TestCase("The quick brown fox jumps over the lazy dog, again and again, to exceed sixty-four bytes of input.")]
    public void XxHash64MatchesReference(string text)
    {
        var data = System.Text.Encoding.UTF8.GetBytes(text);
        var expected = System.IO.Hashing.XxHash64.HashToUInt64(data);
        Assert.AreEqual(expected, XxHash64.Hash(data));
    }

    internal static string Hex(byte[] data) => string.Join(" ", data.Select(x => x.ToString("X2")));
}
