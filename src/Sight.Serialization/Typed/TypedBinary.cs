using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sight.Serialization.Binary;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;
using Sight.Serialization.Text;

namespace Sight.Serialization.Typed;

/// <summary>
/// Typed serialization to and from the binary format
/// </summary>
internal static class TypedBinary
{
    public static void Write(Stream stream, object? value, Type staticType, SerializationOptions options, SchemaMode? schemaMode = null)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "The root value cannot be null");

        var types = new List<Type> { value.GetType() };
        if (staticType != typeof(object) && !staticType.IsInterface)
            types.Add(staticType);

        types.AddRange(options.Exports.Values.Select(x => x?.GetType() ?? throw new SerializationException("Exported objects cannot be null")));
        var model = ClrModelBuilder.Get(types);
        var rootInfo = model.GetStruct(value.GetType());
        if (rootInfo.Create == null && !value.GetType().IsValueType)
            throw new SerializationException($"Root type '{value.GetType()}' is abstract");

        var header = new FileHeader
        {
            Id = options.Id,
            Version = options.Version,
            SchemaMode = schemaMode ?? options.SchemaMode,
        };

        foreach (var entry in options.Metadata)
            header.Metadata.Add(entry.Key, entry.Value);

        var tracker = new DependencyTracker(Array.Empty<DependencyReference>(), id => options.Dependencies.FindVersion(id), allowDiscovery: true);
        using var region = BinaryFormat.EncodeRegion(header, GetFileSchemas(model, rootInfo.Definition.Schema), options, (encoder, table) =>
        {
            var writer = new TypedWriter(encoder, table, model, tracker, options.Limits.MaxDepth);
            encoder.WriteVarUInt((ulong)table.IndexOf(rootInfo.Definition));
            writer.WriteExactStruct(rootInfo, value);
            if (options.Exports.Count == 0)
                return false;

            encoder.WriteVarUInt((ulong)options.Exports.Count);
            foreach (var export in options.Exports)
            {
                if (export.Key.Length == 0)
                    throw new SerializationException("Export names cannot be empty");

                var info = model.GetStruct(export.Value.GetType());
                encoder.WriteString(export.Key);
                encoder.WriteVarUInt((ulong)table.IndexOf(info.Definition));
                writer.WriteExport(info, export.Value);
            }

            return true;
        });

        header.Dependencies.AddRange(tracker.Dependencies);
        BinaryFormat.WriteRegion(stream, header, region);
    }

    /// <summary>
    /// File schemas of a model: the schema of the root type, then every other schema of the model not already in the closure
    /// (e.g. extension schemas declaring derived types)
    /// </summary>
    public static IReadOnlyList<Schema> GetFileSchemas(ClrModel model, Schema root)
    {
        var schemas = new List<Schema> { root };
        var closure = new HashSet<string>(root.Closure.Select(x => x.Name), StringComparer.Ordinal);
        foreach (var schema in model.Schemas.Values.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (closure.Contains(schema.Name))
                continue;

            schemas.Add(schema);
            closure.UnionWith(schema.Closure.Select(x => x.Name));
        }

        return schemas;
    }

    public static object? Read(Stream stream, Type type, SerializationOptions options)
    {
        return ReadWithExports(stream, type, options, readRoot: true).Root;
    }

    /// <summary>
    /// Read the root value (unless <paramref name="readRoot"/> is false) and the exports whose type is mapped
    /// </summary>
    public static LoadedFile ReadWithExports(Stream stream, Type type, SerializationOptions options, bool readRoot)
    {
        var model = ClrModelBuilder.Get(new[] { type });
        var resolver = ResourceResolver.For(options);
        var context = DependencyContext.For(options);
        return BinaryFormat.ReadFile(stream, options, resolver, (decoder, header, schema, table) =>
        {
            var reader = new TypedReader(decoder, table, model, header.Dependencies, context, options.Limits);
            var rootType = table.GetStruct(decoder.ReadVarUInt());
            object? root = null;
            if (readRoot)
                root = reader.ReadExactStruct(rootType, type);
            else
                reader.SkipExactStruct(rootType);

            var exports = new Dictionary<string, object?>(StringComparer.Ordinal);
            if (header.HasExports)
            {
                var count = decoder.ReadVarUInt();
                if (count == 0 || count > (ulong)options.Limits.MaxCollectionCount)
                    throw new SerializationException($"Invalid number of exports {count}");

                for (var i = 0UL; i < count; i++)
                {
                    var name = decoder.ReadString();
                    if (name.Length == 0 || exports.ContainsKey(name))
                        throw new SerializationException($"Invalid or duplicate export name '{name}'");

                    exports.Add(name, reader.ReadExport(table.GetStruct(decoder.ReadVarUInt())));
                }
            }

            return new LoadedFile(root, exports);
        });
    }
}

