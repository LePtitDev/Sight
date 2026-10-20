#nullable enable
using Sight.Logging.Logs;

namespace Sight.Logging.Tests;

internal static class MessageExt
{
    /// <summary>
    /// Find a decorator in a message by walking its content chain
    /// </summary>
    public static T Find<T>(this object message)
        where T : class
    {
        while (message is not T)
            message = ((IContentLog)message).Content;

        return (T)message;
    }

    /// <summary>
    /// Find a decorator in a message by walking its content chain, null if there is none
    /// </summary>
    public static T? Find<T>(this object message, bool optional)
        where T : class
    {
        while (message is not T)
        {
            if (message is not IContentLog log)
                return null;

            message = log.Content;
        }

        return (T)message;
    }
}
