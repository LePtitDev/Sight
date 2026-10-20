using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sight.Serialization.Binary;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Typed;

namespace Sight.Serialization;

/// <summary>
/// Serialization to and from the SBDL binary format
/// </summary>
public static class BinarySerializer
{
    /// <summary>
    /// Write an object; the schema is derived from its type
    /// </summary>
    public static void Write<T>(Stream stream, T value, SerializationOptions? options = null)
    {
        TypedBinary.Write(stream, value, typeof(T), options ?? SerializationOptions.Default);
    }

    /// <summary>
    /// Write an object into a new byte array
    /// </summary>
    public static byte[] Write<T>(T value, SerializationOptions? options = null)
    {
        using var memory = new MemoryStream();
        Write(memory, value, options);
        return memory.ToArray();
    }

    /// <summary>
    /// Write an object asynchronously
    /// </summary>
    public static async Task WriteAsync<T>(Stream stream, T value, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bytes = Write(value, options);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Read an object; the actual type is read from the data (polymorphism)
    /// </summary>
    public static T Read<T>(Stream stream, SerializationOptions? options = null)
    {
        return (T)TypedBinary.Read(stream, typeof(T), options ?? SerializationOptions.Default)!;
    }

    /// <summary>
    /// Read an object from a byte array
    /// </summary>
    public static T Read<T>(byte[] data, SerializationOptions? options = null)
    {
        using var memory = new MemoryStream(data, writable: false);
        return Read<T>(memory, options);
    }

    /// <summary>
    /// Read an object asynchronously
    /// </summary>
    public static async Task<T> ReadAsync<T>(Stream stream, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var data = await StreamHelper.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        return Read<T>(data, options);
    }

    /// <summary>
    /// Read an untyped document
    /// </summary>
    public static DataDocument Read(Stream stream, SerializationOptions? options = null)
    {
        options ??= SerializationOptions.Default;
        return BinaryReading.ReadDocument(stream, options, ResourceResolver.For(options));
    }

    /// <summary>
    /// Read an untyped document from a byte array
    /// </summary>
    public static DataDocument Read(byte[] data, SerializationOptions? options = null)
    {
        using var memory = new MemoryStream(data, writable: false);
        return Read(memory, options);
    }

    /// <summary>
    /// Read an untyped document asynchronously
    /// </summary>
    public static async Task<DataDocument> ReadAsync(Stream stream, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var data = await StreamHelper.ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        return Read(data, options);
    }

    /// <summary>
    /// Write an untyped document (the options give the encoding parameters: string deduplication, compression)
    /// </summary>
    public static void Write(Stream stream, DataDocument document, SerializationOptions? options = null)
    {
        BinaryWriting.WriteDocument(stream, document, options ?? SerializationOptions.Default);
    }

    /// <summary>
    /// Write an untyped document into a new byte array
    /// </summary>
    public static byte[] Write(DataDocument document, SerializationOptions? options = null)
    {
        using var memory = new MemoryStream();
        Write(memory, document, options);
        return memory.ToArray();
    }

    /// <summary>
    /// Write an untyped document asynchronously
    /// </summary>
    public static async Task WriteAsync(Stream stream, DataDocument document, SerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bytes = Write(document, options);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }
}
