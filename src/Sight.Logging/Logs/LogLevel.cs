using System;

namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe a log level
    /// </summary>
    public readonly struct LogLevel
    {
        private readonly object? _value;
        private readonly LogLevels _level;

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogLevel"/>
        /// </summary>
        public LogLevel(object value)
        {
            _value = value;
            _level = default;
        }

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogLevel"/>
        /// </summary>
        public LogLevel(LogLevels level)
        {
            _value = null;
            _level = level;
        }

        /// <summary>
        /// Indicates if level is from defined name
        /// </summary>
        public bool IsDefined => _value == null;

        /// <summary>
        /// Log level
        /// </summary>
        public object Value => _value ?? _level;

        /// <summary>
        /// Defined level name if available
        /// </summary>
        public LogLevels? DefinedLevel => IsDefined ? _level : null;

        /// <inheritdoc />
        public override string ToString()
        {
            return Value!.ToString()!;
        }

        /// <summary>
        /// Implicit converter
        /// </summary>
        public static implicit operator LogLevel(LogLevels level) => new LogLevel(level);
    }
}
