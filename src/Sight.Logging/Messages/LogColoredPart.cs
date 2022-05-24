namespace Sight.Logging.Messages
{
    /// <summary>
    /// Colored log part
    /// </summary>
    public class LogColoredPart : LogPart
    {
        /// <summary>
        /// Initialize a new instance of the class <see cref="LogColoredPart"/>
        /// </summary>
        public LogColoredPart(LogColor color, LogPart innerPart)
        {
            Color = color;
            InnerPart = innerPart;
        }

        /// <summary>
        /// Message color
        /// </summary>
        public LogColor Color { get; }

        /// <summary>
        /// Inner log message part
        /// </summary>
        public LogPart InnerPart { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return InnerPart.ToString();
        }
    }
}
