using System;
using Sight.Logging.Logs;

namespace Sight.Logging.Internal;

/// <summary>
/// Immutable pieces shared by all the messages of a kind (icon, type name and padding of the type column)
/// </summary>
internal sealed class LogKind
{
    private const int TypeColumnSize = 10;

    public static readonly LogKind Trace = new LogKind(LogLevels.Trace, LogIcons.Debug, "trace", LogColors.LowOpacity);
    public static readonly LogKind Debug = new LogKind(LogLevels.Debug, LogIcons.Debug, "debug", LogColors.LowOpacity);
    public static readonly LogKind Information = new LogKind(LogLevels.Information, LogIcons.Information, "info", LogColors.Default);
    public static readonly LogKind Warning = new LogKind(LogLevels.Warning, LogIcons.Warning, "warning", LogColors.Warning);
    public static readonly LogKind Error = new LogKind(LogLevels.Error, LogIcons.Error, "error", LogColors.Error);
    public static readonly LogKind Critical = new LogKind(LogLevels.Critical, LogIcons.Error, "critical", LogColors.Error);
    public static readonly LogKind TimeStart = new LogKind(LogLevels.Information, LogIcons.Start, "timer", LogColors.Highlight);
    public static readonly LogKind TimeStop = new LogKind(LogLevels.Information, LogIcons.Stop, "timer", LogColors.Highlight);

    private LogKind(LogLevels level, LogIcon icon, string type, LogColor color)
    {
        Level = level;
        Header = Log.ColoredLog(color, Log.RichLog(icon, Log.TextLog($" {type}")));
        Padding = Log.TextLog(new string(' ', Math.Max(0, TypeColumnSize - type.Length) + 2));
    }

    public LogLevels Level { get; }

    public object Header { get; }

    public object Padding { get; }

    public static LogKind Of(LogLevels level)
    {
        return level switch
        {
            LogLevels.Trace => Trace,
            LogLevels.Debug => Debug,
            LogLevels.Information => Information,
            LogLevels.Warning => Warning,
            LogLevels.Error => Error,
            _ => Critical
        };
    }

    /// <summary>
    /// Build the message from the content parts and send it to the logger
    /// </summary>
    public void Emit(ILogger logger, System.Collections.Generic.IReadOnlyList<object> content)
    {
        var parts = new object[content.Count + 2];
        parts[0] = Header;
        parts[1] = Padding;
        for (var i = 0; i < content.Count; i++)
            parts[i + 2] = content[i];

        logger.Log(new TimestampedLog(DateTimeOffset.UtcNow, Log.LeveledLog(Level, new RichLog(parts))));
    }
}
