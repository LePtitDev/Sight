using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sight.Serialization.Binary;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;
using Sight.Serialization.Text;

namespace Sight.Serialization.Internal;

/// <summary>
/// Source of a resolved dependency
/// </summary>
internal sealed class DependencySource
{
    public DependencySource(DependencyReference reference, object? value, Func<Stream>? open)
    {
        Reference = reference;
        Value = value;
        Open = open;
    }

    /// <summary>
    /// Identifier and actual version of the dependency
    /// </summary>
    public DependencyReference Reference { get; }

    /// <summary>
    /// Object or document provided in memory
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Function opening the content (binary or text)
    /// </summary>
    public Func<Stream>? Open { get; }
}

/// <summary>
/// Resolution of schemas and dependencies (binary specification §10)
/// </summary>
internal sealed class ResourceResolver
{
    private readonly SerializationOptions _options;
    private readonly Dictionary<(string Name, uint Version), Schema> _schemas = new();
    private readonly object _lock = new();
    private Dictionary<(string Id, uint Version), string>? _index;

    private ResourceResolver(SerializationOptions options)
    {
        _options = options;
    }

    public static ResourceResolver For(SerializationOptions options) => options.GetResolverCache(() => new ResourceResolver(options));

    public Schema ResolveSchema(string name, uint version, ulong? hash)
    {
        Schema schema;
        lock (_lock)
        {
            if (!_schemas.TryGetValue((name, version), out schema!))
            {
                schema = FindSchema(name, version) ?? throw new SerializationException($"Schema '{name}' version {version} not found");
                _schemas[(name, version)] = schema;
            }
        }

        if (hash.HasValue && !ReferenceEquals(schema, Schema.Core) && schema.Hash != hash.Value)
            throw new SerializationException($"Schema '{name}' version {version} does not match the expected hash (0x{hash.Value:X16}, found 0x{schema.Hash:X16})");

        return schema;
    }

    public DependencySource ResolveDependency(string id, uint version)
    {
        var entry = _options.Dependencies.Find(id, version);
        if (entry != null && !entry.IsDeclarationOnly)
            return new DependencySource(new DependencyReference(id, version), entry.Value, entry.Open);

        foreach (var resolver in _options.Resolvers)
        {
            var stream = resolver.Open(id, version);
            if (stream != null)
                return new DependencySource(new DependencyReference(id, version), null, () => stream);
        }

        var index = GetIndex();
        if (index.TryGetValue((id, version), out var path))
            return new DependencySource(new DependencyReference(id, version), null, () => File.OpenRead(path));

        if (_options.AllowNewerDependencies)
        {
            var newerEntry = _options.Dependencies
                .Where(x => !x.IsDeclarationOnly && x.Version > version && string.Equals(x.Id, id, StringComparison.Ordinal))
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            var newerFile = index.Keys
                .Where(x => x.Version > version && string.Equals(x.Id, id, StringComparison.Ordinal))
                .OrderByDescending(x => x.Version)
                .Select(x => (uint?)x.Version)
                .FirstOrDefault();

            if (newerEntry != null && (!newerFile.HasValue || newerEntry.Version >= newerFile.Value))
                return new DependencySource(new DependencyReference(id, newerEntry.Version), newerEntry.Value, newerEntry.Open);

            if (newerFile.HasValue)
            {
                var newerPath = index[(id, newerFile.Value)];
                return new DependencySource(new DependencyReference(id, newerFile.Value), null, () => File.OpenRead(newerPath));
            }
        }

        throw new SerializationException($"Dependency '{id}' version {version} not found");
    }

    private Schema? FindSchema(string name, uint version)
    {
        if (name == "core" && version == 1)
            return Schema.Core;

        foreach (var provided in _options.Schemas)
        {
            foreach (var schema in provided.Closure)
            {
                if (schema.Version == version && string.Equals(schema.Name, name, StringComparison.Ordinal))
                    return schema;
            }
        }

        foreach (var resolver in _options.Resolvers)
        {
            using var stream = resolver.Open(name, version);
            if (stream != null)
                return LoadSchema(stream, name, version);
        }

        if (GetIndex().TryGetValue((name, version), out var path))
        {
            using var stream = File.OpenRead(path);
            return LoadSchema(stream, name, version);
        }

        return null;
    }

    private Schema LoadSchema(Stream stream, string name, uint version)
    {
        var schema = SchemaFiles.Load(stream, _options);
        if (schema.Version != version || !string.Equals(schema.Name, name, StringComparison.Ordinal))
            throw new SerializationException($"Expected schema '{name}' version {version}, found '{schema.Name}' version {schema.Version}");

        return schema;
    }

    private Dictionary<(string Id, uint Version), string> GetIndex()
    {
        lock (_lock)
        {
            return _index ??= BuildIndex();
        }
    }

    private Dictionary<(string Id, uint Version), string> BuildIndex()
    {
        var index = new Dictionary<(string Id, uint Version), string>();
        foreach (var searchPath in _options.SearchPaths)
        {
            if (!Directory.Exists(searchPath))
            {
                _options.OnDiagnostic?.Invoke($"Search path '{searchPath}' does not exist");
                continue;
            }

            var local = new Dictionary<(string Id, uint Version), string>();
            foreach (var file in Directory.EnumerateFiles(searchPath, "*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file);
                if (!string.Equals(extension, ".sbdl", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(extension, ".sbdt", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(extension, ".sgs", StringComparison.OrdinalIgnoreCase))
                    continue;

                (string Id, uint Version)? key;
                try
                {
                    key = ReadIdentity(file);
                }
                catch (Exception e) when (e is SerializationException or IOException)
                {
                    _options.OnDiagnostic?.Invoke($"Cannot index '{file}': {e.Message}");
                    continue;
                }

                if (key == null || key.Value.Id.Length == 0)
                    continue;

                if (local.TryGetValue(key.Value, out var duplicate))
                    throw new SerializationException($"'{key.Value.Id}' version {key.Value.Version} is defined twice in search path '{searchPath}': '{duplicate}' and '{file}'");

                local.Add(key.Value, file);
            }

            foreach (var entry in local)
            {
                if (index.TryGetValue(entry.Key, out var existing))
                {
                    _options.OnDiagnostic?.Invoke($"'{entry.Value}' is shadowed by '{existing}'");
                    continue;
                }

                index.Add(entry.Key, entry.Value);
            }
        }

        return index;
    }

    private static (string Id, uint Version)? ReadIdentity(string path)
    {
        using var stream = File.OpenRead(path);
        if (string.Equals(Path.GetExtension(path), ".sgs", StringComparison.OrdinalIgnoreCase))
            return SchemaFiles.ReadTextIdentity(stream);

        var head = new byte[4];
        var read = stream.Read(head, 0, 4);
        stream.Position = 0;
        if (read == 4 && BinaryFormat.HasMagic(head))
        {
            using var decoder = new BinaryDecoder(stream, 4096);
            var header = BinaryFormat.ReadHeader(decoder);
            return (header.Id, header.Version);
        }

        var textHeader = TextFormat.ReadHeader(stream);
        return (textHeader.Id, textHeader.Version);
    }
}
