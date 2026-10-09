namespace Sight.Logging.Loggers
{
    /// <summary>
    /// Implement logger that ignores all messages
    /// </summary>
    public sealed class NullLogger : ILogger
    {
        private NullLogger()
        {
        }

        /// <summary>
        /// Unique instance of the logger
        /// </summary>
        public static NullLogger Instance { get; } = new NullLogger();

        /// <inheritdoc />
        public void Log(object message)
        {
        }
    }
}
