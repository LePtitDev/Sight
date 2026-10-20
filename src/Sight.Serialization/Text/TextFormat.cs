using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sight.Serialization.Binary;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Text;

/// <summary>
/// Read and write SBDT text documents
/// </summary>
internal static partial class TextFormat
{
    public const uint FormatVersion = 1;

    /// <summary>
    /// Read only the header directives (identifier and version) of a text document
    /// </summary>
    public static FileHeader ReadHeader(Stream stream)
    {
        var text = Lexer.ReadText(stream);
        var reader = new TokenReader(Lexer.Tokenize(text));
        var header = new FileHeader();
        ReadFormatDirective(reader);
        while (reader.Current.Kind == TokenKind.Directive && reader.Current.AfterNewLine)
        {
            var directive = reader.Next();
            switch (directive.Text)
            {
                case "id":
                    header.Id = ExpectName(reader, qualified: true);
                    break;
                case "version":
                    header.Version = reader.ExpectUInt32();
                    break;
                default:
                    return header;
            }
        }

        return header;
    }

    public static DataDocument Parse(string text, SerializationOptions options, ResourceResolver resolver)
    {
        var reader = new TokenReader(Lexer.Tokenize(text));
        ReadFormatDirective(reader);

        string? id = null;
        uint? version = null;
        var metadata = new MetadataCollection();
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        var dependencies = new List<DependencyReference>();
        var schemaReferences = new List<(string Name, uint Version, ulong? Hash, Token Token)>();
        var inlineBlocks = new List<IReadOnlyList<Token>>();

        while (reader.Current.Kind == TokenKind.Directive)
        {
            var directive = reader.Current;
            if (!directive.AfterNewLine)
                throw reader.Error("A directive must start at the beginning of a line");

            if (directive.Text == "export")
                break;

            reader.Next();
            switch (directive.Text)
            {
                case "id":
                    if (id != null)
                        throw TokenReader.Error(directive, "Duplicate @id directive");

                    id = ExpectName(reader, qualified: true);
                    break;
                case "version":
                    if (version != null)
                        throw TokenReader.Error(directive, "Duplicate @version directive");

                    version = reader.ExpectUInt32();
                    break;
                case "meta":
                    var key = ExpectName(reader, qualified: false);
                    reader.Expect('=');
                    var value = ExpectString(reader);
                    if (metadata.TryGetValue(key, out _))
                        throw TokenReader.Error(directive, $"Duplicate metadata key '{key}'");

                    metadata.Add(key, value);
                    break;
                case "schema":
                    if (reader.Current.Is('{'))
                    {
                        if (schemaReferences.Count > 0)
                            throw TokenReader.Error(directive, "Reference and inline @schema directives cannot be mixed");

                        inlineBlocks.Add(ReadBlock(reader));
                    }
                    else
                    {
                        if (inlineBlocks.Count > 0)
                            throw TokenReader.Error(directive, "Reference and inline @schema directives cannot be mixed");

                        var name = reader.ExpectQualifiedName();
                        var schemaVersion = reader.ExpectUInt32();
                        ulong? hash = null;
                        if (reader.Current.Kind == TokenKind.Number && !reader.Current.AfterNewLine)
                            hash = ExpectHash(reader);

                        if (schemaReferences.Any(x => x.Name == name))
                            throw TokenReader.Error(directive, $"Schema '{name}' is listed twice");

                        schemaReferences.Add((name, schemaVersion, hash, directive));
                    }

                    break;
                case "dep":
                    var aliasToken = reader.ExpectIdentifier();
                    reader.Expect('=');
                    var dependencyId = reader.Current.Kind == TokenKind.String ? reader.Next().Value! : reader.ExpectQualifiedName();
                    var dependencyVersion = reader.ExpectUInt32();
                    if (dependencyId.Length == 0)
                        throw TokenReader.Error(aliasToken, "Dependency identifier cannot be empty");

                    if (aliases.ContainsKey(aliasToken.Text) || dependencies.Any(x => x.Id == dependencyId))
                        throw TokenReader.Error(aliasToken, "Duplicate dependency alias or identifier");

                    aliases.Add(aliasToken.Text, dependencyId);
                    dependencies.Add(new DependencyReference(dependencyId, dependencyVersion));
                    break;
                case "sbdl":
                    throw TokenReader.Error(directive, "Duplicate @sbdl directive");
                default:
                    throw TokenReader.Error(directive, $"Unknown directive '@{directive.Text}'");
            }

            if (!reader.Current.AfterNewLine && !reader.IsAtEnd)
                throw reader.Error("Expected a line break after the directive");
        }

        IReadOnlyList<Schema> schemas;
        SchemaMode mode;
        if (inlineBlocks.Count > 0)
        {
            schemas = ParseInlineSchemas(inlineBlocks, resolver);
            mode = SchemaMode.Inline;
        }
        else if (schemaReferences.Count > 0)
        {
            var resolved = new List<Schema>();
            foreach (var reference in schemaReferences)
            {
                try
                {
                    resolved.Add(resolver.ResolveSchema(reference.Name, reference.Version, reference.Hash));
                }
                catch (SerializationException e) when (e.Line == 0)
                {
                    throw TokenReader.Error(reference.Token, e.Message);
                }
            }

            schemas = resolved;
            mode = SchemaMode.Reference;
        }
        else
        {
            schemas = options.Schemas.Count > 0
                ? options.Schemas
                : throw reader.Error("The document has no @schema directive: the schemas must be provided with SerializationOptions.Schemas");
            mode = SchemaMode.None;
        }

        var table = TypeTable.For(schemas);
        var binder = new TextBinder(table, aliases, options.Limits);
        if (reader.IsAtEnd)
            throw reader.Error("Missing root value");

        var root = binder.BindTopLevel(ValueParser.ParseValue(reader));
        var document = new DataDocument(schemas, root)
        {
            Id = id ?? string.Empty,
            Version = version ?? 0,
            SchemaMode = mode,
        };

        foreach (var entry in metadata)
            document.Metadata.Add(entry.Key, entry.Value);

        document.Dependencies.AddRange(dependencies);

        var exportNames = new HashSet<string>(StringComparer.Ordinal);
        while (!reader.IsAtEnd)
        {
            var directive = reader.Current;
            if (directive.Kind != TokenKind.Directive || directive.Text != "export" || !directive.AfterNewLine)
                throw reader.Error($"Unexpected {directive} after the root value");

            reader.Next();
            var name = ExpectName(reader, qualified: false);
            if (name.Length == 0 || !exportNames.Add(name))
                throw TokenReader.Error(directive, $"Invalid or duplicate export name '{name}'");

            reader.Expect('=');
            var node = ValueParser.ParseValue(reader);
            var index = document.Exports.Count;
            if (node is ReferenceNode reference)
            {
                document.Exports.Add(new KeyValuePair<string, DataStruct>(name, root));
                binder.BindExportReference(reference, x => document.Exports[index] = new KeyValuePair<string, DataStruct>(name, x));
            }
            else
            {
                document.Exports.Add(new KeyValuePair<string, DataStruct>(name, binder.BindTopLevel(node)));
            }

            if (!reader.Current.AfterNewLine && !reader.IsAtEnd)
                throw reader.Error("Expected a line break after the directive");
        }

        binder.ResolvePending();
        return document;
    }

