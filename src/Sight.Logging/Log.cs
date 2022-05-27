using System.Collections.Generic;
using Sight.Logging.Internal;
using Sight.Logging.Logs;

namespace Sight.Logging
{
    /// <summary>
    /// Some helpers to build log messages
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Get a log level by name
        /// </summary>
        public static LogLevel Level(string name) => new LogLevel(name);

        /// <summary>
        /// Get a log level by defined name
        /// </summary>
        public static LogLevel Level(LogLevels level) => new LogLevel(level);

        /// <summary>
        /// Get a log color by name or hexadecimal value
        /// </summary>
        public static LogColor Color(string nameOrHex) => new LogColor(nameOrHex);

        /// <summary>
        /// Get a log color by defined name
        /// </summary>
        public static LogColor Color(LogColors color) => new LogColor(color);

        /// <summary>
        /// Get a plain text message
        /// </summary>
        public static string TextLog(string message) => message;

        /// <summary>
        /// Get a message with a log level
        /// </summary>
        public static ILeveledLog LeveledLog(LogLevel level, object message) => new LeveledLog(level, message);

        /// <summary>
        /// Get a message with a color
        /// </summary>
        public static IColoredLog ColoredLog(LogColor color, object message) => new ColoredLog(color, message);

        /// <summary>
        /// Get a message with multiple parts
        /// </summary>
        public static IRichLog RichLog(IEnumerable<object> parts) => new RichLog(parts);

        /// <inheritdoc cref="RichLog(IEnumerable{object})"/>
        public static IRichLog RichLog(params object[] parts) => RichLog(parts.Length == 1 && parts[0] is IEnumerable<object> enumerable ? enumerable : parts);
    }
}
