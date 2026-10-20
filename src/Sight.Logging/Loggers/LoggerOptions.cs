using System;
using System.Text;

namespace Sight.Logging.Loggers;

/// <summary>
/// Describe when a <see cref="ConsoleLogger"/> writes ANSI escape sequences
/// </summary>
public enum ConsoleColorMode
{
    /// <summary>
    /// Sequences are written unless the <c>NO_COLOR</c> environment variable is set or the output is redirected
    /// </summary>
    Auto,

    /// <summary>
    /// Sequences are always written
    /// </summary>
    Always,

    /// <summary>
    /// Sequences are never written
    /// </summary>
    Never
}

/// <summary>
/// Describe when a <see cref="FileLogger"/> starts a new file
/// </summary>
public enum FileRollInterval
{
    /// <summary>
    /// The file is never rolled because of the time
    /// </summary>
    None,

    /// <summary>
    /// A new file is started each hour
    /// </summary>
    Hourly,

    /// <summary>
    /// A new file is started each day
    /// </summary>
    Daily
}

/// <summary>
/// Options shared by the text loggers (<see cref="ConsoleLogger"/> and <see cref="FileLogger"/>)
/// </summary>
public abstract class TextLoggerOptions
{
    /// <summary>
    /// Indicates if messages are written in a separated thread
    /// </summary>
    public bool CreateThread { get; set; }

    /// <summary>
    /// Options of the message queue used when <see cref="CreateThread"/> is true
    /// </summary>
    public QueueOptions? Queue { get; set; }

    /// <summary>
    /// Indicates if the directories of the stack trace locations are removed from exceptions
    /// (absolute build machine paths should not be leaked in production logs), false by default
    /// </summary>
    public bool HideStackTracePaths { get; set; }

    /// <summary>
    /// Format of the timestamp written before each message (custom date and time format string), null for no timestamp.
    /// The timestamp is captured when the message is created, messages created without it get the writing time.
    /// </summary>
    public string? TimestampFormat { get; set; }

    /// <summary>
    /// Indicates if the timestamp is written in UTC (otherwise in local time)
    /// </summary>
    public bool UtcTimestamp { get; set; }
}

/// <summary>
/// Options of <see cref="ConsoleLogger"/>
/// </summary>
public sealed class ConsoleLoggerOptions : TextLoggerOptions
{
    /// <summary>
    /// Initialize a new instance of the class <see cref="ConsoleLoggerOptions"/> (local time <c>HH:mm:ss.fff</c> timestamp)
    /// </summary>
    public ConsoleLoggerOptions()
    {
        TimestampFormat = "HH:mm:ss.fff";
    }

    /// <summary>
    /// Indicates if <see cref="System.Console.OutputEncoding"/> is set to UTF-8 when the logger is created (process-wide side effect,
    /// ignored when there is no console), false by default
    /// </summary>
    public bool Utf8Output { get; set; }

    /// <summary>
    /// Indicates when ANSI escape sequences (colors, hyperlinks) are written, <see cref="ConsoleColorMode.Auto"/> by default
    /// </summary>
    public ConsoleColorMode ColorMode { get; set; }

    /// <summary>
    /// Indicates if hyperlinks are written as clickable OSC 8 links (when ANSI sequences are enabled), true by default
    /// </summary>
    public bool Hyperlinks { get; set; } = true;
}

/// <summary>
/// Options of <see cref="FileLogger"/>
/// </summary>
public sealed class FileLoggerOptions : TextLoggerOptions
{
    /// <summary>
    /// Initialize a new instance of the class <see cref="FileLoggerOptions"/> (UTC ISO 8601 timestamp with milliseconds)
    /// </summary>
    public FileLoggerOptions()
    {
        TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffK";
        UtcTimestamp = true;
    }

    /// <summary>
    /// Indicates if messages are appended to an existing file (otherwise the file is overwritten), true by default
    /// </summary>
    public bool Append { get; set; } = true;

    /// <summary>
    /// Indicates if the file is flushed after each message when messages are written synchronously, true by default
    /// (in background mode the file is flushed each time the queue becomes empty)
    /// </summary>
    public bool AutoFlush { get; set; } = true;

    /// <summary>
    /// File encoding, UTF-8 without byte order mark by default
    /// </summary>
    public Encoding? Encoding { get; set; }

    /// <summary>
    /// Size in bytes after which the file is rolled (renamed <c>name.yyyyMMdd-HHmmss.ext</c> and replaced by a new one), null for no limit
    /// </summary>
    public long? MaxFileSize { get; set; }

    /// <summary>
    /// Period after which the file is rolled, none by default
    /// </summary>
    public FileRollInterval RollInterval { get; set; }

    /// <summary>
    /// Number of rolled files kept besides the current one (oldest are deleted), null to keep all of them
    /// </summary>
    public int? MaxFiles { get; set; }

    internal Func<DateTime>? Clock { get; set; }
}
