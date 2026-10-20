using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sight.Logging.Logs;

namespace Sight.Logging.Loggers;

/// <summary>
/// Implement logger for multiple outputs, a failing logger does not prevent the following ones from logging
/// </summary>
#if NETSTANDARD2_0
public class MixedLogger : ILogger, ILogLevelFilter, IDisposable
#else
public class MixedLogger : ILogger, ILogLevelFilter, IDisposable, IAsyncDisposable
#endif
{
    private readonly ILogger[] _loggers;
    private readonly bool _ownsLoggers;

    /// <summary>
    /// Initialize a new instance of the class <see cref="MixedLogger"/>
    /// </summary>
    public MixedLogger(params ILogger[] loggers)
        : this((IReadOnlyList<ILogger>)loggers)
    {
    }

    /// <summary>
    /// Initialize a new instance of the class <see cref="MixedLogger"/>
    /// </summary>
    /// <param name="loggers">Loggers that receive each message</param>
    /// <param name="ownsLoggers">Indicates if the disposable loggers are disposed with this one (false by default)</param>
    public MixedLogger(IReadOnlyList<ILogger> loggers, bool ownsLoggers = false)
    {
        if (loggers == null)
            throw new ArgumentNullException(nameof(loggers));

        _loggers = loggers.ToArray();
        _ownsLoggers = ownsLoggers;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevels level)
    {
        foreach (var logger in _loggers)
        {
            if (logger.IsEnabled(level))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Log(object message)
    {
        foreach (var logger in _loggers)
        {
            try
            {
                logger.Log(message);
            }
            catch (Exception)
            {
                // Logging must never throw into the application, the other loggers still receive the message
            }
        }
    }

    /// <summary>
    /// Dispose the loggers if they are owned
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose the loggers if they are owned
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || !_ownsLoggers)
            return;

        foreach (var logger in _loggers)
        {
            try
            {
                (logger as IDisposable)?.Dispose();
            }
            catch (Exception)
            {
                // Dispose the remaining loggers anyway
            }
        }
    }

#if !NETSTANDARD2_0
    /// <inheritdoc cref="Dispose()"/>
    public async ValueTask DisposeAsync()
    {
        if (_ownsLoggers)
        {
            foreach (var logger in _loggers)
            {
                try
                {
                    if (logger is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    else
                        (logger as IDisposable)?.Dispose();
                }
                catch (Exception)
                {
                    // Dispose the remaining loggers anyway
                }
            }
        }

        GC.SuppressFinalize(this);
    }
#endif
}
