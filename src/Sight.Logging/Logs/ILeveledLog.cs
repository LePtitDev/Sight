namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe a log message with a specific log level
    /// </summary>
    public interface ILeveledLog : IContentLog
    {
        /// <summary>
        /// Log level
        /// </summary>
        public LogLevel Level { get; }
    }
}
