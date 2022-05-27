using System.Collections;
using System.Collections.Generic;
using System.Text;
using Sight.Logging.Logs;

namespace Sight.Logging.Internal
{
    /// <summary>
    /// Chain of log parts
    /// </summary>
    internal class RichLog : IRichLog
    {
        private readonly IEnumerable<object> _parts;

        public RichLog(IEnumerable<object> parts)
        {
            _parts = parts;
        }

        public IEnumerator<object> GetEnumerator()
        {
            return _parts.GetEnumerator();
        }

        public override string ToString()
        {
            var bld = new StringBuilder();
            foreach (var part in _parts)
            {
                bld.Append(part);
            }

            return bld.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
