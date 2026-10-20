using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Sight.Serialization.Binary;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Typed;

/// <summary>
/// Encodes and decodes the values of a CLR value position (<see cref="ClrValueType"/>)
/// </summary>
internal abstract class ClrConverter
{
    /// <summary>
    /// Create the converter of a value position: typed for scalars, enums and lists of them, boxed otherwise
    /// </summary>
    public static ClrConverter Create(ClrValueType type)
    {
        Type converter;
        if (type.Shape == ClrShape.Scalar && type.Type == typeof(string))
        {
            converter = typeof(StringConverter);
        }
        else if (type.Shape == ClrShape.Scalar && type.Type.IsPrimitive)
        {
            converter = typeof(ScalarConverter<>).MakeGenericType(type.Type);
        }
        else if (type.Shape == ClrShape.Enum && type.Type.IsEnum)
        {
            converter = typeof(EnumConverter<>).MakeGenericType(type.Type);
        }
        else if (type.Shape == ClrShape.List && type.Element != null && ListConverter.TryGetElementType(type.Type, out var elementType) && type.Element.Type == elementType
            && elementType != typeof(bool))
        {
            converter = typeof(ListConverter<,>).MakeGenericType(type.Type, elementType);
        }
        else
        {
            converter = typeof(BoxedConverter<>).MakeGenericType(type.Type);
        }

        return (ClrConverter)Activator.CreateInstance(converter, type)!;
    }

    /// <summary>
    /// Create the typed accessor of a member
    /// </summary>
    public abstract ClrMemberAccessor CreateAccessor(MemberInfo member, ClrValueType type);
}

/// <summary>
/// Converter of values of type <typeparamref name="T"/>
/// </summary>
internal abstract class ClrConverter<T> : ClrConverter
{
    /// <summary>
    /// Write a non-null value (or fail on null)
    /// </summary>
    public abstract void Write(TypedWriter writer, ClrValueType type, T value);

    /// <summary>
    /// Read a value written with <paramref name="writerType"/> (presence already handled)
    /// </summary>
    public abstract T Read(TypedReader reader, TypeReference writerType, ClrValueType target);

    public override ClrMemberAccessor CreateAccessor(MemberInfo member, ClrValueType type) => new ClrMemberAccessor<T>(member, type, this);
}

/// <summary>
/// Generic path: values are boxed and handled by <see cref="TypedWriter.WriteValue"/> and <see cref="TypedReader.ReadValue"/>
/// </summary>
internal sealed class BoxedConverter<T> : ClrConverter<T>
{
    public BoxedConverter(ClrValueType type)
    {
    }

    public override void Write(TypedWriter writer, ClrValueType type, T value) => writer.WriteValue(type, value);

    public override T Read(TypedReader reader, TypeReference writerType, ClrValueType target) => (T)reader.ReadValue(writerType, target)!;
}

