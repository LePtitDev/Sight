using System.Text;

namespace Sight.Serialization.Internal;

internal static class Utf8
{
    /// <summary>
    /// UTF-8 encoding without BOM that throws on invalid data
    /// </summary>
    public static readonly UTF8Encoding Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}
