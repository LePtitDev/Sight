using System;
using System.Threading;
using Sight.Logging.Internal;
using Sight.Logging.Logs;

namespace Sight.Logging.Loggers;

/// <summary>
/// Implement logger that ignores the messages under a minimum level
/// </summary>
public sealed class FilteredLogger : ILogger, ILogLevelFilter
{
    private readonly ILogger _logger;
    private int _minimumLevel;

    /// <summary>
    /// Initialize a new instance of the class <see cref="FilteredLogger"/>
    /// </summary>
    /// <param name="logger">Inner logger</param>
    /// <param name="minimumLevel">Messages under this level are ignored</param>
    public FilteredLogger(ILogger logger, LogLevels minimumLevel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _minimumLevel = (int)minimumLevel;
    }

    /// <summary>
    /// Messages under this level are ignored (can be changed at any time)
    /// </summary>
    public LogLevels MinimumLevel
    {
        get => (LogLevels)Volatile.Read(ref _minimumLevel);
        set => Volatile.Write(ref _minimumLevel, (int)value);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevels level)
    {
        return level >= MinimumLevel && (_logger is not ILogLevelFilter filter || filter.IsEnabled(level));
    }

    /// <inheritdoc />
    public void Log(object message)
    {
        if (LogMessage.ResolveLevel(message) >= MinimumLevel)
            _logger.Log(message);
    }
}
