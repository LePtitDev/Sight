namespace Sight.Serialization.Schemas;

/// <summary>
/// Kind of a type (values of <c>core.Kind</c>)
/// </summary>
public enum TypeKind : byte
{
    /// <summary>
    /// Null value (only in <c>any</c>)
    /// </summary>
    Null = 0,

    /// <summary>
    /// Boolean
    /// </summary>
    Bool = 1,

    /// <summary>
    /// Unsigned 8-bit integer
    /// </summary>
    U8 = 2,

    /// <summary>
    /// Unsigned 16-bit integer
    /// </summary>
    U16 = 3,

    /// <summary>
    /// Unsigned 32-bit integer
    /// </summary>
    U32 = 4,

    /// <summary>
    /// Unsigned 64-bit integer
    /// </summary>
    U64 = 5,

    /// <summary>
    /// Signed 8-bit integer
    /// </summary>
    S8 = 6,

    /// <summary>
    /// Signed 16-bit integer
    /// </summary>
    S16 = 7,

    /// <summary>
    /// Signed 32-bit integer
    /// </summary>
    S32 = 8,

    /// <summary>
    /// Signed 64-bit integer
    /// </summary>
    S64 = 9,

    /// <summary>
    /// 32-bit floating point
    /// </summary>
    F32 = 10,

    /// <summary>
    /// 64-bit floating point
    /// </summary>
    F64 = 11,

    /// <summary>
    /// Unicode string
    /// </summary>
    String = 12,

    /// <summary>
    /// Enum declared in a schema
    /// </summary>
    Enum = 13,

    /// <summary>
    /// List of values
    /// </summary>
    List = 14,

    /// <summary>
    /// Map of unique keys to values
    /// </summary>
    Map = 15,

    /// <summary>
    /// Struct declared in a schema
    /// </summary>
    Struct = 16,

    /// <summary>
    /// Internal reference to a struct
    /// </summary>
    Ref = 17,

    /// <summary>
    /// External reference to a struct of another file
    /// </summary>
    ExtRef = 18,

    /// <summary>
    /// Dynamic value carrying its type
    /// </summary>
    Any = 19,
}

/// <summary>
/// Helpers on <see cref="TypeKind"/>
/// </summary>
public static class TypeKindExtensions
{
    /// <summary>
    /// Indicates whether the kind is an integer type
    /// </summary>
    public static bool IsInteger(this TypeKind kind) => kind is >= TypeKind.U8 and <= TypeKind.S64;

    /// <summary>
    /// Indicates whether the kind is a signed integer type
    /// </summary>
    public static bool IsSignedInteger(this TypeKind kind) => kind is >= TypeKind.S8 and <= TypeKind.S64;

    /// <summary>
    /// Indicates whether the kind is an unsigned integer type
    /// </summary>
    public static bool IsUnsignedInteger(this TypeKind kind) => kind is >= TypeKind.U8 and <= TypeKind.U64;

    /// <summary>
    /// Indicates whether the kind is a floating point type
    /// </summary>
    public static bool IsFloat(this TypeKind kind) => kind is TypeKind.F32 or TypeKind.F64;

    /// <summary>
    /// Indicates whether the kind is a scalar type (bool, number or string)
    /// </summary>
    public static bool IsScalar(this TypeKind kind) => kind is >= TypeKind.Bool and <= TypeKind.String;

    /// <summary>
    /// Indicates whether the fixed encoding is allowed for this kind
    /// </summary>
    public static bool AllowsFixed(this TypeKind kind) => kind is TypeKind.U16 or TypeKind.U32 or TypeKind.U64 or TypeKind.S16 or TypeKind.S32 or TypeKind.S64;

    /// <summary>
    /// Size in bits of an integer kind
    /// </summary>
    public static int IntegerBits(this TypeKind kind) => kind switch
    {
        TypeKind.U8 or TypeKind.S8 => 8,
        TypeKind.U16 or TypeKind.S16 => 16,
        TypeKind.U32 or TypeKind.S32 => 32,
        TypeKind.U64 or TypeKind.S64 => 64,
        _ => 0,
    };

    /// <summary>
    /// Name of the kind in the schema language
    /// </summary>
    public static string ToKeyword(this TypeKind kind) => kind switch
    {
        TypeKind.Null => "null",
        TypeKind.Bool => "bool",
        TypeKind.U8 => "u8",
        TypeKind.U16 => "u16",
        TypeKind.U32 => "u32",
        TypeKind.U64 => "u64",
        TypeKind.S8 => "s8",
        TypeKind.S16 => "s16",
        TypeKind.S32 => "s32",
        TypeKind.S64 => "s64",
        TypeKind.F32 => "f32",
        TypeKind.F64 => "f64",
        TypeKind.String => "string",
        TypeKind.List => "list",
        TypeKind.Map => "map",
        TypeKind.Ref => "ref",
        TypeKind.ExtRef => "extref",
        TypeKind.Any => "any",
        _ => kind.ToString(),
    };
}
