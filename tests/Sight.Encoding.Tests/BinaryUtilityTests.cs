using Sight.Encoding.Internal;
using System.IO;

namespace Sight.Encoding.Tests
{
    public class BinaryUtilityTests
    {
        #region ReverseEndianness

        [Test]
        public void ReverseUInt8()
        {
            Assert.AreEqual(0xA8, BinaryUtility.ReverseEndianness(0xA8));
        }

        [Test]
        public void ReverseUInt16()
        {
            Assert.AreEqual(0xA8C2u, BinaryUtility.ReverseEndianness((ushort)0xC2A8u));
        }

        [Test]
        public void ReverseUInt32()
        {
            Assert.AreEqual(0xB4F5A8C2u, BinaryUtility.ReverseEndianness((uint)0xC2A8F5B4u));
        }

        [Test]
        public void ReverseUInt64()
        {
            Assert.AreEqual(0xB45DF5A258C2B1A7u, BinaryUtility.ReverseEndianness((ulong)0xA7B1C258A2F55DB4u));
        }

        #endregion

        #region WriteToBuffer

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt8ToBuffer(bool littleEndian)
        {
            var buffer = new byte[4];
            var value = 0b0110_0010;
            BinaryUtility.WriteInt8ToBuffer((sbyte)value, buffer, littleEndian: littleEndian);

            Assert.AreEqual(0b0110_0010, buffer[0]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(0, buffer[2]);
            Assert.AreEqual(0, buffer[3]);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt8ToBuffer(bool littleEndian)
        {
            var buffer = new byte[4];
            BinaryUtility.WriteUInt8ToBuffer(0b0110_0010, buffer, littleEndian: littleEndian);

            Assert.AreEqual(0b0110_0010, buffer[0]);
            Assert.AreEqual(0, buffer[1]);
            Assert.AreEqual(0, buffer[2]);
            Assert.AreEqual(0, buffer[3]);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt16ToBuffer(bool littleEndian)
        {
            var buffer = new byte[4];
            var value = (ushort)0b1001_0110__0110_0010;
            BinaryUtility.WriteInt16ToBuffer((short)value, buffer, littleEndian: littleEndian);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[1]);
                Assert.AreEqual(0b1001_0110, buffer[0]);
            }

            Assert.AreEqual(0, buffer[2]);
            Assert.AreEqual(0, buffer[3]);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt16ToBuffer(bool littleEndian)
        {
            var buffer = new byte[4];
            BinaryUtility.WriteUInt16ToBuffer(0b1001_0110__0110_0010, buffer, littleEndian: littleEndian);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[1]);
                Assert.AreEqual(0b1001_0110, buffer[0]);
            }

            Assert.AreEqual(0, buffer[2]);
            Assert.AreEqual(0, buffer[3]);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt32ToBuffer(bool littleEndian)
        {
            var buffer = new byte[4];
            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010;
            BinaryUtility.WriteInt32ToBuffer((int)value, buffer, littleEndian: littleEndian);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
                Assert.AreEqual(0b0100_1110, buffer[2]);
                Assert.AreEqual(0b1010_0101, buffer[3]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt32ToBuffer(bool littleEndian)
        {
            var buffer = new byte[4];
            BinaryUtility.WriteUInt32ToBuffer(0b1010_0101__0100_1110__1001_0110__0110_0010, buffer, littleEndian: littleEndian);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
                Assert.AreEqual(0b0100_1110, buffer[2]);
                Assert.AreEqual(0b1010_0101, buffer[3]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt64ToBuffer(bool littleEndian)
        {
            var buffer = new byte[8];
            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101;
            BinaryUtility.WriteInt64ToBuffer((long)value, buffer, littleEndian: littleEndian);

            if (littleEndian)
            {
                Assert.AreEqual(0b1001_1101, buffer[0]);
                Assert.AreEqual(0b0110_1001, buffer[1]);
                Assert.AreEqual(0b1011_0001, buffer[2]);
                Assert.AreEqual(0b0101_1010, buffer[3]);
                Assert.AreEqual(0b0110_0010, buffer[4]);
                Assert.AreEqual(0b1001_0110, buffer[5]);
                Assert.AreEqual(0b0100_1110, buffer[6]);
                Assert.AreEqual(0b1010_0101, buffer[7]);
            }
            else
            {
                Assert.AreEqual(0b1001_1101, buffer[7]);
                Assert.AreEqual(0b0110_1001, buffer[6]);
                Assert.AreEqual(0b1011_0001, buffer[5]);
                Assert.AreEqual(0b0101_1010, buffer[4]);
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt64ToBuffer(bool littleEndian)
        {
            var buffer = new byte[8];
            BinaryUtility.WriteUInt64ToBuffer(0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101, buffer, littleEndian: littleEndian);

            if (littleEndian)
            {
                Assert.AreEqual(0b1001_1101, buffer[0]);
                Assert.AreEqual(0b0110_1001, buffer[1]);
                Assert.AreEqual(0b1011_0001, buffer[2]);
                Assert.AreEqual(0b0101_1010, buffer[3]);
                Assert.AreEqual(0b0110_0010, buffer[4]);
                Assert.AreEqual(0b1001_0110, buffer[5]);
                Assert.AreEqual(0b0100_1110, buffer[6]);
                Assert.AreEqual(0b1010_0101, buffer[7]);
            }
            else
            {
                Assert.AreEqual(0b1001_1101, buffer[7]);
                Assert.AreEqual(0b0110_1001, buffer[6]);
                Assert.AreEqual(0b1011_0001, buffer[5]);
                Assert.AreEqual(0b0101_1010, buffer[4]);
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteSingleToBuffer(bool littleEndian)
        {
            var buffer = new byte[4];
            var value = new SingleToUInt32Union { UInt32 = 0b1010_0101__0100_1110__1001_0110__0110_0010 };
            BinaryUtility.WriteSingleToBuffer(value.Single, buffer, littleEndian: littleEndian);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
                Assert.AreEqual(0b0100_1110, buffer[2]);
                Assert.AreEqual(0b1010_0101, buffer[3]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteDoubleToBuffer(bool littleEndian)
        {
            var buffer = new byte[8];
            var value = new DoubleToUInt64Union { UInt64 = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101 };
            BinaryUtility.WriteDoubleToBuffer(value.Double, buffer, littleEndian: littleEndian);

            if (littleEndian)
            {
                Assert.AreEqual(0b1001_1101, buffer[0]);
                Assert.AreEqual(0b0110_1001, buffer[1]);
                Assert.AreEqual(0b1011_0001, buffer[2]);
                Assert.AreEqual(0b0101_1010, buffer[3]);
                Assert.AreEqual(0b0110_0010, buffer[4]);
                Assert.AreEqual(0b1001_0110, buffer[5]);
                Assert.AreEqual(0b0100_1110, buffer[6]);
                Assert.AreEqual(0b1010_0101, buffer[7]);
            }
            else
            {
                Assert.AreEqual(0b1001_1101, buffer[7]);
                Assert.AreEqual(0b0110_1001, buffer[6]);
                Assert.AreEqual(0b1011_0001, buffer[5]);
                Assert.AreEqual(0b0101_1010, buffer[4]);
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        #endregion

        #region WriteToStream

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt8ToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            var value = 0b0110_0010;
            BinaryUtility.WriteInt8ToStream((sbyte)value, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(0b0110_0010, buffer[0]);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt8ToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            BinaryUtility.WriteUInt8ToStream(0b0110_0010, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(0b0110_0010, buffer[0]);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt16ToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            var value = (ushort)0b1001_0110__0110_0010;
            BinaryUtility.WriteInt16ToStream((short)value, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(2, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[1]);
                Assert.AreEqual(0b1001_0110, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt16ToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            BinaryUtility.WriteUInt16ToStream(0b1001_0110__0110_0010, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(2, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[1]);
                Assert.AreEqual(0b1001_0110, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt32ToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010;
            BinaryUtility.WriteInt32ToStream((int)value, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(4, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
                Assert.AreEqual(0b0100_1110, buffer[2]);
                Assert.AreEqual(0b1010_0101, buffer[3]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt32ToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            BinaryUtility.WriteUInt32ToStream(0b1010_0101__0100_1110__1001_0110__0110_0010, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(4, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
                Assert.AreEqual(0b0100_1110, buffer[2]);
                Assert.AreEqual(0b1010_0101, buffer[3]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt64ToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101;
            BinaryUtility.WriteInt64ToStream((long)value, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(8, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b1001_1101, buffer[0]);
                Assert.AreEqual(0b0110_1001, buffer[1]);
                Assert.AreEqual(0b1011_0001, buffer[2]);
                Assert.AreEqual(0b0101_1010, buffer[3]);
                Assert.AreEqual(0b0110_0010, buffer[4]);
                Assert.AreEqual(0b1001_0110, buffer[5]);
                Assert.AreEqual(0b0100_1110, buffer[6]);
                Assert.AreEqual(0b1010_0101, buffer[7]);
            }
            else
            {
                Assert.AreEqual(0b1001_1101, buffer[7]);
                Assert.AreEqual(0b0110_1001, buffer[6]);
                Assert.AreEqual(0b1011_0001, buffer[5]);
                Assert.AreEqual(0b0101_1010, buffer[4]);
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt64ToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            BinaryUtility.WriteUInt64ToStream(0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(8, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b1001_1101, buffer[0]);
                Assert.AreEqual(0b0110_1001, buffer[1]);
                Assert.AreEqual(0b1011_0001, buffer[2]);
                Assert.AreEqual(0b0101_1010, buffer[3]);
                Assert.AreEqual(0b0110_0010, buffer[4]);
                Assert.AreEqual(0b1001_0110, buffer[5]);
                Assert.AreEqual(0b0100_1110, buffer[6]);
                Assert.AreEqual(0b1010_0101, buffer[7]);
            }
            else
            {
                Assert.AreEqual(0b1001_1101, buffer[7]);
                Assert.AreEqual(0b0110_1001, buffer[6]);
                Assert.AreEqual(0b1011_0001, buffer[5]);
                Assert.AreEqual(0b0101_1010, buffer[4]);
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteSingleToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            var value = new SingleToUInt32Union { UInt32 = 0b1010_0101__0100_1110__1001_0110__0110_0010 };
            BinaryUtility.WriteSingleToStream(value.Single, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(4, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
                Assert.AreEqual(0b0100_1110, buffer[2]);
                Assert.AreEqual(0b1010_0101, buffer[3]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteDoubleToStream(bool littleEndian)
        {
            using var stream = new MemoryStream();
            var value = new DoubleToUInt64Union { UInt64 = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101 };
            BinaryUtility.WriteDoubleToStream(value.Double, stream, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(8, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b1001_1101, buffer[0]);
                Assert.AreEqual(0b0110_1001, buffer[1]);
                Assert.AreEqual(0b1011_0001, buffer[2]);
                Assert.AreEqual(0b0101_1010, buffer[3]);
                Assert.AreEqual(0b0110_0010, buffer[4]);
                Assert.AreEqual(0b1001_0110, buffer[5]);
                Assert.AreEqual(0b0100_1110, buffer[6]);
                Assert.AreEqual(0b1010_0101, buffer[7]);
            }
            else
            {
                Assert.AreEqual(0b1001_1101, buffer[7]);
                Assert.AreEqual(0b0110_1001, buffer[6]);
                Assert.AreEqual(0b1011_0001, buffer[5]);
                Assert.AreEqual(0b0101_1010, buffer[4]);
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        #endregion

        #region WriteToBinaryWriter

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt8ToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            var value = 0b0110_0010;
            BinaryUtility.WriteInt8ToBinaryWriter((sbyte)value, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(0b0110_0010, buffer[0]);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt8ToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            BinaryUtility.WriteUInt8ToBinaryWriter(0b0110_0010, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(0b0110_0010, buffer[0]);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt16ToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            var value = (ushort)0b1001_0110__0110_0010;
            BinaryUtility.WriteInt16ToBinaryWriter((short)value, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(2, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[1]);
                Assert.AreEqual(0b1001_0110, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt16ToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            BinaryUtility.WriteUInt16ToBinaryWriter(0b1001_0110__0110_0010, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(2, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[1]);
                Assert.AreEqual(0b1001_0110, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt32ToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010;
            BinaryUtility.WriteInt32ToBinaryWriter((int)value, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(4, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
                Assert.AreEqual(0b0100_1110, buffer[2]);
                Assert.AreEqual(0b1010_0101, buffer[3]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt32ToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            BinaryUtility.WriteUInt32ToBinaryWriter(0b1010_0101__0100_1110__1001_0110__0110_0010, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(4, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
                Assert.AreEqual(0b0100_1110, buffer[2]);
                Assert.AreEqual(0b1010_0101, buffer[3]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteInt64ToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101;
            BinaryUtility.WriteInt64ToBinaryWriter((long)value, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(8, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b1001_1101, buffer[0]);
                Assert.AreEqual(0b0110_1001, buffer[1]);
                Assert.AreEqual(0b1011_0001, buffer[2]);
                Assert.AreEqual(0b0101_1010, buffer[3]);
                Assert.AreEqual(0b0110_0010, buffer[4]);
                Assert.AreEqual(0b1001_0110, buffer[5]);
                Assert.AreEqual(0b0100_1110, buffer[6]);
                Assert.AreEqual(0b1010_0101, buffer[7]);
            }
            else
            {
                Assert.AreEqual(0b1001_1101, buffer[7]);
                Assert.AreEqual(0b0110_1001, buffer[6]);
                Assert.AreEqual(0b1011_0001, buffer[5]);
                Assert.AreEqual(0b0101_1010, buffer[4]);
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteUInt64ToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            BinaryUtility.WriteUInt64ToBinaryWriter(0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(8, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b1001_1101, buffer[0]);
                Assert.AreEqual(0b0110_1001, buffer[1]);
                Assert.AreEqual(0b1011_0001, buffer[2]);
                Assert.AreEqual(0b0101_1010, buffer[3]);
                Assert.AreEqual(0b0110_0010, buffer[4]);
                Assert.AreEqual(0b1001_0110, buffer[5]);
                Assert.AreEqual(0b0100_1110, buffer[6]);
                Assert.AreEqual(0b1010_0101, buffer[7]);
            }
            else
            {
                Assert.AreEqual(0b1001_1101, buffer[7]);
                Assert.AreEqual(0b0110_1001, buffer[6]);
                Assert.AreEqual(0b1011_0001, buffer[5]);
                Assert.AreEqual(0b0101_1010, buffer[4]);
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteSingleToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            var value = new SingleToUInt32Union { UInt32 = 0b1010_0101__0100_1110__1001_0110__0110_0010 };
            BinaryUtility.WriteSingleToBinaryWriter(value.Single, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(4, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b0110_0010, buffer[0]);
                Assert.AreEqual(0b1001_0110, buffer[1]);
                Assert.AreEqual(0b0100_1110, buffer[2]);
                Assert.AreEqual(0b1010_0101, buffer[3]);
            }
            else
            {
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WriteDoubleToBinaryWriter(bool littleEndian)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            var value = new DoubleToUInt64Union { UInt64 = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101 };
            BinaryUtility.WriteDoubleToBinaryWriter(value.Double, writer, littleEndian: littleEndian);

            var buffer = stream.ToArray();
            Assert.AreEqual(8, buffer.Length);

            if (littleEndian)
            {
                Assert.AreEqual(0b1001_1101, buffer[0]);
                Assert.AreEqual(0b0110_1001, buffer[1]);
                Assert.AreEqual(0b1011_0001, buffer[2]);
                Assert.AreEqual(0b0101_1010, buffer[3]);
                Assert.AreEqual(0b0110_0010, buffer[4]);
                Assert.AreEqual(0b1001_0110, buffer[5]);
                Assert.AreEqual(0b0100_1110, buffer[6]);
                Assert.AreEqual(0b1010_0101, buffer[7]);
            }
            else
            {
                Assert.AreEqual(0b1001_1101, buffer[7]);
                Assert.AreEqual(0b0110_1001, buffer[6]);
                Assert.AreEqual(0b1011_0001, buffer[5]);
                Assert.AreEqual(0b0101_1010, buffer[4]);
                Assert.AreEqual(0b0110_0010, buffer[3]);
                Assert.AreEqual(0b1001_0110, buffer[2]);
                Assert.AreEqual(0b0100_1110, buffer[1]);
                Assert.AreEqual(0b1010_0101, buffer[0]);
            }
        }

        #endregion

        #region ReadFromBuffer

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt8FromBuffer(bool littleEndian)
        {
            var buffer = new byte[]
            {
                0b0110_0010,
                0,
                0,
                0
            };

            var value = (byte)0b0110_0010;
            Assert.AreEqual((sbyte)value, BinaryUtility.ReadInt8FromBuffer(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt8FromBuffer(bool littleEndian)
        {
            var buffer = new byte[]
            {
                0b0110_0010,
                0,
                0,
                0
            };

            Assert.AreEqual(0b0110_0010, BinaryUtility.ReadUInt8FromBuffer(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt16FromBuffer(bool littleEndian)
        {
            var buffer = littleEndian
                ? new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0,
                    0
                }
                : new byte[]
                {
                    0b1001_0110,
                    0b0110_0010,
                    0,
                    0
                };

            var value = (ushort)0b1001_0110__0110_0010;
            Assert.AreEqual((short)value, BinaryUtility.ReadInt16FromBuffer(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt16FromBuffer(bool littleEndian)
        {
            var buffer = littleEndian
                ? new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0,
                    0
                }
                : new byte[]
                {
                    0b1001_0110,
                    0b0110_0010,
                    0,
                    0
                };

            Assert.AreEqual((ushort)0b1001_0110__0110_0010, BinaryUtility.ReadUInt16FromBuffer(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt32FromBuffer(bool littleEndian)
        {
            var buffer = littleEndian
                ? new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                }
                : new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010
                };

            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010;
            Assert.AreEqual((int)value, BinaryUtility.ReadInt32FromBuffer(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt32FromBuffer(bool littleEndian)
        {
            var buffer = littleEndian
                ? new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                }
                : new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010
                };

            Assert.AreEqual((uint)0b1010_0101__0100_1110__1001_0110__0110_0010, BinaryUtility.ReadUInt32FromBuffer(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt64FromBuffer(bool littleEndian)
        {
            var buffer = littleEndian
                ? new byte[]
                {
                    0b1001_1101,
                    0b0110_1001,
                    0b1011_0001,
                    0b0101_1010,
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                }
                : new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010,
                    0b0101_1010,
                    0b1011_0001,
                    0b0110_1001,
                    0b1001_1101
                };

            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101;
            Assert.AreEqual((long)value, BinaryUtility.ReadInt64FromBuffer(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt64FromBuffer(bool littleEndian)
        {
            var buffer = littleEndian
                ? new byte[]
                {
                    0b1001_1101,
                    0b0110_1001,
                    0b1011_0001,
                    0b0101_1010,
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                }
                : new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010,
                    0b0101_1010,
                    0b1011_0001,
                    0b0110_1001,
                    0b1001_1101
                };

            Assert.AreEqual((ulong)0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101, BinaryUtility.ReadUInt64FromBuffer(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadSingleFromBuffer(bool littleEndian)
        {
            var buffer = littleEndian
                ? new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                }
                : new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010
                };

            var value = new SingleToUInt32Union { UInt32 = 0b1010_0101__0100_1110__1001_0110__0110_0010 };
            Assert.AreEqual(value.Single, BinaryUtility.ReadSingleFromBuffer(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadDoubleFromBuffer(bool littleEndian)
        {
            var buffer = littleEndian
                ? new byte[]
                {
                    0b1001_1101,
                    0b0110_1001,
                    0b1011_0001,
                    0b0101_1010,
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                }
                : new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010,
                    0b0101_1010,
                    0b1011_0001,
                    0b0110_1001,
                    0b1001_1101
                };

            var union = new DoubleToUInt64Union { UInt64 = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101 };
            Assert.AreEqual(union.Double, BinaryUtility.ReadDoubleFromBuffer(buffer, littleEndian: littleEndian));
        }

        #endregion

        #region ReadFromStream

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt8FromStream(bool littleEndian)
        {
            var buffer = new MemoryStream(new byte[]
            {
                0b0110_0010,
                0,
                0,
                0
            });

            var value = (byte)0b0110_0010;
            Assert.AreEqual((sbyte)value, BinaryUtility.ReadInt8FromStream(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt8FromStream(bool littleEndian)
        {
            var buffer = new MemoryStream(new byte[]
            {
                0b0110_0010,
                0,
                0,
                0
            });

            Assert.AreEqual(0b0110_0010, BinaryUtility.ReadUInt8FromStream(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt16FromStream(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0,
                    0
                })
                : new MemoryStream(new byte[]
                {
                    0b1001_0110,
                    0b0110_0010,
                    0,
                    0
                });

            var value = (ushort)0b1001_0110__0110_0010;
            Assert.AreEqual((short)value, BinaryUtility.ReadInt16FromStream(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt16FromStream(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0,
                    0
                })
                : new MemoryStream(new byte[]
                {
                    0b1001_0110,
                    0b0110_0010,
                    0,
                    0
                });

            Assert.AreEqual((ushort)0b1001_0110__0110_0010, BinaryUtility.ReadUInt16FromStream(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt32FromStream(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010
                });

            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010;
            Assert.AreEqual((int)value, BinaryUtility.ReadInt32FromStream(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt32FromStream(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010
                });

            Assert.AreEqual((uint)0b1010_0101__0100_1110__1001_0110__0110_0010, BinaryUtility.ReadUInt32FromStream(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt64FromStream(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b1001_1101,
                    0b0110_1001,
                    0b1011_0001,
                    0b0101_1010,
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010,
                    0b0101_1010,
                    0b1011_0001,
                    0b0110_1001,
                    0b1001_1101
                });

            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101;
            Assert.AreEqual((long)value, BinaryUtility.ReadInt64FromStream(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt64FromStream(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b1001_1101,
                    0b0110_1001,
                    0b1011_0001,
                    0b0101_1010,
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010,
                    0b0101_1010,
                    0b1011_0001,
                    0b0110_1001,
                    0b1001_1101
                });

            Assert.AreEqual((ulong)0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101, BinaryUtility.ReadUInt64FromStream(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadSingleFromStream(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010
                });

            var value = new SingleToUInt32Union { UInt32 = 0b1010_0101__0100_1110__1001_0110__0110_0010 };
            Assert.AreEqual(value.Single, BinaryUtility.ReadSingleFromStream(buffer, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadDoubleFromStream(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b1001_1101,
                    0b0110_1001,
                    0b1011_0001,
                    0b0101_1010,
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010,
                    0b0101_1010,
                    0b1011_0001,
                    0b0110_1001,
                    0b1001_1101
                });

            var union = new DoubleToUInt64Union { UInt64 = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101 };
            Assert.AreEqual(union.Double, BinaryUtility.ReadDoubleFromStream(buffer, littleEndian: littleEndian));
        }

        #endregion

        #region ReadFromBinaryReader

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt8FromBinaryReader(bool littleEndian)
        {
            var buffer = new MemoryStream(new byte[]
            {
                0b0110_0010,
                0,
                0,
                0
            });
            using var reader = new BinaryReader(buffer);

            var value = (byte)0b0110_0010;
            Assert.AreEqual((sbyte)value, BinaryUtility.ReadInt8FromBinaryReader(reader, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt8FromBinaryReader(bool littleEndian)
        {
            var buffer = new MemoryStream(new byte[]
            {
                0b0110_0010,
                0,
                0,
                0
            });
            using var reader = new BinaryReader(buffer);

            Assert.AreEqual(0b0110_0010, BinaryUtility.ReadUInt8FromBinaryReader(reader, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt16FromBinaryReader(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0,
                    0
                })
                : new MemoryStream(new byte[]
                {
                    0b1001_0110,
                    0b0110_0010,
                    0,
                    0
                });
            using var reader = new BinaryReader(buffer);

            var value = (ushort)0b1001_0110__0110_0010;
            Assert.AreEqual((short)value, BinaryUtility.ReadInt16FromBinaryReader(reader, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt16FromBinaryReader(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0,
                    0
                })
                : new MemoryStream(new byte[]
                {
                    0b1001_0110,
                    0b0110_0010,
                    0,
                    0
                });
            using var reader = new BinaryReader(buffer);

            Assert.AreEqual((ushort)0b1001_0110__0110_0010, BinaryUtility.ReadUInt16FromBinaryReader(reader, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt32FromBinaryReader(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010
                });
            using var reader = new BinaryReader(buffer);

            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010;
            Assert.AreEqual((int)value, BinaryUtility.ReadInt32FromBinaryReader(reader, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt32FromBinaryReader(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010
                });
            using var reader = new BinaryReader(buffer);

            Assert.AreEqual((uint)0b1010_0101__0100_1110__1001_0110__0110_0010, BinaryUtility.ReadUInt32FromBinaryReader(reader, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadInt64FromBinaryReader(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b1001_1101,
                    0b0110_1001,
                    0b1011_0001,
                    0b0101_1010,
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010,
                    0b0101_1010,
                    0b1011_0001,
                    0b0110_1001,
                    0b1001_1101
                });
            using var reader = new BinaryReader(buffer);

            var value = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101;
            Assert.AreEqual((long)value, BinaryUtility.ReadInt64FromBinaryReader(reader, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadUInt64FromBinaryReader(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b1001_1101,
                    0b0110_1001,
                    0b1011_0001,
                    0b0101_1010,
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010,
                    0b0101_1010,
                    0b1011_0001,
                    0b0110_1001,
                    0b1001_1101
                });
            using var reader = new BinaryReader(buffer);

            Assert.AreEqual((ulong)0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101, BinaryUtility.ReadUInt64FromBinaryReader(reader, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadSingleFromBinaryReader(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010
                });
            using var reader = new BinaryReader(buffer);

            var value = new SingleToUInt32Union { UInt32 = 0b1010_0101__0100_1110__1001_0110__0110_0010 };
            Assert.AreEqual(value.Single, BinaryUtility.ReadSingleFromBinaryReader(reader, littleEndian: littleEndian));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReadDoubleFromBinaryReader(bool littleEndian)
        {
            var buffer = littleEndian
                ? new MemoryStream(new byte[]
                {
                    0b1001_1101,
                    0b0110_1001,
                    0b1011_0001,
                    0b0101_1010,
                    0b0110_0010,
                    0b1001_0110,
                    0b0100_1110,
                    0b1010_0101
                })
                : new MemoryStream(new byte[]
                {
                    0b1010_0101,
                    0b0100_1110,
                    0b1001_0110,
                    0b0110_0010,
                    0b0101_1010,
                    0b1011_0001,
                    0b0110_1001,
                    0b1001_1101
                });
            using var reader = new BinaryReader(buffer);

            var union = new DoubleToUInt64Union { UInt64 = 0b1010_0101__0100_1110__1001_0110__0110_0010__0101_1010__1011_0001__0110_1001__1001_1101 };
            Assert.AreEqual(union.Double, BinaryUtility.ReadDoubleFromBinaryReader(reader, littleEndian: littleEndian));
        }

        #endregion
    }
}
