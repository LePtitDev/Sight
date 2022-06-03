using System;

namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe a color for log message
    /// </summary>
    public readonly struct LogColor
    {
        private readonly object? _value;
        private readonly LogColors _color;

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogColor"/>
        /// </summary>
        public LogColor(object value)
        {
            _value = value;
            _color = default;
        }

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogColor"/>
        /// </summary>
        public LogColor(LogColors color)
        {
            _value = null;
            _color = color;
        }

        /// <summary>
        /// Indicates if color is from defined name
        /// </summary>
        public bool IsDefined => _value == null;

        /// <summary>
        /// Color value
        /// </summary>
        public object Value => _value ?? _color;

        /// <summary>
        /// Defined color name if available
        /// </summary>
        public LogColors? DefinedColor => IsDefined ? _color : null;

        /// <inheritdoc />
        public override string ToString()
        {
            return Value!.ToString()!;
        }

        /// <summary>
        /// Implicit converter
        /// </summary>
        public static implicit operator LogColor(LogColors color) => new LogColor(color);
    }
}
