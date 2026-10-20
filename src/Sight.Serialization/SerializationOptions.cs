using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization;

/// <summary>
/// Options of serialization, deserialization and resource resolution
/// </summary>
public sealed class SerializationOptions
{
    private object? _resolverCache;
    private object? _dependencyCache;

    /// <summary>
    /// Default options
    /// </summary>
    public static SerializationOptions Default { get; } = new();

    /// <summary>
    /// File identifier written in the header (typed serialization)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Content version written in the header (typed serialization)
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// Metadata written in the header (typed serialization)
    /// </summary>
    public MetadataCollection Metadata { get; } = new();

    /// <summary>
    /// How the schema is stored in written files (typed serialization)
    /// </summary>
    public SchemaMode SchemaMode { get; set; } = SchemaMode.Inline;

    /// <summary>
    /// Named objects written as exports, targetable by external references (typed serialization)
    /// </summary>
    public Dictionary<string, object> Exports { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Maximum length in bytes of deduplicated strings (0 disables deduplication)
    /// </summary>
    public byte StringDedupMaxLength { get; set; } = 32;

    /// <summary>
    /// Compression of binary files
    /// </summary>
    public Compression Compression { get; set; }

    /// <summary>
    /// Folders searched for dependencies and referenced schemas
    /// </summary>
    public List<string> SearchPaths { get; } = new();

    /// <summary>
    /// Schemas provided in memory, used to resolve referenced schemas and imports, and as file schemas of files written with <see cref="SchemaMode.None"/>
    /// </summary>
    public List<Schema> Schemas { get; } = new();

    /// <summary>
    /// Dependencies provided in memory or simply declared
    /// </summary>
    public DependencyCollection Dependencies { get; } = new();

    /// <summary>
    /// Custom resolvers of dependencies and schemas
    /// </summary>
    public List<IResourceResolver> Resolvers { get; } = new();

    /// <summary>
    /// Accept a dependency of a newer version than the requested one when the exact version is not found
    /// </summary>
    public bool AllowNewerDependencies { get; set; }

    /// <summary>
    /// Receives diagnostics (shadowed files, warnings)
    /// </summary>
    public Action<string>? OnDiagnostic { get; set; }

    /// <summary>
    /// Limits enforced when reading
    /// </summary>
    public SerializationLimits Limits { get; } = new();

    /// <summary>
    /// Text format: number of spaces per indentation level
    /// </summary>
    public int Indent { get; set; } = 2;

    /// <summary>
    /// Text format: maximum width of containers written on a single line
    /// </summary>
    public int MaxInlineWidth { get; set; } = 80;

    /// <summary>
    /// Text format: write fields equal to their default value and null nullable fields
    /// </summary>
    public bool WriteDefaults { get; set; }

    internal T GetResolverCache<T>(Func<T> factory)
        where T : class
    {
        lock (this)
        {
            return _resolverCache as T ?? (T)(_resolverCache = factory());
        }
    }

    internal T GetDependencyCache<T>(Func<T> factory)
        where T : class
    {
        lock (this)
        {
            return _dependencyCache as T ?? (T)(_dependencyCache = factory());
        }
    }
}

/// <summary>
/// Custom resolver of dependencies and schemas
/// </summary>
public interface IResourceResolver
{
    /// <summary>
    /// Open a resource (SBDL file, binary or text) by identifier and version, or return <c>null</c> if not found
    /// </summary>
    public Stream? Open(string id, uint version);
}

/// <summary>
/// Dependency provided in memory or declared
/// </summary>
public sealed class DependencyEntry
{
    internal DependencyEntry(string id, uint version, object? value, Func<Stream>? open)
    {
        Id = id;
        Version = version;
        Value = value;
        Open = open;
    }

    /// <summary>
    /// Identifier of the dependency
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Version of the dependency
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Object or <see cref="DataDocument"/> provided in memory
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Function opening the dependency content
    /// </summary>
    public Func<Stream>? Open { get; }

    /// <summary>
    /// Indicates whether the dependency is only declared (no content provided)
    /// </summary>
    public bool IsDeclarationOnly => Value == null && Open == null;
}

/// <summary>
/// Collection of dependencies provided in memory or declared
/// </summary>
public sealed class DependencyCollection : IEnumerable<DependencyEntry>
{
    private readonly List<DependencyEntry> _entries = new();

    /// <summary>
    /// Number of entries
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Declare a dependency without providing its content
    /// </summary>
    public void Add(string id, uint version) => AddEntry(new DependencyEntry(id, version, null, null));

    /// <summary>
    /// Provide a dependency as an object (returned as is by external references) or a <see cref="DataDocument"/>
    /// </summary>
    public void Add(string id, uint version, object value) => AddEntry(new DependencyEntry(id, version, value ?? throw new ArgumentNullException(nameof(value)), null));

    /// <summary>
    /// Provide a dependency as a function opening its content
    /// </summary>
    public void Add(string id, uint version, Func<Stream> open) => AddEntry(new DependencyEntry(id, version, null, open ?? throw new ArgumentNullException(nameof(open))));

    /// <summary>
    /// Find an entry by identifier and version
    /// </summary>
    public DependencyEntry? Find(string id, uint version)
    {
        foreach (var entry in _entries)
        {
            if (entry.Version == version && string.Equals(entry.Id, id, StringComparison.Ordinal))
                return entry;
        }

        return null;
    }

    /// <summary>
    /// Find the declared version of a dependency
    /// </summary>
    public uint? FindVersion(string id)
    {
        foreach (var entry in _entries)
        {
            if (string.Equals(entry.Id, id, StringComparison.Ordinal))
                return entry.Version;
        }

        return null;
    }

    /// <inheritdoc />
    public IEnumerator<DependencyEntry> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void AddEntry(DependencyEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Id))
            throw new ArgumentException("Dependency identifier cannot be empty");

        if (Find(entry.Id, entry.Version) != null)
            throw new ArgumentException($"Dependency '{entry.Id}' version {entry.Version} is already registered");

        _entries.Add(entry);
    }
}
