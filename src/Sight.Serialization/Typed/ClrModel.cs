using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Typed;

internal enum ClrShape
{
    Scalar,
    Enum,
    ByteArray,
    List,
    Map,
    Struct,
    ExtRef,
    Any,

    /// <summary>
    /// Value serialized as a surrogate (<see cref="SerializationConverter{T, TSurrogate}"/>)
    /// </summary>
    Converted,
}

/// <summary>
/// CLR side of a value position, parallel to its <see cref="TypeReference"/>
/// </summary>
internal sealed class ClrValueType
{
    public ClrValueType(Type type, ClrShape shape, TypeReference schemaType)
    {
        Type = type;
        Shape = shape;
        SchemaType = schemaType;
    }

    /// <summary>
    /// Declared CLR type (including <see cref="Nullable{T}"/>)
    /// </summary>
    public Type Type { get; }

    public ClrShape Shape { get; }

    public TypeReference SchemaType { get; set; }

    public ClrValueType? Element { get; set; }

    public ClrValueType? Key { get; set; }

    public ClrValueType? Value { get; set; }

    public ClrTypeInfo? Struct { get; set; }

    public ClrEnumInfo? Enum { get; set; }

    /// <summary>
    /// Encoding and decoding of the values (typed when possible)
    /// </summary>
    public ClrConverter Converter { get; set; } = null!;

    /// <summary>
    /// Creates a collection (list or map) from a capacity
    /// </summary>
    public Func<int, object>? CreateCollection { get; set; }

    /// <summary>
    /// Adds an item to a list being read
    /// </summary>
    public Action<object, object?>? AddItem { get; set; }

    /// <summary>
    /// Converts a list being read to the final value (arrays), or a byte array to the final value (memory types)
    /// </summary>
    public Func<object, object>? Complete { get; set; }

    /// <summary>
    /// Value position of the surrogate of a converted value
    /// </summary>
    public ClrValueType? Surrogate { get; set; }

    /// <summary>
    /// Converter of a converted value
    /// </summary>
    public ISurrogateConverter? SurrogateConverter { get; set; }

    /// <summary>
    /// Target type of an <see cref="ExternalRef{T}"/>
    /// </summary>
    public Type? ExtRefTarget { get; set; }
}

/// <summary>
/// CLR class or struct mapped to a schema struct
/// </summary>
internal sealed class ClrTypeInfo
{
    public ClrTypeInfo(Type type, string schemaName, string name)
    {
        Type = type;
        SchemaName = schemaName;
        Name = name;
        QualifiedName = schemaName + "." + name;
    }

    public Type Type { get; }

    public string SchemaName { get; }

    public string Name { get; }

    public string QualifiedName { get; }

    /// <summary>
    /// Number of presence slots of <see cref="AllMembers"/>
    /// </summary>
    public int SlotCount { get; set; }

    /// <summary>
    /// Plans reading data of writer struct types into this type, by writer schema hash and writer type name
    /// </summary>
    public ConcurrentDictionary<(ulong SchemaHash, string TypeName), ClrReadPlan> ReadPlans { get; } = new();

    public bool IsReference { get; set; }

    public bool IsAbstract => Type.IsAbstract;

    public ClrTypeInfo? Base { get; set; }

    public List<ClrMember> OwnMembers { get; } = new();

    /// <summary>
    /// Members aligned with <see cref="StructDefinition.AllFields"/>
    /// </summary>
    public ClrMember[] AllMembers { get; set; } = Array.Empty<ClrMember>();

    public StructDefinition Definition { get; set; } = null!;

    public Func<object>? Create { get; set; }

    public ClrMember? FindMember(string name)
    {
        foreach (var member in AllMembers)
        {
            if (string.Equals(member.Name, name, StringComparison.Ordinal))
                return member;
        }

        return null;
    }

    public override string ToString() => $"{Type.Name} ({QualifiedName})";
}

/// <summary>
/// CLR enum mapped to a schema enum
/// </summary>
internal sealed class ClrEnumInfo
{
    public ClrEnumInfo(Type type, string schemaName, string name)
    {
        Type = type;
        SchemaName = schemaName;
        Name = name;
        QualifiedName = schemaName + "." + name;
    }

    public Type Type { get; }

    public string SchemaName { get; }

    public string Name { get; }

    public string QualifiedName { get; }

    public EnumDefinition Definition { get; set; } = null!;

