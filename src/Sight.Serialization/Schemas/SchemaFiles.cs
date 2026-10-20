using System.IO;
using Sight.Serialization.Binary;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Text;

namespace Sight.Serialization.Schemas;

/// <summary>
/// Compiled schema files (binary specification §4.7) and schema sources
/// </summary>
internal static class SchemaFiles
{
    /// <summary>
    /// Load a schema from a compiled file (binary or text document of root <c>core.Schema</c>) or a schema source
    /// </summary>
    public static Schema Load(Stream stream, SerializationOptions options)
    {
        var data = ReadAll(stream);
        var resolver = ResourceResolver.For(options);
        if (BinaryFormat.HasMagic(data))
        {
            using var memory = new MemoryStream(data, writable: false);
            return LoadDocument(BinaryReading.ReadDocument(memory, options, resolver), resolver);
        }

        var text = Lexer.DecodeText(data, data.Length);
        if (text.StartsWith("@sbdl", System.StringComparison.Ordinal))
            return LoadDocument(TextFormat.Parse(text, options, resolver), resolver);

        // Schema source (.sgs)
        return SchemaParser.Parse(text, (name, version, hash) => resolver.ResolveSchema(name, version, hash));
    }

    public static void Save(Schema schema, Stream stream, SerializationOptions options)
    {
        var document = ToDocument(schema);
        BinaryWriting.WriteDocument(stream, document, options);
    }

    public static DataDocument ToDocument(Schema schema)
    {
        var document = new DataDocument(Schema.Core, SchemaCodec.ToData(schema))
        {
            Id = schema.Name,
            Version = schema.Version,
            SchemaMode = SchemaMode.Reference,
        };

        foreach (var entry in schema.Metadata)
            document.Metadata.Add(entry.Key, entry.Value);

        return document;
    }

    public static (string Id, uint Version) ReadTextIdentity(Stream stream)
    {
        return SchemaParser.ReadIdentity(Lexer.ReadText(stream));
    }

    private static Schema LoadDocument(DataDocument document, ResourceResolver resolver)
    {
        if (!ReferenceEquals(document.Root.Definition, CoreSchema.SchemaType))
            throw new SerializationException("The file is not a compiled schema (root must be core.Schema)");

        var schema = SchemaCodec.FromData(new[] { document.Root }, (name, version, hash) => resolver.ResolveSchema(name, version, hash))[0];
        if (document.Id.Length > 0 && (document.Id != schema.Name || document.Version != schema.Version))
            throw new SerializationException($"Compiled schema header '{document.Id}' {document.Version} does not match schema '{schema.Name}' {schema.Version}");

        foreach (var entry in document.Metadata)
            schema.Metadata[entry.Key] = entry.Value;

        return schema;
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
