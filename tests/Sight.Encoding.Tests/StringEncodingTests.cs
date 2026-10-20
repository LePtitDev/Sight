using System.IO;

namespace Sight.Encoding.Tests;

public class StringEncodingTests
{
    [TestCase("")]
    [TestCase("Hello")]
    [TestCase("Ünïcödé ✓ 🚀")]
    public void RoundTrip(string value)
    {
        var buffer = new byte[BinaryEncoding.GetStringSize(value)];
        Assert.AreEqual(buffer.Length, BinaryEncoding.WriteString(buffer, value));
        Assert.AreEqual(value, BinaryEncoding.ReadString(buffer, out var bytesRead));
        Assert.AreEqual(buffer.Length, bytesRead);
    }

    [Test]
    public void LengthPrefixIsVarInt()
    {
        var value = new string('a', 200);
        var buffer = new byte[BinaryEncoding.GetStringSize(value)];
        BinaryEncoding.WriteString(buffer, value);
        Assert.AreEqual(202, buffer.Length);
        Assert.AreEqual(new byte[] { 0xC8, 0x01, (byte)'a' }, buffer.AsSpan(0, 3).ToArray());
    }

    [TestCase("")]
    [TestCase("Ünïcödé ✓ 🚀")]
    public void CompatibleWithBinaryWriter(string value)
    {
        var stream = new MemoryStream();
        new BinaryWriter(stream, System.Text.Encoding.UTF8).Write(value);
        var expected = stream.ToArray();

        var buffer = new byte[BinaryEncoding.GetStringSize(value)];
        BinaryEncoding.WriteString(buffer, value);
        Assert.AreEqual(expected, buffer);

        stream.Position = 0;
        Assert.AreEqual(value, new BinaryReader(stream, System.Text.Encoding.UTF8).ReadString());
    }

    [Test]
    public void TruncatedValue()
    {
        var buffer = new byte[BinaryEncoding.GetStringSize("Hello")];
        BinaryEncoding.WriteString(buffer, "Hello");

        Assert.IsFalse(BinaryEncoding.TryReadString(buffer.AsSpan(0, buffer.Length - 1), out var value, out var bytesRead));
        Assert.IsNull(value);
        Assert.AreEqual(0, bytesRead);
        Assert.IsFalse(BinaryEncoding.TryReadString(Array.Empty<byte>(), out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.ReadString(buffer.AsSpan(0, 3), out _));
    }

    [Test]
    public void WriteTooSmallBufferWritesNothing()
    {
        var buffer = new byte[5];
        Assert.IsFalse(BinaryEncoding.TryWriteString(buffer, "Hello", out var bytesWritten));
        Assert.AreEqual(0, bytesWritten);
        Assert.Throws<ArgumentOutOfRangeException>(() => BinaryEncoding.WriteString(buffer, "Hello"));
        Assert.AreEqual(new byte[5], buffer);
    }

    [Test]
    public void WriteNull()
    {
        Assert.Throws<ArgumentNullException>(() => BinaryEncoding.WriteString(new byte[8], null!));
    }
}
