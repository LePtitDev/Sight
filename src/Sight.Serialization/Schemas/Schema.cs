using System;
using System.Collections.Generic;
using System.Linq;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;

namespace Sight.Serialization.Schemas;

/// <summary>
/// Named and versioned set of type definitions
/// </summary>
public sealed partial class Schema
{
    private readonly List<Schema> _imports = new();
    private readonly List<TypeDefinition> _types = new();
    private bool _frozen;
    private ulong? _hash;
    private TypeDefinition[]? _localTypes;
    private Dictionary<TypeDefinition, int>? _localIndexes;
    private Schema[]? _closure;

    /// <summary>
    /// Initialize a new instance of <see cref="Schema"/>
    /// </summary>
    public Schema(string name, uint version, IEnumerable<Schema>? imports = null)
    {
        if (!Names.IsQualifiedName(name))
            throw new ArgumentException($"Invalid schema name '{name}'", nameof(name));

        Name = name;
        Version = version;
        if (imports != null)
            _imports.AddRange(imports);
    }

    /// <summary>
    /// Name of the schema (qualified name)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Version of the schema
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Schemas whose types are usable by this one
    /// </summary>
    public IReadOnlyList<Schema> Imports => _imports;

    /// <summary>
    /// Metadata of the schema (not part of the schema model nor of its hash; stored in compiled schema headers)
    /// </summary>
    public MetadataCollection Metadata { get; } = new();

    /// <summary>
    /// Types declared by this schema, in declaration order
    /// </summary>
    public IReadOnlyList<TypeDefinition> Types => _types;

    /// <summary>
    /// Indicates whether the schema has been validated and can no longer be modified
    /// </summary>
    public bool IsFrozen => _frozen;

    /// <summary>
    /// XXH64 hash of the canonical encoding (validates the schema if needed)
    /// </summary>
    public ulong Hash
    {
        get
        {
            if (_hash.HasValue)
                return _hash.Value;

            Validate();
            _hash = ReferenceEquals(this, CoreSchema.Instance) ? 0UL : SchemaCodec.ComputeHash(this);
            return _hash.Value;
        }
    }

    /// <summary>
    /// Indicates whether <see cref="Hash"/> has already been computed
    /// </summary>
    internal bool HasHash => _hash.HasValue;

    /// <summary>
    /// Built-in <c>core</c> meta-schema
    /// </summary>
    public static Schema Core => CoreSchema.Instance;

    /// <summary>
    /// Add an import
    /// </summary>
    public void AddImport(Schema schema)
    {
        EnsureMutable();
        _imports.Add(schema ?? throw new ArgumentNullException(nameof(schema)));
    }

    /// <summary>
    /// Declare a new struct
    /// </summary>
    public StructDefinition AddStruct(string name, StructDefinition? baseType = null, bool isAbstract = false)
    {
        EnsureMutable();
        var definition = new StructDefinition(this, name, baseType, isAbstract);
        _types.Add(definition);
        return definition;
    }

    /// <summary>
    /// Declare a new enum
    /// </summary>
    public EnumDefinition AddEnum(string name, TypeKind underlying = TypeKind.S32)
    {
        EnsureMutable();
        var definition = new EnumDefinition(this, name, underlying);
        _types.Add(definition);
        return definition;
    }

    /// <summary>
    /// Declare a new type alias
    /// </summary>
    public AliasDefinition AddAlias(string name, TypeReference target)
    {
        EnsureMutable();
        var definition = new AliasDefinition(this, name, target ?? throw new ArgumentNullException(nameof(target)));
        _types.Add(definition);
        return definition;
    }

    /// <summary>
    /// Declare a type alias whose target is resolved later (<see cref="AliasDefinition.Resolve"/>)
    /// </summary>
    internal AliasDefinition AddUnresolvedAlias(string name)
    {
        EnsureMutable();
        var definition = new AliasDefinition(this, name, null);
        _types.Add(definition);
        return definition;
    }

