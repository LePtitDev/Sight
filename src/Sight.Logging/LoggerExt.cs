using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sight.Logging.Logs;

namespace Sight.Logging
{
    /// <summary>
    /// Extension methods for <see cref="ILogger"/>
    /// </summary>
    public static class LoggerExt
    {
        private const int TypeColumnSize = 10;

        private static readonly Regex StackTraceRegex = new Regex(@"(?<PATH>(?>[a-zA-Z]:)?(?>[\/\\][^\/\\:]+)+):line (?<LINE>\d+)", RegexOptions.Compiled);

        /// <summary>
        /// Log information message
        /// </summary>
        public static void LogDebug(this ILogger logger, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Information, LogIcons.Debug, "debug", LogColors.LowOpacity, message, @params);
        }

        /// <summary>
        /// Log information message
        /// </summary>
        public static void LogInformation(this ILogger logger, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Information, LogIcons.Information, "info", LogColors.Default, message, @params);
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void LogWarning(this ILogger logger, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Warning, LogIcons.Warning, "warning", LogColors.Warning, message, @params);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public static void LogError(this ILogger logger, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Error, LogIcons.Error, "error", LogColors.Error, message, @params);
        }

        /// <summary>
        /// Log exception message
        /// </summary>
        public static void LogError(this ILogger logger, Exception exception, string? message = null, params object[] @params)
        {
            LogImpl(logger, LogLevels.Error, LogIcons.Error, "error", LogColors.Error, string.IsNullOrEmpty(message) ? "%e" : $"{message}: %e", @params.Length > 0 ? @params.Append(exception).ToArray() : new object[] { exception });
        }

        /// <summary>
        /// Log message that indicates a record start
        /// </summary>
        public static Stopwatch LogTimeStart(this ILogger logger, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Information, LogIcons.Start, "timer", LogColors.Highlight, message, @params);
            return Stopwatch.StartNew();
        }

        /// <summary>
        /// Log message that indicates a record stop
        /// </summary>
        public static void LogTimeStop(this ILogger logger, Stopwatch stopwatch, string message, params object[] @params)
        {
            LogImpl(logger, LogLevels.Information, LogIcons.Stop, "timer", LogColors.Highlight, $"{message}: %t", @params.Length > 0 ? @params.Append(stopwatch.Elapsed).ToArray() : new object[] { stopwatch.Elapsed });
        }

        private static void LogImpl(ILogger logger, LogLevels level, LogIcon icon, string type, LogColor typeColor, string message, params object[] @params)
        {
            var parts = new List<object>();
            parts.Add(Log.ColoredLog(typeColor, Log.RichLog(icon, Log.TextLog($" {type}"))));

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

                        if (c == 'e')
                        {
                            if (@params[index] is Exception ex)
                            {
                                var exception = ex.ToString();
                                var exceptionParts = new List<object>();
                                var exceptionIndex = 0;
                                foreach (var match in StackTraceRegex.Matches(exception).OfType<Match>())
                                {
                                    if (exceptionIndex < match.Index)
                                        exceptionParts.Add(exception.Substring(exceptionIndex, match.Index - exceptionIndex));

                                    exceptionParts.Add(Log.HyperlinkLog($"{match.Groups["PATH"].Value}#L{match.Groups["LINE"].Value}", match.Value));
                                    exceptionIndex = match.Index + match.Length;
                                }

                                if (exceptionIndex < exception.Length)
                                    exceptionParts.Add(exception.Substring(exceptionIndex));

                                parts.Add(Log.ColoredLog(LogColors.LowOpacity, Log.RichLog(exceptionParts)));
                            }
                        }
                        else if (c == 't')
                        {
                            if (@params[index] is TimeSpan time)
                                parts.Add(Log.ColoredLog(LogColors.Highlight, Log.TimedLog(time, Log.Format(time))));
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
