using System;

namespace Sight.Logging.Logs
{
    /// <summary>
    /// Describe a color for log message
    /// </summary>
    public readonly struct LogColor
    {
        private readonly string? _nameOrHex;
        private readonly LogColors _color;

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogColor"/>
        /// </summary>
        public LogColor(string nameOrHex)
        {
            if (!string.IsNullOrEmpty(nameOrHex) && nameOrHex[0] != '#' && Enum.TryParse(nameOrHex, out LogColors color))
            {
                _nameOrHex = null;
                _color = color;
            }
            else
            {
                _nameOrHex = nameOrHex;
                _color = default;
            }
        }

        /// <summary>
        /// Initialize a new instance of the struct <see cref="LogColor"/>
        /// </summary>
        public LogColor(LogColors color)
        {
            _nameOrHex = null;
            _color = color;
        }

        /// <summary>
        /// Indicates if color is an hexadecimal color
        /// </summary>
        public bool IsHex => !string.IsNullOrEmpty(_nameOrHex) && _nameOrHex![0] == '#';

        /// <summary>
        /// Indicates if color is from defined name
        /// </summary>
        public bool IsDefined => _nameOrHex == null;

        /// <summary>
        /// Color value as string
        /// </summary>
        public string Value => _nameOrHex ?? _color.ToString();

        /// <summary>
        /// Defined color name if available
        /// </summary>
        public LogColors? DefinedColor => IsDefined ? _color : null;

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Implicit converter
        /// </summary>
        public static implicit operator LogColor(LogColors color) => new LogColor(color);
    }
}