    private static void ReadFormatDirective(TokenReader reader)
    {
        var first = reader.Current;
        if (first.Kind != TokenKind.Directive || first.Text != "sbdl" || first.Line != 1 || first.Column != 1)
            throw TokenReader.Error(first, "A text document must start with '@sbdl'");

        reader.Next();
        var version = reader.ExpectUInt32();
        if (version != FormatVersion)
            throw TokenReader.Error(first, $"Unsupported text format version {version}");
    }

    private static IReadOnlyList<Schema> ParseInlineSchemas(List<IReadOnlyList<Token>> blocks, ResourceResolver resolver)
    {
        var identities = blocks.Select(SchemaParser.ReadBlockName).ToList();
        for (var i = 0; i < identities.Count; i++)
        {
            if (identities.IndexOf(identities[i]) != i)
                throw TokenReader.Error(blocks[i][0], $"Schema '{identities[i]}' is defined twice");
        }

        var built = new Dictionary<int, Schema>();
        var building = new HashSet<int>();
        return identities.Select((_, i) => Build(i)).ToArray();

        Schema Build(int index)
        {
            if (built.TryGetValue(index, out var existing))
                return existing;

            if (!building.Add(index))
                throw new SerializationException($"Cyclic import on schema '{identities[index]}'");

            var reader = new TokenReader(blocks[index]);
            var schema = SchemaParser.Parse(reader, (name, version, hash) =>
            {
                var inline = identities.IndexOf(name);
                if (inline < 0)
                    return resolver.ResolveSchema(name, version, hash);

                var import = Build(inline);
                if (import.Version != version)
                    throw new SerializationException($"Schema '{name}' version {version} is imported, but version {import.Version} is provided");

                return import;
            }, isFile: false);

            if (!reader.IsAtEnd)
                throw reader.Error($"Unexpected {reader.Current}");

            built.Add(index, schema);
            return schema;
        }
    }

    /// <summary>
    /// Read the tokens of a balanced '{ ... }' block, braces excluded, followed by an end-of-file token
    /// </summary>
    private static IReadOnlyList<Token> ReadBlock(TokenReader reader)
    {
        var open = reader.Expect('{');
        var tokens = new List<Token>();
        var depth = 1;
        while (true)
        {
            var token = reader.Current;
            if (token.Kind == TokenKind.EndOfFile)
                throw TokenReader.Error(open, "Unterminated '@schema' block");

            reader.Next();
            if (token.Is('{'))
            {
                depth++;
            }
            else if (token.Is('}') && --depth == 0)
            {
                tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, token.Line, token.Column, token.AfterNewLine));
                return tokens;
            }

            tokens.Add(token);
        }
    }

    private static string ExpectString(TokenReader reader)
    {
        if (reader.Current.Kind != TokenKind.String)
            throw reader.Error($"Expected a string, found {reader.Current}");

        return reader.Next().Value!;
    }

    private static string ExpectName(TokenReader reader, bool qualified)
    {
        if (reader.Current.Kind == TokenKind.String)
            return reader.Next().Value!;

        return qualified ? reader.ExpectQualifiedName() : reader.ExpectIdentifier().Text;
    }

    private static ulong ExpectHash(TokenReader reader)
    {
        var token = reader.Next();
        var number = NumberLiteral.Parse(token);
        if (!token.Text.StartsWith("0x", StringComparison.Ordinal) || number.Suffix.HasValue)
            throw TokenReader.Error(token, "Expected a hexadecimal hash");

        return number.Magnitude;
    }
}
