using Sight.Encoding.Internal;

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
    }
}
