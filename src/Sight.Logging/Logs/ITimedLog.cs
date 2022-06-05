using System;

namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe a log message with a record time
    /// </summary>
    public interface ITimedLog : IContentLog
    {
        /// <summary>
        /// Record time
        /// </summary>
        public TimeSpan Time { get; }
    }
}
