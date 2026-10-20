using System;
using Sight.Logging.Logs;

namespace Sight.Logging.Internal;

/// <summary>
/// Helpers to inspect a log message tree
/// </summary>
internal static class LogMessage
{
    /// <summary>
    /// Get the level of a message (<see cref="LogLevels.Information"/> if it has no defined level)
    /// </summary>
    public static LogLevels ResolveLevel(object message)
    {
        while (true)
        {
            if (message is ILeveledLog { Level: var level })
                return level.IsDefined ? (LogLevels)level.DefinedLevel! : LogLevels.Information;

            if (message is not IContentLog log)
                return LogLevels.Information;

            message = log.Content;
        }
    }

    /// <summary>
    /// Get the timestamp and category of a message by walking its decorators
    /// </summary>
    public static void Inspect(object message, out DateTimeOffset? timestamp, out string? category)
    {
        timestamp = null;
        category = null;
        while (message is IContentLog log)
        {
            if (message is ITimestampedLog timestamped)
                timestamp ??= timestamped.Timestamp;
            else if (message is ICategorizedLog categorized)
                category ??= categorized.Category;

            message = log.Content;
        }
    }
}
