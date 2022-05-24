using Sight.Logging.Fields;
using Sight.Logging.Messages;

namespace Sight.Logging
{
    /// <summary>
    /// Logger
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Indicates if log level is enabled
        /// </summary>
        bool IsEnabled(LogLevel level);

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Formatted message</param>
        /// <param name="fields">Additional fields</param>
        void Log(LogLevel level, LogPart message, LogField[] fields);
    }
}