/// <summary>
/// Primitive scalars: no boxing when the writer type is the reader type
/// </summary>
internal sealed class ScalarConverter<T> : ClrConverter<T>
    where T : struct
{
    private readonly TypeKind _kind;

    public ScalarConverter(ClrValueType type)
    {
        _kind = type.SchemaType.Kind;
    }

    public override void Write(TypedWriter writer, ClrValueType type, T value)
    {
        var encoder = writer.Encoder;
        var isFixed = type.SchemaType.IsFixed;
        if (typeof(T) == typeof(int))
        {
            var x = (int)(object)value;
            if (isFixed)
                encoder.WriteFixedUInt32((uint)x);
            else
                encoder.WriteVarInt(x);
        }
        else if (typeof(T) == typeof(long))
        {
            var x = (long)(object)value;
            if (isFixed)
                encoder.WriteFixedUInt64((ulong)x);
            else
                encoder.WriteVarInt(x);
        }
        else if (typeof(T) == typeof(float))
        {
            encoder.WriteSingle((float)(object)value);
        }
        else if (typeof(T) == typeof(double))
        {
            encoder.WriteDouble((double)(object)value);
        }
        else if (typeof(T) == typeof(bool))
        {
            encoder.WriteBoolean((bool)(object)value);
        }
        else if (typeof(T) == typeof(byte))
        {
            encoder.WriteByte((byte)(object)value);
        }
        else if (typeof(T) == typeof(sbyte))
        {
            encoder.WriteByte((byte)(sbyte)(object)value);
        }
        else if (typeof(T) == typeof(short))
        {
            var x = (short)(object)value;
            if (isFixed)
                encoder.WriteFixedUInt16((ushort)x);
            else
                encoder.WriteVarInt(x);
        }
        else if (typeof(T) == typeof(ushort))
        {
            var x = (ushort)(object)value;
            if (isFixed)
                encoder.WriteFixedUInt16(x);
            else
                encoder.WriteVarUInt(x);
        }
        else if (typeof(T) == typeof(uint))
        {
            var x = (uint)(object)value;
            if (isFixed)
                encoder.WriteFixedUInt32(x);
            else
                encoder.WriteVarUInt(x);
        }
        else if (typeof(T) == typeof(ulong))
        {
            var x = (ulong)(object)value;
            if (isFixed)
                encoder.WriteFixedUInt64(x);
            else
                encoder.WriteVarUInt(x);
        }
        else
        {
            writer.WriteValue(type, value);
        }
    }

    public override T Read(TypedReader reader, TypeReference writerType, ClrValueType target)
    {
        // Conversions (widening) take the generic path
        if (writerType.Kind != _kind)
            return (T)reader.ReadValue(writerType, target)!;

        var decoder = reader.Decoder;
        if (typeof(T) == typeof(int))
            return (T)(object)(int)reader.ReadSigned(TypeKind.S32, writerType.IsFixed);

        if (typeof(T) == typeof(long))
            return (T)(object)reader.ReadSigned(TypeKind.S64, writerType.IsFixed);

        if (typeof(T) == typeof(float))
            return (T)(object)decoder.ReadSingle();

        if (typeof(T) == typeof(double))
            return (T)(object)decoder.ReadDouble();

        if (typeof(T) == typeof(bool))
            return (T)(object)decoder.ReadBoolean();

        if (typeof(T) == typeof(byte))
            return (T)(object)decoder.ReadByte();

        if (typeof(T) == typeof(sbyte))
            return (T)(object)(sbyte)decoder.ReadByte();

        if (typeof(T) == typeof(short))
            return (T)(object)(short)reader.ReadSigned(TypeKind.S16, writerType.IsFixed);

        if (typeof(T) == typeof(ushort))
            return (T)(object)(ushort)reader.ReadUnsigned(TypeKind.U16, writerType.IsFixed);

        if (typeof(T) == typeof(uint))
            return (T)(object)(uint)reader.ReadUnsigned(TypeKind.U32, writerType.IsFixed);

        if (typeof(T) == typeof(ulong))
            return (T)(object)reader.ReadUnsigned(TypeKind.U64, writerType.IsFixed);

        return (T)reader.ReadValue(writerType, target)!;
    }
}

/// <summary>
/// Strings
/// </summary>
internal sealed class StringConverter : ClrConverter<string>
{
    public StringConverter(ClrValueType type)
    {
    }

    public override void Write(TypedWriter writer, ClrValueType type, string value)
    {
        if (value == null)
            writer.WriteValue(type, null);
        else
            writer.Encoder.WriteString(value);
    }

    public override string Read(TypedReader reader, TypeReference writerType, ClrValueType target)
    {
        return writerType.Kind == TypeKind.String ? reader.Decoder.ReadString() : (string)reader.ReadValue(writerType, target)!;
    }
}

