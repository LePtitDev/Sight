using System;
using System.IO;
using System.Text;
using Sight.Logging.Fields;
using Sight.Logging.Messages;

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

        /// <summary>
        /// Initialize a new instance of the class <see cref="ConsoleLogger"/>
        /// </summary>
        /// <param name="category"></param>
        public ConsoleLogger(string? category = null)
        {
            Category = category;
        }

        /// <summary>
        /// Logger category
        /// </summary>
        public string? Category { get; set; }

        /// <inheritdoc />s
        public bool IsEnabled(LogLevel level)
        {
            return true;
        }

        /// <inheritdoc />
        public void Log(LogLevel level, LogPart message, LogField[] fields)
        {
            LogImpl(level >= LogLevel.Error ? Console.Error : Console.Out, message);
            Console.WriteLine();
        }

        private static void LogImpl(TextWriter writer, LogPart message)
        {
            switch (message)
            {
                case LogColoredPart coloredPart:
                    var baseColor = Console.ForegroundColor;
                    Console.ForegroundColor = coloredPart.Color switch
                    {
                        LogColor.LowOpacity => ConsoleColor.DarkGray,
                        LogColor.Highlight => ConsoleColor.DarkCyan,
                        LogColor.Success => ConsoleColor.DarkGreen,
                        LogColor.Warning => ConsoleColor.DarkYellow,
                        LogColor.Error => ConsoleColor.DarkRed,
                        _ => ConsoleColor.Gray
                    };

                    LogImpl(writer, coloredPart.InnerPart);
                    Console.ForegroundColor = baseColor;
                    break;
                case LogChain chain:
                    foreach (var part in chain.Parts)
                    {
                        LogImpl(writer, part);
                    }

                    break;
                default:
                    writer.Write(message);
                    break;
            }
        }
    }
}
