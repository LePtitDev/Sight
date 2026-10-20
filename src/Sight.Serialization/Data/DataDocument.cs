using System;
using System.Collections.Generic;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Data;

/// <summary>
/// Untyped document: header, schema, root value and exports
/// </summary>
public sealed class DataDocument
{
    /// <summary>
    /// Initialize a new instance of <see cref="DataDocument"/> using a single schema
    /// </summary>
    public DataDocument(Schema schema, DataStruct root)
        : this(new[] { schema ?? throw new ArgumentNullException(nameof(schema)) }, root)
    {
    }

    /// <summary>
    /// Initialize a new instance of <see cref="DataDocument"/> using several schemas
    /// </summary>
    public DataDocument(IEnumerable<Schema> schemas, DataStruct root)
    {
        Schemas.AddRange(schemas ?? throw new ArgumentNullException(nameof(schemas)));
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    /// <summary>
    /// File identifier (may be empty)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Content version
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// Metadata
    /// </summary>
    public MetadataCollection Metadata { get; } = new();

    /// <summary>
    /// Dependencies targeted by external references
    /// </summary>
    public List<DependencyReference> Dependencies { get; } = new();

    /// <summary>
    /// File schemas: the types of their closure can be used by the document
    /// </summary>
    public List<Schema> Schemas { get; } = new();

    /// <summary>
    /// How the schema is stored when the document is written
    /// </summary>
    public SchemaMode SchemaMode { get; set; } = SchemaMode.Inline;

    /// <summary>
    /// Root value
    /// </summary>
    public DataStruct Root { get; set; }

    /// <summary>
    /// Named exports
    /// </summary>
    public List<KeyValuePair<string, DataStruct>> Exports { get; } = new();

    /// <summary>
    /// Find a type by identity (<c>schema.Type</c>) or by unqualified name among the schemas of the document and their imports
    /// </summary>
    /// <exception cref="SerializationException">The unqualified name is ambiguous</exception>
    public TypeDefinition? FindType(string name) => TypeTable.For(Schemas).FindByName(name);

    /// <summary>
    /// Create a new struct value of a type of the document schemas
    /// </summary>
    public DataStruct CreateStruct(string typeName)
    {
        return FindType(typeName) is StructDefinition definition
            ? new DataStruct(definition)
            : throw new ArgumentException($"Struct '{typeName}' not found in the document schemas", nameof(typeName));
    }
}

/// <summary>
/// Reference to a dependency: identifier and version
/// </summary>
public readonly struct DependencyReference : IEquatable<DependencyReference>
{
    /// <summary>
    /// Initialize a new instance of <see cref="DependencyReference"/>
    /// </summary>
    public DependencyReference(string id, uint version)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Version = version;
    }

    /// <summary>
    /// Identifier of the dependency
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Version of the dependency
    /// </summary>
    public uint Version { get; }

    /// <inheritdoc />
    public bool Equals(DependencyReference other) => string.Equals(Id, other.Id, StringComparison.Ordinal) && Version == other.Version;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DependencyReference other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((StringComparer.Ordinal.GetHashCode(Id ?? string.Empty) * 397) ^ (int)Version);

    /// <inheritdoc />
    public override string ToString() => $"{Id} {Version}";
}

/// <summary>
/// Ordered collection of metadata (string keys and values)
/// </summary>
public sealed class MetadataCollection : IEnumerable<KeyValuePair<string, string>>
{
    private readonly List<KeyValuePair<string, string>> _items = new();

    /// <summary>
    /// Number of entries
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Value of a key
    /// </summary>
    public string this[string key]
    {
        get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"Metadata '{key}' not found");
        set
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var index = IndexOf(key);
            var item = new KeyValuePair<string, string>(key, value ?? throw new ArgumentNullException(nameof(value)));
            if (index < 0)
                _items.Add(item);
            else
                _items[index] = item;
        }
    }

    /// <summary>
    /// Add a new entry
    /// </summary>
    public void Add(string key, string value)
    {
        if (IndexOf(key) >= 0)
            throw new ArgumentException($"Duplicate metadata key '{key}'", nameof(key));

        this[key] = value;
    }

    /// <summary>
    /// Try to get the value of a key
    /// </summary>
    public bool TryGetValue(string key, out string value)
    {
        var index = IndexOf(key);
        value = index >= 0 ? _items[index].Value : string.Empty;
        return index >= 0;
    }

    /// <summary>
    /// Remove all entries
    /// </summary>
    public void Clear() => _items.Clear();

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _items.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private int IndexOf(string key)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (string.Equals(_items[i].Key, key, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }
}
