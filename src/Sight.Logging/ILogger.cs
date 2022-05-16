namespace Sight.Logging
{
    /// <summary>
    /// Logger
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="eventId">ID to a specific event (-1 if no event)</param>
        /// <param name="level">Log level</param>
        /// <param name="message">The message</param>
        /// <param name="attachment">Additional attached object</param>
        void Log(long eventId, LogLevel level, string message, object? attachment);
    }
}
