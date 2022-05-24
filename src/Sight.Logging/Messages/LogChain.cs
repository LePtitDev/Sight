using System;
using System.Collections.Generic;
using System.Text;

namespace Sight.Logging.Messages
{
    /// <summary>
    /// Chain of <see cref="LogPart"/>
    /// </summary>
    public class LogChain : LogPart
    {
        private readonly List<LogPart> _parts;
        private bool _freezed;

        /// <summary>
        /// Initialize a new instance of the class <see cref="LogChain"/> not freezed
        /// </summary>
        public LogChain()
        {
            _parts = new List<LogPart>();
        }

        /// <summary>
        /// Initialize a new instance of the class <see cref="LogChain"/> freezed
        /// </summary>
        public LogChain(params LogPart[] parts)
        {
            _parts = new List<LogPart>(parts.Length);
            _parts.AddRange(parts);
            _freezed = true;
        }

        /// <summary>
        /// Inner parts in chain
        /// </summary>
        public IEnumerable<LogPart> Parts => _parts.AsReadOnly();

        /// <summary>
        /// Freeze the parts collection
        /// </summary>
        public void Freeze()
        {
            _freezed = true;
        }

        /// <summary>
        /// Append a new part in chain
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public LogChain Append(LogPart logPart)
        {
            if (_freezed)
                throw new InvalidOperationException("Strings chain is freeze and cannot be modified");

            _parts.Add(logPart);
            return this;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var bld = new StringBuilder();
            foreach (var logString in Parts)
            {
                bld.Append(logString);
            }

            return bld.ToString();
        }
    }
}
