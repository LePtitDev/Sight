using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Binary;

/// <summary>
/// Header of an SBDL file
/// </summary>
internal sealed class FileHeader
{
    public string Id { get; set; } = string.Empty;

    public uint Version { get; set; }

    public MetadataCollection Metadata { get; } = new();

    public List<DependencyReference> Dependencies { get; } = new();

    public SchemaMode SchemaMode { get; set; }

    public Compression Compression { get; set; }

    public byte DedupMaxLength { get; set; }

    public bool HasExports { get; set; }
}

/// <summary>
/// Dependencies of a file being written: declared ones first, then discovered ones
/// </summary>
internal sealed class DependencyTracker
{
    private readonly Dictionary<string, int> _indexes = new(StringComparer.Ordinal);
    private readonly Func<string, uint?> _findVersion;
    private readonly bool _allowDiscovery;

    public DependencyTracker(IEnumerable<DependencyReference> declared, Func<string, uint?> findVersion, bool allowDiscovery)
    {
        _findVersion = findVersion;
        _allowDiscovery = allowDiscovery;
        foreach (var dependency in declared)
            Add(dependency);
    }

    public List<DependencyReference> Dependencies { get; } = new();

    public int GetIndex(string id, uint? version)
    {
        if (_indexes.TryGetValue(id, out var index))
        {
            if (version.HasValue && Dependencies[index].Version != version.Value)
                throw new SerializationException($"Dependency '{id}' is referenced with versions {Dependencies[index].Version} and {version.Value}");

            return index;
        }

        if (!_allowDiscovery)
            throw new SerializationException($"Dependency '{id}' is not declared");

        var resolved = version ?? _findVersion(id) ?? throw new SerializationException($"Dependency '{id}' is not declared and has no version");
        Add(new DependencyReference(id, resolved));
        return Dependencies.Count - 1;
    }

    private void Add(DependencyReference dependency)
    {
        if (string.IsNullOrEmpty(dependency.Id))
            throw new SerializationException("Dependency identifier cannot be empty");

        if (_indexes.ContainsKey(dependency.Id))
            throw new SerializationException($"Duplicate dependency '{dependency.Id}'");

        _indexes.Add(dependency.Id, Dependencies.Count);
        Dependencies.Add(dependency);
    }
}

/// <summary>
/// Read and write SBDL binary files (header, schema section, body)
/// </summary>
internal static class BinaryFormat
{
    public const byte FormatVersion = 1;

    private static readonly byte[] s_magic = { 0x53, 0x42, 0x44, 0x4C };

    /// <summary>
    /// Indicates whether data starts with the binary magic number
    /// </summary>
    public static bool HasMagic(ReadOnlySpan<byte> data) => data.Length >= 4 && data.Slice(0, 4).SequenceEqual(s_magic);

    public static FileHeader ReadHeader(BinaryDecoder decoder)
    {
        Span<byte> magic = stackalloc byte[4];
        decoder.ReadBytes(magic);
        if (!magic.SequenceEqual(s_magic))
            throw new SerializationException("Invalid SBDL file: wrong magic number");

        var format = decoder.ReadByte();
        if (format != FormatVersion)
            throw new SerializationException($"Unsupported SBDL format version {format}");

        var flags = decoder.ReadByte();
        var schemaMode = flags & 0x03;
        var compression = (flags >> 4) & 0x03;
        if (schemaMode == 3 || compression > 1 || (flags & 0x80) != 0)
            throw new SerializationException($"Invalid SBDL header flags 0x{flags:X2}");

        var header = new FileHeader
        {
            SchemaMode = (SchemaMode)schemaMode,
            Compression = (Compression)compression,
            HasExports = (flags & 0x40) != 0,
            DedupMaxLength = decoder.ReadByte(),
            Id = decoder.ReadString(),
        };

        var version = decoder.ReadVarUInt();
        header.Version = version <= uint.MaxValue ? (uint)version : throw new SerializationException($"Invalid content version {version}");

        if ((flags & 0x04) != 0)
        {
            var count = ReadNonEmptyCount(decoder, "metadata");
            for (var i = 0; i < count; i++)
            {
                var key = decoder.ReadString();
                if (header.Metadata.TryGetValue(key, out _))
                    throw new SerializationException($"Duplicate metadata key '{key}'");

                header.Metadata.Add(key, decoder.ReadString());
            }
        }

        if ((flags & 0x08) != 0)
        {
            var count = ReadNonEmptyCount(decoder, "dependencies");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < count; i++)
            {
                var id = decoder.ReadString();
                if (id.Length == 0 || !ids.Add(id))
                    throw new SerializationException($"Invalid or duplicate dependency '{id}'");

                var dependencyVersion = decoder.ReadVarUInt();
                if (dependencyVersion > uint.MaxValue)
                    throw new SerializationException($"Invalid dependency version {dependencyVersion}");

                header.Dependencies.Add(new DependencyReference(id, (uint)dependencyVersion));
            }
        }

