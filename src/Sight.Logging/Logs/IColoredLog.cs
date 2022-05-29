namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe a colored log message
    /// </summary>
    public interface IColoredLog : IContentLog
    {
        /// <summary>
        /// Log color
        /// </summary>
        public LogColor Color { get; }
    }
}
