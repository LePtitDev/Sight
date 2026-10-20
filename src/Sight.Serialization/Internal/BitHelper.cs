using System.Runtime.InteropServices;
#if NET
using System.Numerics;
#endif

namespace Sight.Serialization.Internal;

internal static class BitHelper
{
#if !NET
    private static readonly byte[] s_leadingOnes = BuildLeadingOnes();

    private static byte[] BuildLeadingOnes()
    {
        var table = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            var count = 0;
            while (count < 8 && (i & (0x80 >> count)) != 0)
                count++;

            table[i] = (byte)count;
        }

        return table;
    }
#endif

    public static int LeadingZeroCount(ulong value)
    {
#if NET
        return BitOperations.LeadingZeroCount(value);
#else
        if (value == 0)
            return 64;

        var count = 0;
        while ((value & 0x8000_0000_0000_0000UL) == 0)
        {
            value <<= 1;
            count++;
        }

        return count;
#endif
    }

    public static int LeadingOneCount(byte value)
    {
#if NET
        return BitOperations.LeadingZeroCount((uint)(byte)~value) - 24;
#else
        return s_leadingOnes[value];
#endif
    }

    public static int SingleToInt32Bits(float value)
    {
#if NET
        return System.BitConverter.SingleToInt32Bits(value);
#else
        return new SingleUnion { Single = value }.Int32;
#endif
    }

    public static float Int32BitsToSingle(int value)
    {
#if NET
        return System.BitConverter.Int32BitsToSingle(value);
#else
        return new SingleUnion { Int32 = value }.Single;
#endif
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct SingleUnion
    {
        [FieldOffset(0)]
        public float Single;

        [FieldOffset(0)]
        public int Int32;
    }
}
