using System;

namespace Sight.Serialization;

/// <summary>
/// Converts values of a CLR type to and from a surrogate type, serialized in their place
/// </summary>
/// <remarks>
/// The schema type of the values is the schema type of <typeparamref name="TSurrogate"/>. Declare a type identity on the converter
/// with <see cref="SerializationTypeAttribute"/> (e.g. <c>[SerializationType("game.Color")]</c>) to name it with a type alias
/// (<c>type Color = u32</c>). Converters are applied with <see cref="SerializationConverterAttribute"/> and never receive <c>null</c>.
/// </remarks>
/// <typeparam name="T">Converted CLR type</typeparam>
/// <typeparam name="TSurrogate">Serialized type: a primitive, a collection, a serialized struct, or another converted type</typeparam>
public abstract class SerializationConverter<T, TSurrogate> : ISurrogateConverter
{
    Type ISurrogateConverter.Type => typeof(T);

    Type ISurrogateConverter.SurrogateType => typeof(TSurrogate);

    /// <summary>
    /// Convert a value to its surrogate
    /// </summary>
    public abstract TSurrogate ToSurrogate(T value);

    /// <summary>
    /// Convert a surrogate read from data to a value
    /// </summary>
    public abstract T FromSurrogate(TSurrogate surrogate);

    object? ISurrogateConverter.ToSurrogate(object value) => ToSurrogate((T)value);

    object? ISurrogateConverter.FromSurrogate(object surrogate) => FromSurrogate((TSurrogate)surrogate);
}

/// <summary>
/// Untyped access to a <see cref="SerializationConverter{T, TSurrogate}"/>
/// </summary>
internal interface ISurrogateConverter
{
    public Type Type { get; }

    public Type SurrogateType { get; }

    public object? ToSurrogate(object value);

    public object? FromSurrogate(object surrogate);
}

/// <summary>
/// Serialize values through a <see cref="SerializationConverter{T, TSurrogate}"/>
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>on a property or a field: values of the member (the converted type must be the member type, or its <see cref="Nullable{T}"/> underlying type);</item>
/// <item>on a class or a struct: every value of that type, including collection items;</item>
/// <item>on an assembly: every value of the converted type, for types declared elsewhere (<c>System.Numerics.Vector3</c>…).</item>
/// </list>
/// The most specific declaration wins. Arguments are passed to the converter constructor: they configure the conversion,
/// but cannot change the schema, which only depends on the surrogate type.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class SerializationConverterAttribute : Attribute
{
    /// <summary>
    /// Initialize a new instance of <see cref="SerializationConverterAttribute"/>
    /// </summary>
    /// <param name="converterType">Type deriving from <see cref="SerializationConverter{T, TSurrogate}"/></param>
    /// <param name="arguments">Arguments of the converter constructor</param>
    public SerializationConverterAttribute(Type converterType, params object?[] arguments)
    {
        ConverterType = converterType;
        Arguments = arguments ?? Array.Empty<object?>();
    }

    /// <summary>
    /// Type deriving from <see cref="SerializationConverter{T, TSurrogate}"/>
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// Arguments of the converter constructor
    /// </summary>
    public object?[] Arguments { get; }
}