/// <summary>
/// Enums: converted to and from their schema value without boxing
/// </summary>
internal sealed class EnumConverter<T> : ClrConverter<T>
    where T : struct
{
    private readonly ClrEnumInfo _info;
    private readonly Func<T, long> _toSchemaValue;
    private readonly Dictionary<string, T> _valuesByName = new(StringComparer.Ordinal);
    private ReadCache? _cache;

    public EnumConverter(ClrValueType type)
    {
        _info = type.Enum!;
        foreach (var entry in _info.ValuesByName)
            _valuesByName.Add(entry.Key, (T)entry.Value);

        // Signed underlying types are sign-extended, unsigned ones are reinterpreted (as ClrEnumInfo.ToSchemaValue)
        var parameter = Expression.Parameter(typeof(T), "value");
        var underlying = Enum.GetUnderlyingType(typeof(T));
        _toSchemaValue = Expression.Lambda<Func<T, long>>(Expression.Convert(Expression.Convert(parameter, underlying), typeof(long)), parameter).Compile();
    }

    public override void Write(TypedWriter writer, ClrValueType type, T value)
    {
        writer.WriteEnum(_info.Definition, _toSchemaValue(value));
    }

    public override T Read(TypedReader reader, TypeReference writerType, ClrValueType target)
    {
        if (writerType.Kind != TypeKind.Enum)
            return (T)reader.ReadValue(writerType, target)!;

        var definition = writerType.EnumDefinition!;
        var value = reader.ReadEnumValue(definition);

        // Values of the last writer enum, mapped by name
        var cache = _cache;
        if (cache == null || !ReferenceEquals(cache.Definition, definition))
            _cache = cache = new ReadCache(definition);

        if (cache.Values.TryGetValue(value, out var result))
            return result;

        var name = definition.FindValue(value)?.Name ?? throw reader.Error($"Value {value} is not declared by enum '{definition.QualifiedName}'");
        if (!_valuesByName.TryGetValue(name, out result))
            throw reader.Error($"Enum '{target.Type}' has no value '{name}'");

        cache.Values[value] = result;
        return result;
    }

    private sealed class ReadCache
    {
        public ReadCache(EnumDefinition definition)
        {
            Definition = definition;
        }

        public EnumDefinition Definition { get; }

        public ConcurrentDictionary<long, T> Values { get; } = new();
    }
}

/// <summary>
/// Lists of typed elements (<see cref="List{T}"/>, arrays and the list interfaces implemented by <see cref="List{T}"/>)
/// </summary>
internal static class ListConverter
{
    public static bool TryGetElementType(Type type, out Type elementType)
    {
        elementType = null!;
        if (type.IsArray)
        {
            if (type.GetArrayRank() != 1)
                return false;

            elementType = type.GetElementType()!;
            return true;
        }

        if (!type.IsGenericType)
            return false;

        var definition = type.GetGenericTypeDefinition();
        if (definition != typeof(List<>) && definition != typeof(IList<>) && definition != typeof(ICollection<>) && definition != typeof(IEnumerable<>)
            && definition != typeof(IReadOnlyList<>) && definition != typeof(IReadOnlyCollection<>))
        {
            return false;
        }

        elementType = type.GetGenericArguments()[0];
        return true;
    }
}

internal sealed class ListConverter<TList, TElement> : ClrConverter<TList>
    where TList : class, IEnumerable<TElement>
{
    private readonly ClrConverter<TElement> _element;

    public ListConverter(ClrValueType type)
    {
        _element = (ClrConverter<TElement>)type.Element!.Converter;
    }

    public override void Write(TypedWriter writer, ClrValueType type, TList value)
    {
        if (value is not ICollection<TElement> collection)
        {
            writer.WriteValue(type, value);
            return;
        }

        var element = type.Element!;
        var isNullable = element.SchemaType.IsNullable;
        writer.EnterList();
        writer.Encoder.WriteVarUInt((ulong)collection.Count);
        var index = 0;
        if (value is List<TElement> list)
        {
            for (; index < list.Count; index++)
                WriteItem(writer, element, isNullable, list[index], index);
        }
        else if (value is TElement[] array)
        {
            for (; index < array.Length; index++)
                WriteItem(writer, element, isNullable, array[index], index);
        }
        else
        {
            foreach (var item in collection)
                WriteItem(writer, element, isNullable, item, index++);
        }

        writer.ExitList();
    }

    public override TList Read(TypedReader reader, TypeReference writerType, ClrValueType target)
    {
        var elementType = writerType.ElementType;
        if (writerType.Kind != TypeKind.List || elementType == null || (elementType.Kind == TypeKind.Bool && !elementType.IsNullable))
            return (TList)reader.ReadValue(writerType, target)!;

        var element = target.Element!;
        reader.Enter();
        var count = reader.ReadCount();
        var list = new List<TElement>(Math.Min(count, 1024));
        for (var i = 0; i < count; i++)
        {
            if (elementType.IsNullable && !reader.ReadPresence())
            {
                reader.Null(element);
                list.Add(default!);
                continue;
            }

            list.Add(_element.Read(reader, elementType, element));
        }

        reader.Exit();
        return typeof(TList).IsArray ? (TList)(object)list.ToArray() : (TList)(object)list;
    }

    private void WriteItem(TypedWriter writer, ClrValueType element, bool isNullable, TElement item, int index)
    {
        writer.SetListIndex(index);
        if (isNullable)
        {
            if (item == null)
            {
                writer.Encoder.WriteByte(0);
                return;
            }

            writer.Encoder.WriteByte(1);
        }

        _element.Write(writer, element, item);
    }
}

