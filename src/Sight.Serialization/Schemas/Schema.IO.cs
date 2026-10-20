using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sight.Serialization.Internal;
using Sight.Serialization.Text;

namespace Sight.Serialization.Schemas;

public sealed partial class Schema
{
    /// <summary>
    /// Schema derived from a CLR type and the types it uses
    /// </summary>
    public static Schema FromType<T>() => FromType(typeof(T));

    /// <summary>
    /// Schema derived from a CLR type and the types it uses
    /// </summary>
    public static Schema FromType(Type type)
    {
        var model = Typed.ClrModelBuilder.Get(new[] { type });
        if (type.IsEnum)
            return model.Enums[type].Definition.Schema;

        return model.GetStruct(type).Definition.Schema;
    }

    /// <summary>
    /// Parse a schema written in the schema language; imports are resolved with the options
    /// </summary>
    public static Schema Parse(string text, SerializationOptions? options = null)
    {
        var resolver = ResourceResolver.For(options ?? SerializationOptions.Default);
        return SchemaParser.Parse(text, (name, version, hash) => resolver.ResolveSchema(name, version, hash));
    }

    /// <summary>
    /// Parse a schema written in the schema language (UTF-8 stream)
    /// </summary>
    public static Schema Parse(Stream stream, SerializationOptions? options = null) => Parse(Lexer.ReadText(stream), options);

    /// <summary>
    /// Parse a schema written in the schema language
    /// </summary>
    public static Schema Parse(TextReader reader, SerializationOptions? options = null) => Parse(reader.ReadToEnd(), options);

    /// <summary>
    /// Parse a schema written in the schema language (UTF-8 stream)
    /// </summary>
    public static async Task<Schema> ParseAsync(Stream stream, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var data = await StreamHelper.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        return Parse(Lexer.DecodeText(data, data.Length), options);
    }

    /// <summary>
    /// Load a compiled schema (<c>.sbdl</c>), or parse a schema source
    /// </summary>
    public static Schema Load(Stream stream, SerializationOptions? options = null)
    {
        return SchemaFiles.Load(stream, options ?? SerializationOptions.Default);
    }

    /// <summary>
    /// Load a compiled schema (<c>.sbdl</c>), or parse a schema source
    /// </summary>
    public static async Task<Schema> LoadAsync(Stream stream, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var data = await StreamHelper.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        using var memory = new MemoryStream(data, writable: false);
        return Load(memory, options);
    }

    /// <summary>
    /// Save the schema as a compiled schema file (<c>.sbdl</c>)
    /// </summary>
    public void Save(Stream stream, SerializationOptions? options = null)
    {
        SchemaFiles.Save(this, stream, options ?? SerializationOptions.Default);
    }

    /// <summary>
    /// Save the schema as a compiled schema file (<c>.sbdl</c>)
    /// </summary>
    public async Task SaveAsync(Stream stream, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        Save(memory, options);
        memory.Position = 0;
        await memory.CopyToAsync(stream, 81920, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Compile a schema source into a compiled schema file
    /// </summary>
    public static void Compile(Stream source, Stream output, SerializationOptions? options = null)
    {
        Parse(source, options).Save(output, options);
    }

    /// <summary>
    /// Compile a schema source into a compiled schema file
    /// </summary>
    public static async Task CompileAsync(Stream source, Stream output, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var schema = await ParseAsync(source, options, cancellationToken).ConfigureAwait(false);
        await schema.SaveAsync(output, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Text of the schema in the schema language (canonical form)
    /// </summary>
    public string ToText()
    {
        Validate();
        return SchemaPrinter.Print(this);
    }

    /// <summary>
    /// Write the schema in the schema language (canonical form)
    /// </summary>
    public void ToText(TextWriter writer) => writer.Write(ToText());

    /// <summary>
    /// Write the schema in the schema language (canonical form, UTF-8 without BOM)
    /// </summary>
    public void ToText(Stream stream)
    {
        var bytes = Utf8.Strict.GetBytes(ToText());
        stream.Write(bytes, 0, bytes.Length);
    }
}
