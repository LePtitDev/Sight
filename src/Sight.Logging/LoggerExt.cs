using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sight.Logging.Logs;

namespace Sight.Logging
{
    /// <summary>
    /// Extension methods for <see cref="ILogger"/>
    /// </summary>
    public static class LoggerExt
    {
        private const int TypeColumnSize = 10;

        /// <summary>
        /// Log information message
        /// </summary>
        public static void LogDebug(this ILogger logger, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Information, "… debug", LogColors.LowOpacity, message, @params);
        }

        /// <summary>
        /// Log information message
        /// </summary>
        public static void LogInformation(this ILogger logger, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Information, "  info", LogColors.Default, message, @params);
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void LogWarning(this ILogger logger, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Warning, "# warning", LogColors.Warning, message, @params);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public static void LogError(this ILogger logger, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Error, "X error", LogColors.Error, message, @params);
        }

        /// <summary>
        /// Log exception message
        /// </summary>
        public static void LogError(this ILogger logger, Exception exception, string? message = null, params object[] @params)
        {
            LogImpl(logger, LogLevels.Error, "X error", LogColors.Error, string.IsNullOrEmpty(message) ? "%e" : $"{message}: %e", @params.Length > 0 ? @params.Append(exception).ToArray() : new object[] { exception });
        }

        private static void LogImpl(ILogger logger, LogLevels level, string type, LogColor typeColor, string message, params object[] @params)
        {
            var parts = new List<object>();
            parts.Add(Log.ColoredLog(typeColor, Log.TextLog(type)));

            var availableSize = Math.Max(0, TypeColumnSize - type.Length);
            parts.Add(Log.TextLog(new string(' ', availableSize + 2)));

            if (@params.Length > 0)
            {
                var index = 0;
                var bld = new StringBuilder();
                var escape = false;
                foreach (var c in message)
                {
                    if (escape)
                    {
                        if (c == '%')
                        {
                            bld.Append(c);
                            continue;
                        }

                        parts.Add(Log.TextLog(bld.ToString()));
                        bld.Clear();

                        if (c == 'e' && @params[index] is Exception ex)
                        {
                            parts.Add(Log.ColoredLog(LogColors.LowOpacity, Log.TextLog(ex.ToString())));
                        }
                        else
                        {
                            bld.Append(c);
                            continue;
                        }

                        index++;
                    }
                    else if (c == '%' && index < @params.Length)
                    {
                        escape = true;
                    }
                    else
                    {
                        bld.Append(c);
                    }
                }

                if (bld.Length > 0)
                {
                    parts.Add(Log.TextLog(bld.ToString()));
                }
            }
            else
            {
                parts.Add(Log.TextLog(message));
            }

            logger.Log(Log.LeveledLog(level, Log.RichLog(parts)));
        }
    }
}
