using System;
using System.IO;
using System.Text;
using Sight.Logging.Logs;

namespace Sight.Logging.Loggers
{
    /// <summary>
    /// Implement logger for console outputs
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        static ConsoleLogger()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        /// <inheritdoc />
        public void Log(object message)
        {
            var writer = ResolveLogLevel(message) >= LogLevels.Error ? Console.Error : Console.Out;
            LogImpl(writer, message);
            writer.WriteLine();
        }

        private static void LogImpl(TextWriter writer, object message)
        {
            switch (message)
            {
                case IColoredLog { Color: var color } coloredLog:
                    var baseColor = Console.ForegroundColor;
                    if (color.IsDefined)
                    {
                        Console.ForegroundColor = color.DefinedColor switch
                        {
                            LogColors.LowOpacity => ConsoleColor.DarkGray,
                            LogColors.Highlight => ConsoleColor.DarkCyan,
                            LogColors.Success => ConsoleColor.DarkGreen,
                            LogColors.Warning => ConsoleColor.DarkYellow,
                            LogColors.Error => ConsoleColor.DarkRed,
                            _ => ConsoleColor.Gray
                        };
                    }

                    LogImpl(writer, coloredLog.Content);
                    Console.ForegroundColor = baseColor;
                    break;
                case IRichLog richLog:
                    foreach (var part in richLog)
                    {
                        LogImpl(writer, part);
                    }

                    break;
                case IContentLog contentLog:
                    LogImpl(writer, contentLog.Content);
                    break;
                default:
                    writer.Write(message);
                    break;
            }
        }

        private static LogLevels ResolveLogLevel(object message)
        {
            if (message is ILeveledLog { Level: var level })
                return level.IsDefined ? (LogLevels)level.DefinedLevel! : LogLevels.Information;

            if (message is IContentLog log)
                return ResolveLogLevel(log.Content);

            return LogLevels.Information;
        }
    }
}
