using Sight.Serialization.Schemas;

namespace Sight.Serialization.Data;

internal static class DataRanges
{
    public static bool Fits(TypeKind kind, long value) => kind switch
    {
        TypeKind.S8 => value is >= sbyte.MinValue and <= sbyte.MaxValue,
        TypeKind.S16 => value is >= short.MinValue and <= short.MaxValue,
        TypeKind.S32 => value is >= int.MinValue and <= int.MaxValue,
        TypeKind.S64 => true,
        TypeKind.U8 or TypeKind.U16 or TypeKind.U32 or TypeKind.U64 => value >= 0 && Fits(kind, (ulong)value),
        _ => false,
    };

    public static bool Fits(TypeKind kind, ulong value) => kind switch
    {
        TypeKind.U8 => value <= byte.MaxValue,
        TypeKind.U16 => value <= ushort.MaxValue,
        TypeKind.U32 => value <= uint.MaxValue,
        TypeKind.U64 => true,
        TypeKind.S8 or TypeKind.S16 or TypeKind.S32 or TypeKind.S64 => value <= long.MaxValue && Fits(kind, (long)value),
        _ => false,
    };
}
