using System.Collections.Generic;
using System.IO;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Binary;

/// <summary>
/// Write untyped documents to the binary format
/// </summary>
internal static class BinaryWriting
{
    public static void WriteDocument(Stream output, DataDocument document, SerializationOptions options)
    {
        var header = CreateHeader(document);
        using var region = EncodeDocument(header, document, options);
        BinaryFormat.WriteRegion(output, header, region);
    }

    public static FileHeader CreateHeader(DataDocument document)
    {
        var header = new FileHeader
        {
            Id = document.Id,
            Version = document.Version,
            SchemaMode = document.SchemaMode,
        };

        foreach (var entry in document.Metadata)
            header.Metadata.Add(entry.Key, entry.Value);

        return header;
    }

    public static BinaryEncoder EncodeDocument(FileHeader header, DataDocument document, SerializationOptions options)
    {
        var tracker = new DependencyTracker(document.Dependencies, _ => null, allowDiscovery: false);
        var region = BinaryFormat.EncodeRegion(header, document.Schemas, options, (encoder, table) =>
        {
            var writer = new DataBinaryWriter(encoder, table, x => tracker.GetIndex(x.DependencyId, null), options.Limits.MaxDepth);
            var root = document.Root;
            encoder.WriteVarUInt((ulong)table.IndexOf(root.Definition));
            writer.WriteExactStruct(root.Definition, root, "root");
            if (document.Exports.Count == 0)
                return false;

            var names = new HashSet<string>(System.StringComparer.Ordinal);
            encoder.WriteVarUInt((ulong)document.Exports.Count);
            foreach (var export in document.Exports)
            {
                if (export.Key.Length == 0 || !names.Add(export.Key))
                    throw new SerializationException($"Invalid or duplicate export name '{export.Key}'");

                encoder.WriteString(export.Key);
                encoder.WriteVarUInt((ulong)table.IndexOf(export.Value.Definition));
                writer.WriteExport(export.Value, "export " + export.Key);
            }

            return true;
        });

        header.Dependencies.AddRange(tracker.Dependencies);
        return region;
    }
}

/// <summary>
/// Read untyped documents from the binary format
/// </summary>
internal static class BinaryReading
{
    public static DataDocument ReadDocument(Stream input, SerializationOptions options, ResourceResolver resolver)
    {
        return BinaryFormat.ReadFile(input, options, resolver, (decoder, header, schemas, table) =>
        {
            var reader = new DataBinaryReader(decoder, table, header.Dependencies, options.Limits);
            var rootType = table.GetStruct(decoder.ReadVarUInt());
            var root = reader.ReadExactStruct(rootType);
            var document = new DataDocument(schemas, root)
            {
                Id = header.Id,
                Version = header.Version,
                SchemaMode = header.SchemaMode,
            };

            foreach (var entry in header.Metadata)
                document.Metadata.Add(entry.Key, entry.Value);

            document.Dependencies.AddRange(header.Dependencies);
            if (header.HasExports)
            {
                var count = decoder.ReadVarUInt();
                if (count == 0 || count > (ulong)options.Limits.MaxCollectionCount)
                    throw new SerializationException($"Invalid number of exports {count}");

                var names = new HashSet<string>(System.StringComparer.Ordinal);
                for (var i = 0UL; i < count; i++)
                {
                    var name = decoder.ReadString();
                    if (name.Length == 0 || !names.Add(name))
                        throw new SerializationException($"Invalid or duplicate export name '{name}'");

                    var type = table.GetStruct(decoder.ReadVarUInt());
                    document.Exports.Add(new KeyValuePair<string, DataStruct>(name, reader.ReadExport(type)));
                }
            }

            return document;
        });
    }
}
