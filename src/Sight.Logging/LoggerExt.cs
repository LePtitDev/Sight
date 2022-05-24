using System;
using System.Linq;
using System.Text;
using Sight.Logging.Fields;
using Sight.Logging.Messages;

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
            if (logger.IsEnabled(LogLevel.Information))
            {
                LogImpl(logger, LogLevel.Information, "… debug", LogColor.LowOpacity, message, @params);
            }
        }

        /// <summary>
        /// Log information message
        /// </summary>
        public static void LogInformation(this ILogger logger, string message, params object[] @params)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                LogImpl(logger, LogLevel.Information, "  info", LogColor.Default, message, @params);
            }
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void LogWarning(this ILogger logger, string message, params object[] @params)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                LogImpl(logger, LogLevel.Warning, "# warning", LogColor.Warning, message, @params);
            }
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public static void LogError(this ILogger logger, string message, params object[] @params)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                LogImpl(logger, LogLevel.Error, "X error", LogColor.Error, message, @params);
            }
        }

        /// <summary>
        /// Log exception message
        /// </summary>
        public static void LogError(this ILogger logger, Exception exception, string? message = null, params object[] @params)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                LogImpl(logger, LogLevel.Error, "X error", LogColor.Error, string.IsNullOrEmpty(message) ? "%e" : $"{message}: %e", @params.Length > 0 ? @params.Append(exception).ToArray() : new object[] { exception });
            }
        }

        private static void LogImpl(ILogger logger, LogLevel level, string type, LogColor typeColor, string message, params object[] @params)
        {
            var chain = new LogChain();
            chain.Append(new LogColoredPart(typeColor, new LogString(type)));

            var availableSize = Math.Max(0, TypeColumnSize - type.Length);
            chain.Append(new LogString(new string(' ', availableSize + 2)));

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

                        chain.Append(new LogString(bld.ToString()));
                        bld.Clear();

                        if (c == 'e' && @params[index] is Exception ex)
                        {
                            chain.Append(new LogColoredPart(LogColor.LowOpacity, new LogString(ex.ToString())));
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
                    chain.Append(new LogString(bld.ToString()));
                }
            }
            else
            {
                chain.Append(new LogString(message));
            }

            logger.Log(level, chain, Array.Empty<LogField>());
        }
    }
}
