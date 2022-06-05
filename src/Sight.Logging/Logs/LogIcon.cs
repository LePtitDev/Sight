namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe an icon for log message
    /// </summary>
    public readonly struct LogIcon
    {
        private readonly object? _value;
        private readonly LogIcons _icon;

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogIcon"/>
        /// </summary>
        public LogIcon(object value)
        {
            _value = value;
            _icon = default;
        }

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogIcon"/>
        /// </summary>
        public LogIcon(LogIcons icon)
        {
            _value = null;
            _icon = icon;
        }

        /// <summary>
        /// Indicates if icon is from defined name
        /// </summary>
        public bool IsDefined => _value == null;

        /// <summary>
        /// Log icon
        /// </summary>
        public object Value => _value ?? _icon;

        /// <summary>
        /// Defined icon name if available
        /// </summary>
        public LogIcons? DefinedIcon => IsDefined ? _icon : null;

        /// <inheritdoc />
        public override string ToString()
        {
            return "?";
        }

        /// <summary>
        /// Implicit converter
        /// </summary>
        public static implicit operator LogIcon(LogIcons icon) => new LogIcon(icon);
    }
}
