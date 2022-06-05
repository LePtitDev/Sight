using System.Collections.Generic;

namespace Sight.Logging.Loggers
{
    /// <summary>
    /// Implement logger for multiple outputs
    /// </summary>
    public class MixedLogger : ILogger
    {
        private readonly IReadOnlyList<ILogger> _loggers;

        /// <summary>
        /// Initialize a new instance of the class <see cref="MixedLogger"/>
        /// </summary>
        public MixedLogger(params ILogger[] loggers)
            : this((IReadOnlyList<ILogger>)loggers)
        {
        }

        /// <summary>
        /// Initialize a new instance of the class <see cref="MixedLogger"/>
        /// </summary>
        public MixedLogger(IReadOnlyList<ILogger> loggers)
        {
            _loggers = loggers;
        }

        /// <inheritdoc />
        public void Log(object message)
        {
            foreach (var logger in _loggers)
            {
                logger.Log(message);
            }
        }
    }
}
