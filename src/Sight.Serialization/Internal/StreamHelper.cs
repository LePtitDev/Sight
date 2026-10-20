using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sight.Serialization.Internal;

internal static class StreamHelper
{
    public static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, 81920, cancellationToken).ConfigureAwait(false);
        return memory.ToArray();
    }
}
