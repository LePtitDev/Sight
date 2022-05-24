namespace Sight.Logging.Messages
{
    /// <summary>
    /// Define available colors for logs
    /// </summary>
    public enum LogColor
    {
        /// <summary>
        /// Default foreground color (light for dark theme and black in light theme)
        /// </summary>
        Default,

        /// <summary>
        /// Default foreground with low opacity
        /// </summary>
        LowOpacity,

        /// <summary>
        /// Highlight color
        /// </summary>
        Highlight,

        /// <summary>
        /// Success color
        /// </summary>
        Success,

        /// <summary>
        /// Warning color
        /// </summary>
        Warning,

        /// <summary>
        /// Error color
        /// </summary>
        Error
    }
}
