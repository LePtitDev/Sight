using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging;

/// <summary>
/// Extension methods to filter the messages of a <see cref="ILogger"/>
/// </summary>
public static class LoggerFilterExt
{
    /// <summary>
    /// Get a logger that ignores the messages under a minimum level
    /// </summary>
    public static FilteredLogger WithMinimumLevel(this ILogger logger, LogLevels minimumLevel)
    {
        return new FilteredLogger(logger, minimumLevel);
    }

    /// <summary>
    /// Indicates if a message of the specified level is not ignored by the logger (always true if the logger cannot filter)
    /// </summary>
    public static bool IsEnabled(this ILogger logger, LogLevels level)
    {
        return logger is not ILogLevelFilter filter || filter.IsEnabled(level);
    }
}
