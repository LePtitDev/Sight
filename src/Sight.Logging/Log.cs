using System;
using System.Collections.Generic;
using System.Text;
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
        /// Get a message with a hyperlink
        /// </summary>
        public static IHyperlinkLog HyperlinkLog(string href, object message) => new HyperlinkLog(href, message);

        /// <summary>
        /// Get a message with a record time
        /// </summary>
        public static ITimedLog TimedLog(TimeSpan time, object message) => new TimedLog(time, message);

        /// <summary>
        /// Get a message with multiple parts
        /// </summary>
        public static IRichLog RichLog(IEnumerable<object> parts) => new RichLog(parts);

        /// <inheritdoc cref="RichLog(IEnumerable{object})"/>
        public static IRichLog RichLog(params object[] parts) => RichLog(parts.Length == 1 && parts[0] is IEnumerable<object> enumerable ? enumerable : parts);

        /// <summary>
        /// Format time to string
        /// </summary>
        public static string Format(TimeSpan time)
        {
            var bld = new StringBuilder();
            if (time.Days > 0)
                bld.Append(time.Days).Append('d');

            if (time.Hours > 0)
            {
                if (bld.Length > 0)
                    bld.Append(' ');

                bld.Append(time.Hours).Append('h');
            }

            if (time.Minutes > 0)
            {
                if (bld.Length > 0)
                    bld.Append(' ');

                bld.Append(time.Minutes).Append('m');
            }

            if (time.Seconds > 0)
            {
                if (bld.Length > 0)
                    bld.Append(' ');

                bld.Append(time.Seconds);
                if (time.Milliseconds > 0)
                    bld.Append('.').Append($"{time.Milliseconds}:D3");

                bld.Append('s');
            }

            if (time.Milliseconds > 0 && bld.Length == 0)
            {
                bld.Append(time.Milliseconds).Append("ms");
            }

            return bld.ToString();
        }
    }
}
