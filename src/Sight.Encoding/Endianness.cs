namespace Sight.Encoding;

/// <summary>
/// Byte order of an encoded value
/// </summary>
public enum Endianness
{
    /// <summary>
    /// Least significant byte first
    /// </summary>
    Little,

    /// <summary>
    /// Most significant byte first (network order)
    /// </summary>
    Big
}
