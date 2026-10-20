using System;
using System.Text;

namespace Sight.Serialization.Schemas;

/// <summary>
/// Reference to a type, as used by fields, list elements, map keys and values
/// </summary>
public sealed class TypeReference : IEquatable<TypeReference>
{
    private TypeReference(TypeKind kind, bool isNullable, bool isFixed, TypeDefinition? definition, TypeReference? first, TypeReference? second)
    {
        Kind = kind;
        IsNullable = isNullable;
        IsFixed = isFixed;
        Definition = definition;
        First = first;
        Second = second;
    }

    /// <summary>
    /// <c>bool</c> type
    /// </summary>
    public static TypeReference Bool { get; } = Primitive(TypeKind.Bool);

    /// <summary>
    /// <c>u8</c> type
    /// </summary>
    public static TypeReference U8 { get; } = Primitive(TypeKind.U8);

    /// <summary>
    /// <c>u16</c> type
    /// </summary>
    public static TypeReference U16 { get; } = Primitive(TypeKind.U16);

    /// <summary>
    /// <c>u32</c> type
    /// </summary>
    public static TypeReference U32 { get; } = Primitive(TypeKind.U32);

    /// <summary>
    /// <c>u64</c> type
    /// </summary>
    public static TypeReference U64 { get; } = Primitive(TypeKind.U64);

    /// <summary>
    /// <c>s8</c> type
    /// </summary>
    public static TypeReference S8 { get; } = Primitive(TypeKind.S8);

    /// <summary>
    /// <c>s16</c> type
    /// </summary>
    public static TypeReference S16 { get; } = Primitive(TypeKind.S16);

    /// <summary>
    /// <c>s32</c> type
    /// </summary>
    public static TypeReference S32 { get; } = Primitive(TypeKind.S32);

    /// <summary>
    /// <c>s64</c> type
    /// </summary>
    public static TypeReference S64 { get; } = Primitive(TypeKind.S64);

    /// <summary>
    /// <c>f32</c> type
    /// </summary>
    public static TypeReference F32 { get; } = Primitive(TypeKind.F32);

    /// <summary>
    /// <c>f64</c> type
    /// </summary>
    public static TypeReference F64 { get; } = Primitive(TypeKind.F64);

    /// <summary>
    /// <c>string</c> type
    /// </summary>
    public static TypeReference String { get; } = Primitive(TypeKind.String);

    /// <summary>
    /// <c>any</c> type
    /// </summary>
    public static TypeReference Any { get; } = Primitive(TypeKind.Any);

    /// <summary>
    /// Null type (only used to describe <c>null</c> values in <c>any</c>)
    /// </summary>
    public static TypeReference Null { get; } = new(TypeKind.Null, false, false, null, null, null);

    /// <summary>
    /// Kind of the type
    /// </summary>
    public TypeKind Kind { get; }

    /// <summary>
    /// Indicates whether the type accepts <c>null</c>
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Indicates whether the integer uses the fixed-width encoding
    /// </summary>
    public bool IsFixed { get; }

    /// <summary>
    /// Enum or struct definition (for <see cref="TypeKind.Enum"/>, <see cref="TypeKind.Struct"/>,
    /// <see cref="TypeKind.Ref"/> and <see cref="TypeKind.ExtRef"/>)
    /// </summary>
    public TypeDefinition? Definition { get; }

    /// <summary>
    /// Struct definition, if the type refers to one
    /// </summary>
    public StructDefinition? StructDefinition => Definition as StructDefinition;

    /// <summary>
    /// Enum definition, if the type is an enum
    /// </summary>
    public EnumDefinition? EnumDefinition => Definition as EnumDefinition;

    /// <summary>
    /// Element type of a list
    /// </summary>
    public TypeReference? ElementType => Kind == TypeKind.List ? First : null;

    /// <summary>
    /// Key type of a map
    /// </summary>
    public TypeReference? KeyType => Kind == TypeKind.Map ? First : null;

    /// <summary>
    /// Value type of a map
    /// </summary>
    public TypeReference? ValueType => Kind == TypeKind.Map ? Second : null;

    internal TypeReference? First { get; }

    internal TypeReference? Second { get; }

    /// <summary>
    /// Create a primitive type reference (bool, numbers, string or any)
    /// </summary>
    public static TypeReference Primitive(TypeKind kind)
    {
        if (!kind.IsScalar() && kind != TypeKind.Any)
            throw new ArgumentException($"{kind} is not a primitive kind", nameof(kind));

        return new TypeReference(kind, false, false, null, null, null);
    }

