namespace Sight.Encoding;

/// <summary>
/// ZigZag transform, maps signed integers to unsigned ones so that small absolute values stay small
/// (0 → 0, -1 → 1, 1 → 2, -2 → 3, ...)
/// </summary>
public static class ZigZag
{
    /// <summary>
    /// Encode a signed 32-bit value
    /// </summary>
    public static uint Encode(int value) => (uint)((value << 1) ^ (value >> 31));

    /// <summary>
    /// Encode a signed 64-bit value
    /// </summary>
    public static ulong Encode(long value) => (ulong)((value << 1) ^ (value >> 63));

    /// <summary>
    /// Decode a signed 32-bit value
    /// </summary>
    public static int Decode(uint value) => (int)(value >> 1) ^ -(int)(value & 1);

    /// <summary>
    /// Decode a signed 64-bit value
    /// </summary>
    public static long Decode(ulong value) => (long)(value >> 1) ^ -(long)(value & 1);
}
