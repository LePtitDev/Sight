using System;

namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe a log level
    /// </summary>
    public readonly struct LogLevel
    {
        private readonly string? _name;
        private readonly LogLevels _level;

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogLevel"/>
        /// </summary>
        public LogLevel(string name)
        {
            if (Enum.TryParse(name, out LogLevels level))
            {
                _name = null;
                _level = level;
            }
            else
            {
                _name = name;
                _level = default;
            }
        }

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogLevel"/>
        /// </summary>
        public LogLevel(LogLevels level)
        {
            _name = null;
            _level = level;
        }

        /// <summary>
        /// Indicates if level is from defined name
        /// </summary>
        public bool IsDefined => _name == null;

        /// <summary>
        /// Log level as string
        /// </summary>
        public string Value => _name ?? _level.ToString();

        /// <summary>
        /// Defined level name if available
        /// </summary>
        public LogLevels? DefinedLevel => IsDefined ? _level : null;

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Implicit converter
        /// </summary>
        public static implicit operator LogLevel(LogLevels level) => new LogLevel(level);
    }
}