    /// <summary>
    /// Find a type by name: qualified (<c>schema.Type</c>), or unqualified in this schema then in its direct imports
    /// </summary>
    /// <exception cref="SerializationException">The unqualified name is ambiguous</exception>
    public TypeDefinition? FindType(string name)
    {
        var own = FindOwnType(name);
        if (own != null)
            return own;

        var separator = name.LastIndexOf('.');
        if (separator > 0)
        {
            var schemaName = name.Substring(0, separator);
            var typeName = name.Substring(separator + 1);
            if (string.Equals(schemaName, Name, StringComparison.Ordinal))
                return FindOwnType(typeName);

            foreach (var import in _imports)
            {
                if (string.Equals(import.Name, schemaName, StringComparison.Ordinal))
                    return import.FindOwnType(typeName);
            }

            return null;
        }

        TypeDefinition? found = null;
        foreach (var import in _imports)
        {
            var candidate = import.FindOwnType(name);
            if (candidate == null)
                continue;

            if (found != null)
                throw new SerializationException($"Type name '{name}' is ambiguous between '{found.QualifiedName}' and '{candidate.QualifiedName}'");

            found = candidate;
        }

        return found;
    }

    /// <summary>
    /// Validate the schema and its imports, then freeze them
    /// </summary>
    /// <exception cref="SerializationException">The schema is invalid</exception>
    public void Validate()
    {
        if (_frozen)
            return;

        SchemaValidator.Validate(this);
        foreach (var definition in _types.OfType<StructDefinition>())
            definition.ResetCache();

        _frozen = true;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name} {Version}";

    /// <summary>
    /// Types usable by this schema: its own types, then the types of each direct import
    /// </summary>
    internal IReadOnlyList<TypeDefinition> LocalTypes => _localTypes ??= _types.Concat(_imports.SelectMany(x => x._types)).ToArray();

    /// <summary>
    /// Closure: depth-first pre-order traversal of imports, each schema once
    /// </summary>
    internal IReadOnlyList<Schema> Closure => _closure ??= BuildClosure();

    internal TypeDefinition? FindOwnType(string name)
    {
        foreach (var type in _types)
        {
            if (string.Equals(type.Name, name, StringComparison.Ordinal))
                return type;
        }

        return null;
    }

    internal int GetLocalIndex(TypeDefinition definition)
    {
        _localIndexes ??= LocalTypes.Select((x, i) => (x, i)).ToDictionary(x => x.x, x => x.i);
        return _localIndexes.TryGetValue(definition, out var index)
            ? index
            : throw new SerializationException($"Type '{definition.QualifiedName}' is not usable by schema '{Name}' (missing import?)");
    }

    internal void EnsureMutable()
    {
        if (_frozen)
            throw new InvalidOperationException($"Schema '{Name}' is frozen and cannot be modified");

        _hash = null;
        _localTypes = null;
        _localIndexes = null;
        _closure = null;
    }

    private Schema[] BuildClosure() => ClosureOf(new[] { this });

    /// <summary>
    /// Closure of a list of schemas: depth-first pre-order traversal of each schema and its imports, each schema once
    /// </summary>
    internal static Schema[] ClosureOf(IEnumerable<Schema> schemas)
    {
        var result = new List<Schema>();
        var visited = new Dictionary<string, Schema>(StringComparer.Ordinal);
        foreach (var schema in schemas)
            Visit(schema);

        return result.ToArray();

        void Visit(Schema schema)
        {
            if (visited.TryGetValue(schema.Name, out var existing))
            {
                if (existing.Version != schema.Version)
                    throw new SerializationException($"Schema closure contains '{schema.Name}' in versions {existing.Version} and {schema.Version}");

                return;
            }

            visited.Add(schema.Name, schema);
            result.Add(schema);
            foreach (var import in schema._imports)
                Visit(import);
        }
    }
}
