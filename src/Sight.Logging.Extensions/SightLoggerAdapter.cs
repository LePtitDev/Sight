using System;
using Microsoft.Extensions.Logging;
using Sight.Logging.Logs;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Sight.Logging.Extensions;

/// <summary>
/// Adapt a Sight logger to <see cref="Microsoft.Extensions.Logging.ILogger"/> (scopes are not supported)
/// </summary>
internal sealed class SightLoggerAdapter : Microsoft.Extensions.Logging.ILogger
{
    private readonly Sight.Logging.ILogger _logger;

    public SightLoggerAdapter(Sight.Logging.ILogger logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && _logger.IsEnabled(Convert(logLevel));
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var text = formatter(state, exception);
        if (exception == null)
        {
            // Without parameter the text is written as is
            Write(logLevel, text);
            return;
        }

        // Escape the text so that braces and percents of the formatted message are not read as placeholders
        var escaped = text.Replace("%", "%%").Replace("{", "{{").Replace("}", "}}");
        Write(logLevel, string.IsNullOrEmpty(text) ? "%e" : escaped + ": %e", exception);
    }

    private void Write(LogLevel logLevel, string message, params object[] @params)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                _logger.LogTrace(message, @params);
                break;
            case LogLevel.Debug:
                _logger.LogDebug(message, @params);
                break;
            case LogLevel.Information:
                _logger.LogInformation(message, @params);
                break;
            case LogLevel.Warning:
                _logger.LogWarning(message, @params);
                break;
            case LogLevel.Error:
                _logger.LogError(message, @params);
                break;
            default:
                _logger.LogCritical(message, @params);
                break;
        }
    }

    private static LogLevels Convert(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogLevels.Trace,
            LogLevel.Debug => LogLevels.Debug,
            LogLevel.Information => LogLevels.Information,
            LogLevel.Warning => LogLevels.Warning,
            LogLevel.Error => LogLevels.Error,
            _ => LogLevels.Critical
        };
    }
}
