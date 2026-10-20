using System;
using Sight.Logging.Logs;

namespace Sight.Logging.Internal;

internal class TimestampedLog : ITimestampedLog
{
    public TimestampedLog(DateTimeOffset timestamp, object content)
    {
        Timestamp = timestamp;
        Content = content;
    }

    public DateTimeOffset Timestamp { get; }

    public object Content { get; }

    public override string ToString()
    {
        return Content!.ToString()!;
    }
}
