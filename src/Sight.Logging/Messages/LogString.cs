namespace Sight.Logging.Messages
{
    /// <summary>
    /// Log message as string
    /// </summary>
    public class LogString : LogPart
    {
        /// <summary>
        /// Initialize a new instance of the class <see cref="LogString"/>
        /// </summary>
        /// <param name="value"></param>
        public LogString(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Message
        /// </summary>
        public string Value { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }
    }
}
