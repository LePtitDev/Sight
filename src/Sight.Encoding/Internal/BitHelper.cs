namespace Sight.Encoding.Internal;

internal static class BitHelper
{
    public static uint SingleToUInt32Bits(float value)
    {
#if NET8_0_OR_GREATER
        return System.BitConverter.SingleToUInt32Bits(value);
#else
        return new SingleToUInt32Union { Single = value }.UInt32;
#endif
    }

    public static float UInt32BitsToSingle(uint value)
    {
#if NET8_0_OR_GREATER
        return System.BitConverter.UInt32BitsToSingle(value);
#else
        return new SingleToUInt32Union { UInt32 = value }.Single;
#endif
    }
}
