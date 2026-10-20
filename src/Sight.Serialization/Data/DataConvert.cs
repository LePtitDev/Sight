using Sight.Serialization.Schemas;

namespace Sight.Serialization.Data;

internal static class DataConvert
{
    /// <summary>
    /// Convert a scalar, string or enum value to the exact kind of a type
    /// </summary>
    public static DataValue CoerceScalar(DataValue? value, TypeReference type, string context)
    {
        if (value == null || value.IsNull)
        {
            return type.IsNullable || type.Kind == TypeKind.Any
                ? DataValue.Null
                : throw new SerializationException($"{context}: null is not allowed for type {type}");
        }

        switch (type.Kind)
        {
            case TypeKind.Bool:
                if (value.Kind == TypeKind.Bool)
                    return value;

                break;
            case TypeKind.String:
                if (value.Kind == TypeKind.String)
                    return value;

                break;
            case TypeKind.F32:
            case TypeKind.F64:
                if (value.Kind == type.Kind)
                    return value;

                if (value.Kind.IsFloat() || value.Kind.IsInteger())
                    return DataScalar.FromDouble(type.Kind, value.AsDouble());

                break;
            case TypeKind.Enum:
                var definition = type.EnumDefinition!;
                if (value is DataEnum data && string.Equals(data.Definition.QualifiedName, definition.QualifiedName, System.StringComparison.Ordinal))
                {
                    return ReferenceEquals(data.Definition, definition)
                        ? value
                        : new DataEnum(definition.FindValue(data.Name) ?? throw new SerializationException($"{context}: enum '{definition.QualifiedName}' has no value '{data.Name}'"));
                }

                if (value.Kind == TypeKind.String)
                {
                    return new DataEnum(definition.FindValue(value.AsString())
                        ?? throw new SerializationException($"{context}: enum '{definition.QualifiedName}' has no value '{value.AsString()}'"));
                }

                break;
            default:
                if (type.Kind.IsInteger() && value.Kind.IsInteger())
                {
                    if (value.Kind == type.Kind)
                        return value;

                    try
                    {
                        return value.Kind.IsSignedInteger()
                            ? DataScalar.FromInt64(type.Kind, value.AsInt64())
                            : DataScalar.FromUInt64(type.Kind, value.AsUInt64());
                    }
                    catch (System.OverflowException e)
                    {
                        throw new SerializationException($"{context}: {e.Message}", e);
                    }
                }

                break;
        }

        throw new SerializationException($"{context}: a value of kind {value.Kind} is not valid for type {type}");
    }
}
