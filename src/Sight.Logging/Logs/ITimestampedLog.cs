using System;

namespace Sight.Logging.Logs;

/// <summary>
/// Describe a log message with the time it was logged
/// </summary>
public interface ITimestampedLog : IContentLog
{
    /// <summary>
    /// Time the message was logged (captured when the message is created, not when it is written)
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