    /// <summary>
    /// Create a reference to an enum or a struct
    /// </summary>
    public static TypeReference Of(TypeDefinition definition)
    {
        var kind = definition is EnumDefinition ? TypeKind.Enum : TypeKind.Struct;
        return new TypeReference(kind, false, false, definition, null, null);
    }

    /// <summary>
    /// Create a <c>list&lt;T&gt;</c> type
    /// </summary>
    public static TypeReference List(TypeReference elementType)
    {
        return new TypeReference(TypeKind.List, false, false, null, elementType, null);
    }

    /// <summary>
    /// Create a <c>map&lt;K, V&gt;</c> type
    /// </summary>
    public static TypeReference Map(TypeReference keyType, TypeReference valueType)
    {
        return new TypeReference(TypeKind.Map, false, false, null, keyType, valueType);
    }

    /// <summary>
    /// Create a <c>ref&lt;T&gt;</c> type
    /// </summary>
    public static TypeReference Ref(StructDefinition target)
    {
        return new TypeReference(TypeKind.Ref, false, false, target, null, null);
    }

    /// <summary>
    /// Create an <c>extref&lt;T&gt;</c> type
    /// </summary>
    public static TypeReference ExtRef(StructDefinition target)
    {
        return new TypeReference(TypeKind.ExtRef, false, false, target, null, null);
    }

    /// <summary>
    /// Get the nullable version of this type
    /// </summary>
    public TypeReference AsNullable() => IsNullable ? this : new TypeReference(Kind, true, IsFixed, Definition, First, Second);

    /// <summary>
    /// Get the non-nullable version of this type
    /// </summary>
    public TypeReference AsNonNullable() => !IsNullable ? this : new TypeReference(Kind, false, IsFixed, Definition, First, Second);

    /// <summary>
    /// Get the fixed-width version of this integer type
    /// </summary>
    public TypeReference AsFixed()
    {
        if (!Kind.AllowsFixed())
            throw new InvalidOperationException($"Fixed encoding is not allowed on {Kind}");

        return IsFixed ? this : new TypeReference(Kind, IsNullable, true, Definition, First, Second);
    }

    internal TypeReference AsNonFixed() => IsFixed ? new TypeReference(Kind, IsNullable, false, Definition, First, Second) : this;

    internal TypeReference WithFlags(bool isNullable, bool isFixed) =>
        isNullable == IsNullable && isFixed == IsFixed ? this : new TypeReference(Kind, isNullable, isFixed, Definition, First, Second);

    /// <inheritdoc />
    public bool Equals(TypeReference? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        return other != null
            && Kind == other.Kind
            && IsNullable == other.IsNullable
            && IsFixed == other.IsFixed
            && ReferenceEquals(Definition, other.Definition)
            && Equals(First, other.First)
            && Equals(Second, other.Second);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TypeReference);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)Kind * 397;
            hash = (hash * 31) + (IsNullable ? 1 : 0);
            hash = (hash * 31) + (IsFixed ? 1 : 0);
            hash = (hash * 31) + (Definition?.GetHashCode() ?? 0);
            hash = (hash * 31) + (First?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Second?.GetHashCode() ?? 0);
            return hash;
        }
    }

    /// <summary>
    /// Representation in the schema language, with fully qualified type names
    /// </summary>
    public override string ToString() => ToString(null);

    /// <summary>
    /// Representation in the schema language, type names relative to a schema
    /// </summary>
    public string ToString(Schema? context)
    {
        var builder = new StringBuilder();
        Append(builder, context);
        return builder.ToString();
    }

    internal void Append(StringBuilder builder, Schema? context)
    {
        switch (Kind)
        {
            case TypeKind.Enum:
            case TypeKind.Struct:
                builder.Append(Definition!.GetName(context));
                break;
            case TypeKind.List:
                builder.Append("list<");
                First!.Append(builder, context);
                builder.Append('>');
                break;
            case TypeKind.Map:
                builder.Append("map<");
                First!.Append(builder, context);
                builder.Append(", ");
                Second!.Append(builder, context);
                builder.Append('>');
                break;
            case TypeKind.Ref:
            case TypeKind.ExtRef:
                builder.Append(Kind.ToKeyword()).Append('<').Append(Definition!.GetName(context)).Append('>');
                break;
            default:
                builder.Append(Kind.ToKeyword());
                break;
        }

        if (IsNullable)
            builder.Append('?');
    }
}