        return header;
    }

    public static void WriteHeader(BinaryEncoder encoder, FileHeader header)
    {
        encoder.WriteBytes(s_magic);
        encoder.WriteByte(FormatVersion);
        var flags = (byte)header.SchemaMode;
        if (header.Metadata.Count > 0)
            flags |= 0x04;

        if (header.Dependencies.Count > 0)
            flags |= 0x08;

        flags |= (byte)((byte)header.Compression << 4);
        if (header.HasExports)
            flags |= 0x40;

        encoder.WriteByte(flags);
        encoder.WriteByte(header.DedupMaxLength);
        encoder.WriteString(header.Id);
        encoder.WriteVarUInt(header.Version);
        if (header.Metadata.Count > 0)
        {
            encoder.WriteVarUInt((ulong)header.Metadata.Count);
            foreach (var entry in header.Metadata)
            {
                encoder.WriteString(entry.Key);
                encoder.WriteString(entry.Value);
            }
        }

        if (header.Dependencies.Count > 0)
        {
            encoder.WriteVarUInt((ulong)header.Dependencies.Count);
            foreach (var dependency in header.Dependencies)
            {
                encoder.WriteString(dependency.Id);
                encoder.WriteVarUInt(dependency.Version);
            }
        }
    }

    /// <summary>
    /// Write a complete file: <paramref name="writeBody"/> writes the root type index, root value and exports
    /// </summary>
    public static void WriteFile(Stream output, FileHeader header, IReadOnlyList<Schema> schemas, SerializationOptions options, Func<BinaryEncoder, TypeTable, bool> writeBody)
    {
        using var region = EncodeRegion(header, schemas, options, writeBody);
        WriteRegion(output, header, region);
    }

    /// <summary>
    /// Encode schema section and body into a buffer; header flags are updated
    /// </summary>
    public static BinaryEncoder EncodeRegion(FileHeader header, IReadOnlyList<Schema> schemas, SerializationOptions options, Func<BinaryEncoder, TypeTable, bool> writeBody)
    {
        if (schemas.Count == 0)
            throw new SerializationException("A file requires at least one schema");

        foreach (var schema in schemas)
            schema.Validate();

        header.DedupMaxLength = options.StringDedupMaxLength;
        header.Compression = options.Compression;
        var table = TypeTable.For(schemas);
        var region = new BinaryEncoder { DedupMaxLength = header.DedupMaxLength };
        try
        {
            WriteSchemaSection(region, header.SchemaMode, schemas, table);
            header.HasExports = writeBody(region, table);
            return region;
        }
        catch
        {
            region.Dispose();
            throw;
        }
    }

    public static void WriteRegion(Stream output, FileHeader header, BinaryEncoder region)
    {
        using (var headerEncoder = new BinaryEncoder(256))
        {
            WriteHeader(headerEncoder, header);
            headerEncoder.CopyTo(output);
        }

        if (header.Compression == Compression.Deflate)
        {
            using var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true);
            region.CopyTo(deflate);
        }
        else
        {
            region.CopyTo(output);
        }
    }

    public static byte[] Compress(BinaryEncoder region)
    {
        using var memory = new MemoryStream();
        using (var deflate = new DeflateStream(memory, CompressionLevel.Optimal, leaveOpen: true))
            region.CopyTo(deflate);

        return memory.ToArray();
    }

    /// <summary>
    /// Read a complete file: <paramref name="readBody"/> reads the root type index, root value and exports
    /// </summary>
    public static T ReadFile<T>(Stream input, SerializationOptions options, ResourceResolver resolver, Func<BinaryDecoder, FileHeader, IReadOnlyList<Schema>, TypeTable, T> readBody)
    {
        using var headerDecoder = new BinaryDecoder(input) { MaxStringLength = options.Limits.MaxStringLength };
        var header = ReadHeader(headerDecoder);
        if (header.Compression == Compression.Deflate)
        {
            using var deflate = new DeflateStream(headerDecoder.GetRemainingStream(), System.IO.Compression.CompressionMode.Decompress);
            using var limited = new LimitedStream(deflate, options.Limits.MaxDecompressedSize);
            using var decoder = new BinaryDecoder(limited) { DedupMaxLength = header.DedupMaxLength, MaxStringLength = options.Limits.MaxStringLength };
            return ReadRegion(decoder, header, options, resolver, readBody);
        }

        headerDecoder.DedupMaxLength = header.DedupMaxLength;
        return ReadRegion(headerDecoder, header, options, resolver, readBody);
    }

    private static T ReadRegion<T>(BinaryDecoder decoder, FileHeader header, SerializationOptions options, ResourceResolver resolver, Func<BinaryDecoder, FileHeader, IReadOnlyList<Schema>, TypeTable, T> readBody)
    {
        var schemas = ReadSchemaSection(decoder, header.SchemaMode, options, resolver, out var shared);

        // Inline schemas decoded for this file only: their table is not worth sharing
        var table = shared ? TypeTable.For(schemas) : new TypeTable(Schema.ClosureOf(schemas));
        var result = readBody(decoder, header, schemas, table);
        if (!decoder.IsAtEnd)
            throw new SerializationException($"Invalid SBDL file: unexpected data after the body at position {decoder.Position}");

        return result;
    }

    private static void WriteSchemaSection(BinaryEncoder encoder, SchemaMode mode, IReadOnlyList<Schema> schemas, TypeTable table)
    {
        switch (mode)
        {
            case SchemaMode.Inline:
                // The section starts the region: its encoding only depends on the table and is reused
                var section = table.GetInlineSection(encoder.DedupMaxLength, dedupMaxLength => EncodeInlineSection(table, dedupMaxLength));
                encoder.WriteBytes(section.Bytes);
                encoder.AddDedupStrings(section.Strings);
                break;
            case SchemaMode.Reference:
                var names = new HashSet<string>(StringComparer.Ordinal);
                encoder.WriteVarUInt((ulong)schemas.Count);
                foreach (var schema in schemas)
                {
                    if (!names.Add(schema.Name))
                        throw new SerializationException($"Schema '{schema.Name}' is listed twice");

                    encoder.WriteString(schema.Name);
                    encoder.WriteVarUInt(schema.Version);
                    encoder.WriteFixedUInt64(schema.Hash);
                }

                break;
            case SchemaMode.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private static InlineSchemaSection EncodeInlineSection(TypeTable table, byte dedupMaxLength)
    {
        using var encoder = new BinaryEncoder { DedupMaxLength = dedupMaxLength };
        var closure = table.Closure;
        encoder.WriteVarUInt((ulong)closure.Count);
        var writer = new DataBinaryWriter(encoder, CoreSchema.Table, null, int.MaxValue);
        foreach (var item in closure)
            writer.WriteExactStruct(CoreSchema.SchemaType, SchemaCodec.ToData(item), "schema");

        return new InlineSchemaSection(encoder.WrittenSpan.ToArray(), encoder.GetDedupStrings());
    }

    /// <summary>
    /// Read the schema section; <paramref name="shared"/> is false if the schemas were decoded for this file only
    /// </summary>
    private static IReadOnlyList<Schema> ReadSchemaSection(BinaryDecoder decoder, SchemaMode mode, SerializationOptions options, ResourceResolver resolver, out bool shared)
    {
        shared = true;
        switch (mode)
        {
            case SchemaMode.Inline:
                var cached = InlineSchemaCache.TryRead(decoder, options.Limits);
                if (cached != null)
                    return cached;

                shared = false;
                var start = decoder.Position;
                var cacheable = decoder.DedupStringCount == 0;
                var count = ReadNonEmptyCount(decoder, "schemas");
                var reader = new DataBinaryReader(decoder, CoreSchema.Table, null, options.Limits);
                var values = new List<DataStruct>();
                for (var i = 0; i < count; i++)
                    values.Add(reader.ReadExactStruct(CoreSchema.SchemaType));

                var schemas = SchemaCodec.FromData(values, (name, version, _) => resolver.ResolveSchema(name, version, null));
                var closure = Schema.ClosureOf(schemas);
                if (closure.Length != schemas.Count || closure.Where((x, i) => !ReferenceEquals(x, schemas[i])).Any())
                    throw new SerializationException("Inline schemas must be a closed list in closure order");

                if (cacheable)
                    InlineSchemaCache.Add(decoder, start, options.Limits, schemas);

                return schemas;
            case SchemaMode.Reference:
                var referenceCount = ReadNonEmptyCount(decoder, "schemas");
                var resolved = new List<Schema>();
                for (var i = 0; i < referenceCount; i++)
                {
                    var schemaName = decoder.ReadString();
                    var schemaVersion = decoder.ReadVarUInt();
                    if (schemaVersion > uint.MaxValue)
                        throw new SerializationException($"Invalid schema version {schemaVersion}");

                    var hash = decoder.ReadFixedUInt64();
                    if (resolved.Any(x => x.Name == schemaName))
                        throw new SerializationException($"Schema '{schemaName}' is listed twice");

                    resolved.Add(resolver.ResolveSchema(schemaName, (uint)schemaVersion, hash));
                }

                return resolved;
            default:
                return options.Schemas.Count > 0
                    ? options.Schemas
                    : throw new SerializationException("The file has no schema: the schemas must be provided with SerializationOptions.Schemas");
        }
    }

    private static int ReadNonEmptyCount(BinaryDecoder decoder, string what)
    {
        var count = decoder.ReadVarUInt();
        if (count == 0 || count > int.MaxValue)
            throw new SerializationException($"Invalid number of {what}: {count}");

        return (int)count;
    }
}
