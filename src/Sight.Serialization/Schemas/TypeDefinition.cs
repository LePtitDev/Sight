using System;
using System.Collections.Generic;
using System.Linq;
using Sight.Serialization.Data;

namespace Sight.Serialization.Schemas;

/// <summary>
/// Type declared in a schema (struct or enum)
/// </summary>
public abstract class TypeDefinition
{
    private protected TypeDefinition(Schema schema, string name)
    {
        Schema = schema;
        Name = name;
    }

    /// <summary>
    /// Schema declaring the type
    /// </summary>
    public Schema Schema { get; }

    /// <summary>
    /// Name of the type in its schema
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Type identity: <c>schema.Type</c>
    /// </summary>
    public string QualifiedName => Schema.Name + "." + Name;

    /// <inheritdoc />
    public override string ToString() => QualifiedName;

    internal string GetName(Schema? context)
    {
        return ReferenceEquals(context, Schema) ? Name : QualifiedName;
    }
}

/// <summary>
/// Struct declared in a schema
/// </summary>
public sealed class StructDefinition : TypeDefinition
{
    private readonly List<FieldDefinition> _fields = new();
    private FieldDefinition[]? _allFields;
    private StructDefinition? _base;
    private bool _isAbstract;

    internal StructDefinition(Schema schema, string name, StructDefinition? baseType, bool isAbstract)
        : base(schema, name)
    {
        _base = baseType;
        _isAbstract = isAbstract;
    }

    /// <summary>
    /// Parent struct, if any
    /// </summary>
    public StructDefinition? Base
    {
        get => _base;
        set
        {
            Schema.EnsureMutable();
            _base = value;
        }
    }

    /// <summary>
    /// Indicates whether the struct can only be instantiated through a derived type
    /// </summary>
    public bool IsAbstract
    {
        get => _isAbstract;
        set
        {
            Schema.EnsureMutable();
            _isAbstract = value;
        }
    }

    /// <summary>
    /// Fields declared by this struct
    /// </summary>
    public IReadOnlyList<FieldDefinition> Fields => _fields;

    /// <summary>
    /// Field sequence: fields of the root-most base first, then each derived level, ending with own fields
    /// </summary>
    public IReadOnlyList<FieldDefinition> AllFields => _allFields ??= BuildAllFields();

    /// <summary>
    /// Add a field to the struct
    /// </summary>
    public FieldDefinition AddField(string name, TypeReference type, DataValue? defaultValue = null, IEnumerable<string>? aliases = null)
    {
        Schema.EnsureMutable();
        if (defaultValue != null)
        {
            if (type.IsNullable || (!type.Kind.IsScalar() && type.Kind != TypeKind.Enum))
                throw new ArgumentException($"Field '{name}' of type {type} cannot have a default value", nameof(defaultValue));

            defaultValue = DataConvert.CoerceScalar(defaultValue, type, $"Default value of field '{QualifiedName}.{name}'");
        }

        var field = new FieldDefinition(this, name, type, defaultValue, aliases?.ToArray() ?? Array.Empty<string>());
        _fields.Add(field);
        return field;
    }

    /// <summary>
    /// Find a field of the field sequence by name
    /// </summary>
    public FieldDefinition? FindField(string name)
    {
        foreach (var field in AllFields)
        {
            if (string.Equals(field.Name, name, StringComparison.Ordinal))
                return field;
        }

        return null;
    }

    /// <summary>
    /// Indicates whether this struct is <paramref name="other"/> or derives from it
    /// </summary>
    public bool IsAssignableTo(StructDefinition other)
    {
        for (var current = this; current != null; current = current.Base)
        {
            if (ReferenceEquals(current, other))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Indicates whether this struct is <paramref name="other"/> or derives from it, comparing type identities
    /// </summary>
    public bool IsAssignableToIdentity(StructDefinition other)
    {
        for (var current = this; current != null; current = current.Base)
        {
            if (string.Equals(current.QualifiedName, other.QualifiedName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    internal void ResetCache() => _allFields = null;

    private FieldDefinition[] BuildAllFields()
    {
        var chain = new List<StructDefinition>();
        var visited = new HashSet<StructDefinition>();
        for (var current = this; current != null; current = current.Base)
        {
            if (!visited.Add(current))
                throw new SerializationException($"Cyclic inheritance on struct '{QualifiedName}'");

            chain.Add(current);
        }

        chain.Reverse();
        return chain.SelectMany(x => x._fields).ToArray();
    }
}

/// <summary>
/// Field of a struct
/// </summary>
public sealed class FieldDefinition
{
    internal FieldDefinition(StructDefinition owner, string name, TypeReference type, DataValue? defaultValue, IReadOnlyList<string> aliases)
    {
        Owner = owner;
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        Aliases = aliases;
    }

    /// <summary>
    /// Struct declaring the field
    /// </summary>
    public StructDefinition Owner { get; }

    /// <summary>
    /// Name of the field
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Type of the field
    /// </summary>
    public TypeReference Type { get; }

    /// <summary>
    /// Default value, if any (scalar, string or enum)
    /// </summary>
    public DataValue? DefaultValue { get; }

    /// <summary>
    /// Former names of the field, accepted when reading data written with an older schema
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// Indicates whether the field has a slot in the presence bitmap
    /// </summary>
    public bool HasPresenceSlot => Type.IsNullable || DefaultValue != null;

    /// <inheritdoc />
    public override string ToString() => $"{Name}: {Type}";
}

/// <summary>
/// Enum declared in a schema
/// </summary>
public sealed class EnumDefinition : TypeDefinition
{
    private readonly List<EnumValueDefinition> _values = new();

    internal EnumDefinition(Schema schema, string name, TypeKind underlying)
        : base(schema, name)
    {
        if (!underlying.IsInteger())
            throw new ArgumentException($"Invalid underlying type {underlying} for enum '{name}'", nameof(underlying));

        Underlying = underlying;
    }

    /// <summary>
    /// Underlying integer type
    /// </summary>
    public TypeKind Underlying { get; }

    /// <summary>
    /// Values of the enum
    /// </summary>
    public IReadOnlyList<EnumValueDefinition> Values => _values;

    /// <summary>
    /// Add a value to the enum
    /// </summary>
    public EnumValueDefinition AddValue(string name, long value)
    {
        Schema.EnsureMutable();
        var item = new EnumValueDefinition(this, name, value);
        _values.Add(item);
        return item;
    }

    /// <summary>
    /// Find a value by name
    /// </summary>
    public EnumValueDefinition? FindValue(string name)
    {
        foreach (var value in _values)
        {
            if (string.Equals(value.Name, name, StringComparison.Ordinal))
                return value;
        }

        return null;
    }

    /// <summary>
    /// Find a value by its numeric value
    /// </summary>
    public EnumValueDefinition? FindValue(long value)
    {
        foreach (var item in _values)
        {
            if (item.Value == value)
                return item;
        }

        return null;
    }
}

/// <summary>
/// Value of an enum
/// </summary>
public sealed class EnumValueDefinition
{
    internal EnumValueDefinition(EnumDefinition owner, string name, long value)
    {
        Owner = owner;
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Enum declaring the value
    /// </summary>
    public EnumDefinition Owner { get; }

    /// <summary>
    /// Name of the value
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Numeric value (bit pattern of the underlying type, as a signed 64-bit integer)
    /// </summary>
    public long Value { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Name} = {Value}";
}
