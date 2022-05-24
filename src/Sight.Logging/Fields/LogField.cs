namespace Sight.Logging.Fields
{
    /// <summary>
    /// Additional field to fill more info about a log
    /// </summary>
    public class LogField
    {
        /// <summary>
        /// Initialize a new instance of the class <see cref="LogField"/>
        /// </summary>
        /// <param name="name">Field name</param>
        /// <param name="value">Field content</param>
        public LogField(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Content
        /// </summary>
        public object Value { get; }
    }
}
