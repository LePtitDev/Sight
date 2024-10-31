using System.Runtime.InteropServices;

namespace Sight.Encoding.Internal
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct SingleToUInt32Union
    {
        [FieldOffset(0)] public float Single;
        [FieldOffset(0)] public uint UInt32;
    }
}
