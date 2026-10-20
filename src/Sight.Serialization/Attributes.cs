using System;

namespace Sight.Serialization;

/// <summary>
/// Declare the type identity (<c>schema.Type</c>) of a class, struct or enum
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false)]
public sealed class SerializationTypeAttribute : Attribute
{
    /// <summary>
    /// Initialize a new instance of <see cref="SerializationTypeAttribute"/>
    /// </summary>
    /// <param name="id">Type identity, e.g. <c>game.Player</c></param>
    public SerializationTypeAttribute(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Type identity, e.g. <c>game.Player</c>
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Indicates whether every use of this type is an internal reference (<c>ref&lt;T&gt;</c>): identity is preserved, cycles are allowed
    /// </summary>
    public bool IsReference { get; set; }

    /// <summary>
    /// Indicates whether values of this struct are written in positional form in text documents: <c>(1.5, 0.0, -3.0)</c>
    /// (every serialized member must be a non-nullable scalar or enum)
    /// </summary>
    public bool IsPositional { get; set; }
}

/// <summary>
/// Declare a derived type of a class, so that it can be (de)serialized through the base type
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class SerializationDerivedTypeAttribute : Attribute
{
    /// <summary>
    /// Initialize a new instance of <see cref="SerializationDerivedTypeAttribute"/>
    /// </summary>
    public SerializationDerivedTypeAttribute(Type derivedType)
    {
        DerivedType = derivedType;
    }

    /// <summary>
    /// Derived type
    /// </summary>
    public Type DerivedType { get; }
}

/// <summary>
/// Declare the version of a schema, on the assembly declaring its types
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class SerializationSchemaAttribute : Attribute
{
    /// <summary>
    /// Initialize a new instance of <see cref="SerializationSchemaAttribute"/>
    /// </summary>
    public SerializationSchemaAttribute(string name, uint version)
    {
        Name = name;
        Version = version;
    }

    /// <summary>
    /// Schema name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Schema version
    /// </summary>
    public uint Version { get; }
}

/// <summary>
/// Set the name of a serialized field (default: member name in camelCase)
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SerializationFieldAttribute : Attribute
{
    /// <summary>
    /// Initialize a new instance of <see cref="SerializationFieldAttribute"/>
    /// </summary>
    public SerializationFieldAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Field name
    /// </summary>
    public string Name { get; }
}

/// <summary>
/// Former names of a field, accepted when reading data written with an older schema
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SerializationAliasAttribute : Attribute
{
    /// <summary>
    /// Initialize a new instance of <see cref="SerializationAliasAttribute"/>
    /// </summary>
    public SerializationAliasAttribute(params string[] aliases)
    {
        Aliases = aliases;
    }

    /// <summary>
    /// Former names
    /// </summary>
    public string[] Aliases { get; }
}

/// <summary>
/// Exclude a member from serialization
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SerializationIgnoreAttribute : Attribute
{
}

/// <summary>
/// Use the fixed-width encoding for an integer member (hashes, identifiers...)
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SerializationFixedAttribute : Attribute
{
}

/// <summary>
/// Serialize a member (or the elements of a collection member) as internal references (<c>ref&lt;T&gt;</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SerializationReferenceAttribute : Attribute
{
}
