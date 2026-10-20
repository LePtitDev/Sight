using Sight.Logging.Loggers;

namespace Sight.Logging;

/// <summary>
/// Extension methods to categorize the messages of a <see cref="ILogger"/>
/// </summary>
public static class LoggerCategoryExt
{
    /// <summary>
    /// Get a logger that tags each message with a category. Calling it on a categorized logger
    /// appends to its category (<c>Http</c> then <c>Client</c> gives <c>Http.Client</c>).
    /// </summary>
    public static ILogger For(this ILogger logger, string category)
    {
        return logger is CategoryLogger parent
            ? new CategoryLogger(parent.Inner, parent.Category + "." + category)
            : new CategoryLogger(logger, category);
    }

    /// <summary>
    /// Get a logger that tags each message with the name of a type
    /// </summary>
    public static ILogger For<T>(this ILogger logger)
    {
        return logger.For(typeof(T).Name);
    }
}