/// <summary>
/// Typed access to a member: values are read and written without boxing when the converter is typed
/// </summary>
internal abstract class ClrMemberAccessor
{
    public abstract object? GetValue(object instance);

    public abstract void SetValue(object instance, object? value);

    /// <summary>
    /// Default value of the member, omitted when writing
    /// </summary>
    public abstract void SetDefault(object? value);

    /// <summary>
    /// Write the value of the member; with <paramref name="omitAbsent"/>, nothing is written for null and default values
    /// </summary>
    /// <returns>false if the value was omitted</returns>
    public abstract bool Write(TypedWriter writer, object instance, bool omitAbsent);

    /// <summary>
    /// Read a value written with <paramref name="writerType"/> into the member
    /// </summary>
    public abstract void Read(TypedReader reader, TypeReference writerType, object instance);
}

internal sealed class ClrMemberAccessor<T> : ClrMemberAccessor
{
    private readonly ClrValueType _type;
    private readonly ClrConverter<T> _converter;
    private readonly Func<object, T> _getter;
    private readonly Action<object, T> _setter;
    private bool _hasDefault;
    private T _default = default!;

    public ClrMemberAccessor(MemberInfo member, ClrValueType type, ClrConverter<T> converter)
    {
        _type = type;
        _converter = converter;
        _getter = Accessors.Getter<T>(member);
        _setter = Accessors.Setter<T>(member);
    }

    public override void SetDefault(object? value)
    {
        _hasDefault = value != null;
        _default = value == null ? default! : (T)value;
    }

    public override object? GetValue(object instance) => _getter(instance);

    public override void SetValue(object instance, object? value) => _setter(instance, (T)value!);

    public override bool Write(TypedWriter writer, object instance, bool omitAbsent)
    {
        var value = _getter(instance);
        if (omitAbsent && (value == null || (_hasDefault && AreEqual(value, _default))))
            return false;

        _converter.Write(writer, _type, value);
        return true;
    }

    public override void Read(TypedReader reader, TypeReference writerType, object instance)
    {
        _setter(instance, _converter.Read(reader, writerType, _type));
    }

    /// <summary>
    /// Equality used to omit values equal to their default (bitwise for floats, see <see cref="ClrConvert.AreEqual"/>)
    /// </summary>
    private static bool AreEqual(T value, T defaultValue)
    {
        if (typeof(T) == typeof(double))
            return BitConverter.DoubleToInt64Bits((double)(object)value!) == BitConverter.DoubleToInt64Bits((double)(object)defaultValue!);

        if (typeof(T) == typeof(float))
            return Internal.BitHelper.SingleToInt32Bits((float)(object)value!) == Internal.BitHelper.SingleToInt32Bits((float)(object)defaultValue!);

        if (!typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null)
            return ClrConvert.AreEqual(value!, defaultValue);

        return EqualityComparer<T>.Default.Equals(value, defaultValue);
    }
}
