using System.Collections.Generic;
using System.Linq;

namespace Sight.Serialization.Schemas;

/// <summary>
/// File type table: concatenation of the types of a schema closure
/// </summary>
internal sealed class TypeTable
{
    private readonly Dictionary<TypeDefinition, int> _indexes = new();
    private readonly Dictionary<StructDefinition, StructDefinition[]> _concreteSets = new();
    private readonly Dictionary<string, TypeDefinition> _byIdentity = new(System.StringComparer.Ordinal);

    public TypeTable(IReadOnlyList<Schema> closure)
    {
        Closure = closure;
        Types = closure.SelectMany(x => x.Types).ToArray();
        for (var i = 0; i < Types.Count; i++)
        {
            _indexes[Types[i]] = i;
            _byIdentity[Types[i].QualifiedName] = Types[i];
        }
    }

    public IReadOnlyList<Schema> Closure { get; }

    /// <summary>
    /// Type table of a list of file schemas
    /// </summary>
    public static TypeTable For(IEnumerable<Schema> schemas) => new(Schema.ClosureOf(schemas));

    /// <summary>
    /// Find a type by identity (<c>schema.Type</c>) or by unqualified name, unique among all the schemas of the table
    /// </summary>
    /// <exception cref="SerializationException">The unqualified name is ambiguous</exception>
    public TypeDefinition? FindByName(string name)
    {
        var definition = FindByIdentity(name);
        if (definition != null || name.IndexOf('.') >= 0)
            return definition;

        foreach (var type in Types)
        {
            if (!string.Equals(type.Name, name, System.StringComparison.Ordinal))
                continue;

            if (definition != null)
                throw new SerializationException($"Type name '{name}' is ambiguous between '{definition.QualifiedName}' and '{type.QualifiedName}'");

            definition = type;
        }

        return definition;
    }

    public IReadOnlyList<TypeDefinition> Types { get; }

    public int IndexOf(TypeDefinition definition)
    {
        return _indexes.TryGetValue(definition, out var index)
            ? index
            : throw new SerializationException($"Type '{definition.QualifiedName}' is not part of the file schema");
    }

    public bool Contains(TypeDefinition definition) => _indexes.ContainsKey(definition);

    public TypeDefinition? FindByIdentity(string qualifiedName) => _byIdentity.TryGetValue(qualifiedName, out var definition) ? definition : null;

    public TypeDefinition Get(ulong index)
    {
        return index < (ulong)Types.Count
            ? Types[(int)index]
            : throw new SerializationException($"Invalid type index {index}");
    }

    public StructDefinition GetStruct(ulong index)
    {
        return Get(index) as StructDefinition ?? throw new SerializationException($"Type index {index} is not a struct");
    }

    /// <summary>
    /// Concrete set C(T): T if not abstract plus non-abstract descendants, ordered by table index
    /// </summary>
    public StructDefinition[] GetConcreteSet(StructDefinition type)
    {
        if (_concreteSets.TryGetValue(type, out var set))
            return set;

        set = Types.OfType<StructDefinition>().Where(x => !x.IsAbstract && x.IsAssignableTo(type)).ToArray();
        _concreteSets[type] = set;
        return set;
    }
}
