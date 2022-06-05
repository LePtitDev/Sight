namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe a log message that decorate an inner content
    /// </summary>
    public interface IContentLog
    {
        /// <summary>
        /// Log content
        /// </summary>
        public object Content { get; }
    }
}
