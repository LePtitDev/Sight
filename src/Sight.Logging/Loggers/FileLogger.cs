using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sight.Logging.Logs;

namespace Sight.Logging.Loggers
{
    /// <summary>
    /// Implement logger for file outputs
    /// </summary>
    public sealed class FileLogger : ILogger, IDisposable
    {
        private readonly QueueLogger? _queue;
        private readonly TextWriter _writer;

        /// <summary>
        /// Initialize a new instance of the class <see cref="FileLogger"/>
        /// </summary>
        public FileLogger(string filePath, bool createThread = false)
        {
            var file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
            _writer = new StreamWriter(file);

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

            _writer.Dispose();
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

        private void LogImpl(object message)
        {
            var bld = new StringBuilder();
            LogImpl(bld, message, "\u001b[0m");

            _writer.WriteLine(bld.ToString());
            _writer.Flush();
        }

        private static void LogImpl(StringBuilder writer, object message, string baseColor)
        {
            switch (message)
            {
                case LogIcon icon:
                    writer.Append(icon.DefinedIcon switch
                    {
                        LogIcons.Debug => char.ConvertFromUtf32(0x1F4AC),
                        LogIcons.Information => 'ℹ',
                        LogIcons.Warning => '⚠',
                        LogIcons.Error => '❌',
                        LogIcons.Start => '⏱',
                        LogIcons.Stop => '⏹',
                        LogIcons.Hidden => ' ',
                        _ => '�'
                    });

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
    }
}
