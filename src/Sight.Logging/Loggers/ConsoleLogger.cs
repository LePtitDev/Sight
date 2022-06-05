using System;
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
            var bld = new StringBuilder();
            LogImpl(bld, message, "\u001b[0m");

            var writer = ResolveLogLevel(message) >= LogLevels.Error ? Console.Error : Console.Out;
            writer.WriteLine(bld.ToString());
            writer.Flush();
        }

        private static void LogImpl(StringBuilder writer, object message, string baseColor)
        {
            switch (message)
            {
                case LogIcon icon:
                    writer.Append(icon.DefinedIcon switch
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
                    string? foregroundColor = null;
                    if (color.IsDefined)
                    {
                        foregroundColor = color.DefinedColor switch
                        {
                            LogColors.LowOpacity => "\u001b[90m", // dark gray
                            LogColors.Highlight => "\u001b[36m", // dark cyan
                            LogColors.Success => "\u001b[32m", // dark green
                            LogColors.Warning => "\u001b[33m", // dark yellow
                            LogColors.Error => "\u001b[31m", // dark red
                            _ => "\u001b[0m" // default
                        };

                        writer.Append(foregroundColor);
                    }

                    LogImpl(writer, coloredLog.Content, foregroundColor ?? baseColor);
                    if (color.IsDefined)
                    {
                        writer.Append(baseColor);
                    }

                    break;
                case IHyperlinkLog hyperlinkLog:
                    writer.Append("\u001b[4m");
                    writer.Append(hyperlinkLog.Content);
                    writer.Append("\u001b[24m");
                    break;
                case IRichLog richLog:
                    foreach (var part in richLog)
                    {
                        LogImpl(writer, part, baseColor);
                    }

                    break;
                case IContentLog contentLog:
                    LogImpl(writer, contentLog.Content, baseColor);
                    break;
                default:
                    writer.Append(message);
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
