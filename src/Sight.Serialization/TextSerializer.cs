using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Text;
using Sight.Serialization.Typed;

namespace Sight.Serialization;

/// <summary>
/// Serialization to and from the SBDT text format
/// </summary>
public static class TextSerializer
{
    /// <summary>
    /// Write an object; the schema is derived from its type
    /// </summary>
    public static void Write<T>(Stream stream, T value, SerializationOptions? options = null)
    {
        var bytes = Utf8.Strict.GetBytes(Write(value, options));
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Write an object into a string
    /// </summary>
    public static string Write<T>(T value, SerializationOptions? options = null)
    {
        options ??= SerializationOptions.Default;

        // The typed text path goes through the binary encoding, which guarantees identical semantics
        using var memory = new MemoryStream();
        TypedBinary.Write(memory, value, typeof(T), options, SchemaMode.Inline);
        var document = BinarySerializer.Read(memory.ToArray(), options);
        document.SchemaMode = options.SchemaMode;
        return TextFormat.Write(document, options);
    }

    /// <summary>
    /// Write an object asynchronously
    /// </summary>
    public static async Task WriteAsync<T>(Stream stream, T value, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bytes = Utf8.Strict.GetBytes(Write(value, options));
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Read an object; the actual type is read from the data (polymorphism)
    /// </summary>
    public static T Read<T>(string text, SerializationOptions? options = null)
    {
        options ??= SerializationOptions.Default;
        var document = Read(text, options);
        var binary = BinarySerializer.Write(document, options);
        return BinarySerializer.Read<T>(binary, options);
    }

    /// <summary>
    /// Read an object from a UTF-8 stream
    /// </summary>
    public static T Read<T>(Stream stream, SerializationOptions? options = null) => Read<T>(Lexer.ReadText(stream), options);

    /// <summary>
    /// Read an object asynchronously
    /// </summary>
    public static async Task<T> ReadAsync<T>(Stream stream, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var data = await StreamHelper.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        return Read<T>(Lexer.DecodeText(data, data.Length), options);
    }

    /// <summary>
    /// Read an untyped document
    /// </summary>
    public static DataDocument Read(string text, SerializationOptions? options = null)
    {
        options ??= SerializationOptions.Default;
        return TextFormat.Parse(text, options, ResourceResolver.For(options));
    }

    /// <summary>
    /// Read an untyped document from a UTF-8 stream
    /// </summary>
    public static DataDocument Read(Stream stream, SerializationOptions? options = null) => Read(Lexer.ReadText(stream), options);

    /// <summary>
    /// Read an untyped document asynchronously
    /// </summary>
    public static async Task<DataDocument> ReadAsync(Stream stream, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var data = await StreamHelper.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        return Read(Lexer.DecodeText(data, data.Length), options);
    }

    /// <summary>
    /// Write an untyped document into a string (canonical form)
    /// </summary>
    public static string Write(DataDocument document, SerializationOptions? options = null)
    {
        return TextFormat.Write(document, options ?? SerializationOptions.Default);
    }

    /// <summary>
    /// Write an untyped document (UTF-8, canonical form)
    /// </summary>
    public static void Write(Stream stream, DataDocument document, SerializationOptions? options = null)
    {
        var bytes = Utf8.Strict.GetBytes(Write(document, options));
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Write an untyped document asynchronously
    /// </summary>
    public static async Task WriteAsync(Stream stream, DataDocument document, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bytes = Utf8.Strict.GetBytes(Write(document, options));
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }
}
