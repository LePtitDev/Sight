namespace Sight.Encoding.Tests
{
    public class BinaryUtilityTests
    {
        [Test]
        public void ReverseByte()
        {
            Assert.AreEqual(0xA8, BinaryUtility.ReverseEndianness(0xA8));
        }

        [Test]
        public void ReverseUShort()
        {
            Assert.AreEqual(0xA8C2u, BinaryUtility.ReverseEndianness((ushort)0xC2A8u));
        }

        [Test]
        public void ReverseUInt()
        {
            Assert.AreEqual(0xB4F5A8C2u, BinaryUtility.ReverseEndianness((uint)0xC2A8F5B4u));
        }

        [Test]
        public void ReverseULong()
        {
            Assert.AreEqual(0xB45DF5A258C2B1A7u, BinaryUtility.ReverseEndianness((ulong)0xA7B1C258A2F55DB4u));
        }

        [Test]
        public void WriteByteToBuffer()
        {
            var buffer = new byte[4];
            BinaryUtility.WriteByteToBuffer(0b0110_0010, buffer);

            Assert.AreEqual(0b0110_0010, buffer[0]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(0, buffer[2]);
            Assert.AreEqual(0, buffer[3]);
        }

        [Test]
        public void WriteShortToBuffer()
        {
            var buffer = new byte[4];
            var value = (ushort)0b1001_0110__0110_0010;
            BinaryUtility.WriteShortToBuffer((short)value, buffer);

            Assert.AreEqual(0b0110_0010, buffer[0]);
            Assert.AreEqual(0b1001_0110, buffer[1]);
            Assert.AreEqual(0, buffer[2]);
            Assert.AreEqual(0, buffer[3]);
        }

        [Test]
        public void WriteUShortToBuffer()
        {
            var buffer = new byte[4];
            BinaryUtility.WriteUShortToBuffer(0b1001_0110__0110_0010, buffer);

            Assert.AreEqual(0b0110_0010, buffer[0]);
            Assert.AreEqual(0b1001_0110, buffer[1]);
            Assert.AreEqual(0, buffer[2]);
            Assert.AreEqual(0, buffer[3]);
        }

        [Test]
        public void WriteIntToBuffer()
        {
            var buffer = new byte[4];
            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010;
            BinaryUtility.WriteIntToBuffer((int)value, buffer);

            Assert.AreEqual(0b0110_0010, buffer[0]);
            Assert.AreEqual(0b1001_0110, buffer[1]);
            Assert.AreEqual(0b0100_1110, buffer[2]);
            Assert.AreEqual(0b1010_0101, buffer[3]);
        }

        [Test]
        public void WriteUIntToBuffer()
        {
            var buffer = new byte[4];
            BinaryUtility.WriteUIntToBuffer(0b1010_0101__0100_1110__1001_0110__0110_0010, buffer);

            Assert.AreEqual(0b0110_0010, buffer[0]);
            Assert.AreEqual(0b1001_0110, buffer[1]);
            Assert.AreEqual(0b0100_1110, buffer[2]);
            Assert.AreEqual(0b1010_0101, buffer[3]);
        }

        [Test]
        public void WriteLongToBuffer()
        {
            var buffer = new byte[8];
            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101;
            BinaryUtility.WriteLongToBuffer((long)value, buffer);

            Assert.AreEqual(0b1001_1101, buffer[0]);
            Assert.AreEqual(0b0110_1001, buffer[1]);
            Assert.AreEqual(0b1011_0001, buffer[2]);
            Assert.AreEqual(0b0101_1010, buffer[3]);
            Assert.AreEqual(0b0110_0010, buffer[4]);
            Assert.AreEqual(0b1001_0110, buffer[5]);
            Assert.AreEqual(0b0100_1110, buffer[6]);
            Assert.AreEqual(0b1010_0101, buffer[7]);
        }

        [Test]
        public void WriteULongToBuffer()
        {
            var buffer = new byte[8];
            BinaryUtility.WriteULongToBuffer(0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101, buffer);

            Assert.AreEqual(0b1001_1101, buffer[0]);
            Assert.AreEqual(0b0110_1001, buffer[1]);
            Assert.AreEqual(0b1011_0001, buffer[2]);
            Assert.AreEqual(0b0101_1010, buffer[3]);
            Assert.AreEqual(0b0110_0010, buffer[4]);
            Assert.AreEqual(0b1001_0110, buffer[5]);
            Assert.AreEqual(0b0100_1110, buffer[6]);
            Assert.AreEqual(0b1010_0101, buffer[7]);
        }
    }
}
