using System.Runtime.InteropServices;

namespace Sight.Encoding.Internal
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct DoubleToUInt64Union
    {
        [FieldOffset(0)] public double Double;
        [FieldOffset(0)] public ulong UInt64;
    }
}
