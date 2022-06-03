using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sight.Logging.Logs;

namespace Sight.Logging.Loggers
{
    /// <summary>
    /// Implement logger for console outputs
    /// </summary>
    public sealed class ConsoleLogger : ILogger, IDisposable
    {
        private readonly QueueLogger? _queue;

        static ConsoleLogger()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        /// <summary>
        /// Initialize a new instance of the class <see cref="ConsoleLogger"/>
        /// </summary>
        /// <param name="createThread">Indicates if console will be written in a separated thread</param>
        public ConsoleLogger(bool createThread = false)
        {
            if (createThread)
            {
                _queue = new QueueLogger();
                Task.Run(ProcessMessages);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_queue != null)
            {
                _queue.Dispose();

                foreach (var message in _queue.UnsafeMessages)
                {
                    LogImpl(message);
                }
            }
        }

        /// <inheritdoc />
        public void Log(object message)
        {
            if (_queue == null)
            {
                LogImpl(message);
            }
            else
            {
                _queue.Log(message);
            }
        }

        private static void LogImpl(object message)
        {
            var writer = ResolveLogLevel(message) >= LogLevels.Error ? Console.Error : Console.Out;
            LogImpl(writer, message);
            writer.WriteLine();
        }

        private static void LogImpl(TextWriter writer, object message)
        {
            switch (message)
            {
                case LogIcon icon:
                    writer.Write(icon.DefinedIcon switch
                    {
                        LogIcons.Debug => '…',
                        LogIcons.Information => 'i',
                        LogIcons.Warning => '#',
                        LogIcons.Error => 'X',
                        LogIcons.Start => '>',
                        LogIcons.Stop => '%',
                        LogIcons.Hidden => ' ',
                        _ => '?'
                    });

                    break;
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

        private async Task ProcessMessages()
        {
            while (true)
            {
                try
                {
                    var message = await _queue!.WaitForMessageAsync();
                    LogImpl(message);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
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