    /// <summary>
    /// CLR value of each enum value name
    /// </summary>
    public Dictionary<string, object> ValuesByName { get; } = new(StringComparer.Ordinal);

    public long ToSchemaValue(object value)
    {
        return Definition.Underlying.IsSignedInteger() ? Convert.ToInt64(value) : unchecked((long)Convert.ToUInt64(value));
    }
}

/// <summary>
/// Serialized member of a CLR type
/// </summary>
internal sealed class ClrMember
{
    public ClrMember(MemberInfo member, string name, ClrValueType valueType)
    {
        Member = member;
        Name = name;
        ValueType = valueType;
    }

    public MemberInfo Member { get; }

    public string Name { get; }

    public ClrValueType ValueType { get; }

    public string[] Aliases { get; set; } = Array.Empty<string>();

    public DataValue? DefaultValue { get; set; }

    /// <summary>
    /// Default value as a CLR object, compared to values when writing
    /// </summary>
    public object? DefaultClrValue { get; set; }

    /// <summary>
    /// Typed access to the member value
    /// </summary>
    public ClrMemberAccessor Accessor { get; set; } = null!;

    public FieldDefinition Field { get; set; } = null!;
}

/// <summary>
/// Model of a set of CLR types: schemas and mapping information
/// </summary>
internal sealed class ClrModel
{
    public Dictionary<Type, ClrTypeInfo> Structs { get; } = new();

    public Dictionary<Type, ClrEnumInfo> Enums { get; } = new();

    public Dictionary<string, ClrTypeInfo> StructsByIdentity { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, ClrEnumInfo> EnumsByIdentity { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, Schema> Schemas { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// File schemas of files written with a root schema
    /// </summary>
    public ConcurrentDictionary<Schema, IReadOnlyList<Schema>> FileSchemas { get; } = new();

    public ClrTypeInfo GetStruct(Type type)
    {
        return Structs.TryGetValue(type, out var info)
            ? info
            : throw new SerializationException($"Type '{type}' is not part of the serialization model (declare it with [SerializationDerivedType] on its base type)");
    }

    /// <summary>
    /// Struct info of a CLR type or its closest mapped base type
    /// </summary>
    public ClrTypeInfo? FindStruct(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (Structs.TryGetValue(current, out var info))
                return info;
        }

        return null;
    }
}

/// <summary>
/// How data of a writer struct type is read into a CLR type (binary specification §9)
/// </summary>
internal sealed class ClrReadPlan
{
    public ClrReadPlan(int fieldCount)
    {
        Members = new ClrMember?[fieldCount];
        Defaults = new object?[fieldCount];
    }

    /// <summary>
    /// Member of each writer field (null: skipped)
    /// </summary>
    public ClrMember?[] Members { get; }

    /// <summary>
    /// CLR default value of each writer field, assigned when the field is absent
    /// </summary>
    public object?[] Defaults { get; }

    /// <summary>
    /// Members missing in the writer type and their value
    /// </summary>
    public List<(ClrMember Member, object? Default)> Missing { get; } = new();

    public int SlotCount { get; set; }

    public int BitmapSize { get; set; }
}

/// <summary>
/// Accessors compiled with expression trees
/// </summary>
internal static class Accessors
{
    public static Func<object, T> Getter<T>(MemberInfo member)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var declaring = member.DeclaringType!;
        Expression target = declaring.IsValueType ? Expression.Unbox(instance, declaring) : Expression.Convert(instance, declaring);
        var access = Expression.MakeMemberAccess(target, member);
        return Expression.Lambda<Func<object, T>>(access, instance).Compile();
    }

    public static Action<object, T> Setter<T>(MemberInfo member)
    {
        if (member is PropertyInfo property && property.SetMethod == null)
            throw new SerializationException($"Property '{member.DeclaringType}.{member.Name}' has no setter");

        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(T), "value");
        var declaring = member.DeclaringType!;
        Expression target = declaring.IsValueType ? Expression.Unbox(instance, declaring) : Expression.Convert(instance, declaring);
        var assign = Expression.Assign(Expression.MakeMemberAccess(target, member), value);
        return Expression.Lambda<Action<object, T>>(assign, instance, value).Compile();
    }

    public static Func<object>? Constructor(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
            return null;

        if (type.IsValueType)
            return Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(type), typeof(object))).Compile();

        var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)
            ?? throw new SerializationException($"Type '{type}' has no parameterless constructor");

        return Expression.Lambda<Func<object>>(Expression.New(constructor)).Compile();
    }
}