/// <summary>
/// Root value and exports of a typed file
/// </summary>
internal sealed class LoadedFile
{
    public LoadedFile(object? root, Dictionary<string, object?> exports)
    {
        Root = root;
        Exports = exports;
    }

    public object? Root { get; }

    public Dictionary<string, object?> Exports { get; }
}

/// <summary>
/// Creation of <see cref="ExternalRef{T}"/> instances for a runtime type
/// </summary>
internal static class ExternalRefFactory
{
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> s_constructors = new();

    public static object Create(Type target, string id, string key, uint version, DependencyContext context)
    {
        var constructor = s_constructors.GetOrAdd(target, t => typeof(ExternalRef<>).MakeGenericType(t).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(string), typeof(uint), typeof(DependencyContext) },
            null)!);

        return constructor.Invoke(new object[] { id, key, version, context });
    }
}

/// <summary>
/// Resolution and cache of dependencies targeted by external references
/// </summary>
internal sealed class DependencyContext
{
    private readonly SerializationOptions _options;
    private readonly Dictionary<(string Id, uint Version, Type Type, bool Root), LoadedFile> _files = new();
    private readonly object _lock = new();

    private DependencyContext(SerializationOptions options)
    {
        _options = options;
    }

    public static DependencyContext For(SerializationOptions options) => options.GetDependencyCache(() => new DependencyContext(options));

    public T Resolve<T>(string id, uint version, string key)
    {
        var source = ResourceResolver.For(_options).ResolveDependency(id, version);
        if (source.Value != null && source.Value is not DataDocument)
        {
            if (key.Length > 0)
                throw new SerializationException($"Export '{key}' of dependency '{id}' cannot be resolved: the dependency is provided as an object");

            return source.Value is T value ? value : throw new SerializationException($"Dependency '{id}' is a '{source.Value.GetType()}', not a '{typeof(T)}'");
        }

        LoadedFile file;
        var cacheKey = (source.Reference.Id, source.Reference.Version, typeof(T), key.Length == 0);
        lock (_lock)
        {
            if (!_files.TryGetValue(cacheKey, out file!))
            {
                file = Load(source, typeof(T), key.Length == 0);
                _files.Add(cacheKey, file);
            }
        }

        object? result;
        if (key.Length == 0)
            result = file.Root;
        else if (!file.Exports.TryGetValue(key, out result))
            throw new SerializationException($"Dependency '{id}' has no export '{key}'");

        return result is T typed ? typed : throw new SerializationException($"Value '{key}' of dependency '{id}' is not a '{typeof(T)}'");
    }

    private LoadedFile Load(DependencySource source, Type type, bool readRoot)
    {
        byte[] binary;
        if (source.Value is DataDocument document)
        {
            binary = BinarySerializer.Write(document, _options);
        }
        else
        {
            using var stream = source.Open!();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var data = memory.ToArray();
            binary = BinaryFormat.HasMagic(data)
                ? data
                : BinarySerializer.Write(TextFormat.Parse(Lexer.DecodeText(data, data.Length), _options, ResourceResolver.For(_options)), _options);
        }

        using var input = new MemoryStream(binary, writable: false);
        return TypedBinary.ReadWithExports(input, type, _options, readRoot);
    }
}
