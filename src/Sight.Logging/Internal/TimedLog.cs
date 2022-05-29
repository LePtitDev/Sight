using System;
using Sight.Logging.Logs;

namespace Sight.Logging.Internal
{
    internal class TimedLog : ITimedLog
    {
        public TimedLog(TimeSpan time, object content)
        {
            Time = time;
            Content = content;
        }

        public TimeSpan Time { get; }

        public object Content { get; }

        public override string ToString()
        {
            return Content!.ToString()!;
        }
    }
}
