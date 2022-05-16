namespace Sight.Logging.Loggers
{
    /// <summary>
    /// Implement logger for console outputs
    /// </summary>
    public class ConsoleLogger : ILogger
    {
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

        /// <inheritdoc />
        public void Log(long eventId, LogLevel level, string message, object? attachment)
        {
            
        }
    }
}
