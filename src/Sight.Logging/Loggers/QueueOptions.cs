using System;

namespace Sight.Logging.Loggers;

/// <summary>
/// Describe what happens when a bounded queue is full
/// </summary>
public enum QueueFullMode
{
    /// <summary>
    /// The producer waits for a free slot
    /// </summary>
    Wait,

    /// <summary>
    /// The oldest queued message is dropped
    /// </summary>
    DropOldest,

    /// <summary>
    /// The newest queued message is dropped to make room for the new one
    /// </summary>
    DropNewest
}

/// <summary>
/// Options of a message queue (<see cref="QueueLogger"/> or background mode of a logger)
/// </summary>
public sealed class QueueOptions
{
    /// <summary>
    /// Max number of queued messages, null (default) for an unbounded queue
    /// </summary>
    public int? Capacity { get; set; }

    /// <summary>
    /// Behavior when <see cref="Capacity"/> is reached
    /// </summary>
    public QueueFullMode FullMode { get; set; } = QueueFullMode.Wait;

    /// <summary>
    /// Max time a logger waits for the queued messages to be written when it is disposed
    /// (default: 10 seconds, <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to wait without limit)
    /// </summary>
    public TimeSpan DisposeTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
