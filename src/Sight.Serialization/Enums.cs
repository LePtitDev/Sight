namespace Sight.Serialization;

/// <summary>
/// How the schema is stored in a file
/// </summary>
public enum SchemaMode : byte
{
    /// <summary>
    /// Schema and its imports are embedded: the file is self-contained
    /// </summary>
    Inline = 0,

    /// <summary>
    /// Only the schema name, version and hash are stored; the schema is resolved externally
    /// </summary>
    Reference = 1,

    /// <summary>
    /// Nothing is stored; the reader must be given the schema
    /// </summary>
    None = 2,
}

/// <summary>
/// Compression of the binary format (header excluded)
/// </summary>
public enum Compression : byte
{
    /// <summary>
    /// No compression
    /// </summary>
    None = 0,

    /// <summary>
    /// Raw Deflate (RFC 1951)
    /// </summary>
    Deflate = 1,
}
