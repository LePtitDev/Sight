using System.IO;

namespace Sight.Tokenize.Tests
{
    public class EncodingTests
    {
        [TestCase(new int[0])]
        [TestCase(new[] { 0x53, 0x49, 0x47, 0x48, 0x54 })]
        [TestCase(new[] { 0xE9, 0xED, 0x01FA, 0x250D })]
        public async Task Test_utf8_document_reading(int[] unicodeChars)
        {
            var text = string.Join(string.Empty, unicodeChars.Select(char.ConvertFromUtf32));
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

            var index = 0;
            while (await stream.GetNextUtf8Async() is { Eof: false } read)
            {
                if (index >= unicodeChars.Length)
                    Assert.Fail("Character count is invalid");

                if (unicodeChars[index] != read.Utf32)
                    Assert.Fail($"Character at index {index} is invalid (expected: {unicodeChars[index]}, result: {read.Utf32})");

                index++;
            }

            if (index < unicodeChars.Length)
                Assert.Fail($"All characters not read (index: {index})");

            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
