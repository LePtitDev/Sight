namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe a log message with an hyperlink attached
    /// </summary>
    public interface IHyperlinkLog : IContentLog
    {
        /// <summary>
        /// Hyperlink reference
        /// </summary>
        public string Href { get; }
    }
}
