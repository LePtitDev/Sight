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
        /// <param name="message">Formatted message</param>
        public void Log(object message);
    }
}
