namespace Sight.Serialization;

/// <summary>
/// Limits enforced when reading data
/// </summary>
public sealed class SerializationLimits
{
    /// <summary>
    /// Maximum nesting depth of structs, lists, maps and any values
    /// </summary>
    public int MaxDepth { get; set; } = 128;

    /// <summary>
    /// Maximum number of elements of a single list or map
    /// </summary>
    public int MaxCollectionCount { get; set; } = 1 << 24;

    /// <summary>
    /// Maximum length of a string in bytes
    /// </summary>
    public int MaxStringLength { get; set; } = 1 << 24;

    /// <summary>
    /// Maximum number of referenced objects per file
    /// </summary>
    public int MaxObjectCount { get; set; } = 1 << 24;

    /// <summary>
    /// Maximum decompressed size in bytes
    /// </summary>
    public long MaxDecompressedSize { get; set; } = int.MaxValue;
}
