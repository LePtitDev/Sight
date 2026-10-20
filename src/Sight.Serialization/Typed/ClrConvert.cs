using System;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Typed;

/// <summary>
/// Conversions between schema values and CLR values
/// </summary>
internal static class ClrConvert
{
    /// <summary>
    /// Convert a scalar or enum data value (defaults) to a CLR value
    /// </summary>
    public static object? FromData(DataValue value, ClrValueType target)
    {
        if (value.IsNull)
            return null;

        if (target.Shape == ClrShape.Converted)
            return target.SurrogateConverter!.FromSurrogate(FromData(value, target.Surrogate!) ?? throw new SerializationException($"null cannot be converted to '{target.Type}'"));

        switch (value)
        {
            case DataEnum data:
                return EnumToClr(data.Definition, data.Value.Value, target);
            case DataScalar data when data.Kind == TypeKind.String:
                return target.Shape is ClrShape.Scalar or ClrShape.Any ? data.AsString() : throw Incompatible(data.Kind, target);
            case DataScalar data when data.Kind == TypeKind.Bool:
                return ScalarToClr(data.AsBoolean(), target);
            case DataScalar data when data.Kind.IsFloat():
                return ScalarToClr(data.Kind == TypeKind.F32 ? (object)(float)data.AsDouble() : data.AsDouble(), target);
            case DataScalar data when data.Kind.IsSignedInteger():
                return ScalarToClr(data.AsInt64(), target);
            case DataScalar data when data.Kind.IsUnsignedInteger():
                return ScalarToClr(data.AsUInt64(), target);
            default:
                throw new SerializationException($"Cannot convert a value of kind {value.Kind} to '{target.Type}'");
        }
    }

    /// <summary>
    /// Convert a raw scalar (bool, long, ulong, float, double, string) to the target CLR type
    /// </summary>
    public static object ScalarToClr(object raw, ClrValueType target)
    {
        if (target.Shape == ClrShape.Any)
            return raw;

        if (target.Shape != ClrShape.Scalar)
            throw new SerializationException($"Cannot convert a {raw.GetType().Name} value to '{target.Type}'");

        var type = Nullable.GetUnderlyingType(target.Type) ?? target.Type;
        if (raw.GetType() == type)
            return raw;

        try
        {
            switch (raw)
            {
                case long value:
                    if (type == typeof(byte)) return checked((byte)value);
                    if (type == typeof(sbyte)) return checked((sbyte)value);
                    if (type == typeof(short)) return checked((short)value);
                    if (type == typeof(ushort)) return checked((ushort)value);
                    if (type == typeof(int)) return checked((int)value);
                    if (type == typeof(uint)) return checked((uint)value);
                    if (type == typeof(ulong)) return checked((ulong)value);
                    if (type == typeof(float)) return (float)value;
                    if (type == typeof(double)) return (double)value;
                    break;
                case ulong value:
                    if (type == typeof(byte)) return checked((byte)value);
                    if (type == typeof(sbyte)) return checked((sbyte)value);
                    if (type == typeof(short)) return checked((short)value);
                    if (type == typeof(ushort)) return checked((ushort)value);
                    if (type == typeof(int)) return checked((int)value);
                    if (type == typeof(uint)) return checked((uint)value);
                    if (type == typeof(long)) return checked((long)value);
                    if (type == typeof(float)) return (float)value;
                    if (type == typeof(double)) return (double)value;
                    break;
                case float value:
                    if (type == typeof(double)) return (double)value;
                    break;
                case double value:
                    if (type == typeof(float)) return (float)value;
                    break;
            }
        }
        catch (OverflowException)
        {
            throw new SerializationException($"Value {raw} is out of range for '{type}'");
        }

        throw new SerializationException($"Cannot convert a {raw.GetType().Name} value to '{type}'");
    }

    public static object EnumToClr(EnumDefinition definition, long value, ClrValueType target)
    {
        var name = definition.FindValue(value)?.Name ?? throw new SerializationException($"Value {value} is not declared by enum '{definition.QualifiedName}'");
        if (target.Shape == ClrShape.Enum)
        {
            return target.Enum!.ValuesByName.TryGetValue(name, out var clr)
                ? clr
                : throw new SerializationException($"Enum '{target.Type}' has no value '{name}'");
        }

        if (target.Shape == ClrShape.Any)
            return new DataEnum(definition.FindValue(value)!);

        throw new SerializationException($"Cannot convert a value of enum '{definition.QualifiedName}' to '{target.Type}'");
    }

    /// <summary>
    /// Equality used to omit values equal to their default (bitwise for floats)
    /// </summary>
    public static bool AreEqual(object value, object? defaultValue)
    {
        return value switch
        {
            double d when defaultValue is double e => BitConverter.DoubleToInt64Bits(d) == BitConverter.DoubleToInt64Bits(e),
            float f when defaultValue is float g => Internal.BitHelper.SingleToInt32Bits(f) == Internal.BitHelper.SingleToInt32Bits(g),
            _ => value.Equals(defaultValue),
        };
    }

    private static SerializationException Incompatible(TypeKind kind, ClrValueType target)
    {
        return new SerializationException($"Cannot convert a value of kind {kind} to '{target.Type}'");
    }
}
