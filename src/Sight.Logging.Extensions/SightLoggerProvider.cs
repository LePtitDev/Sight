using System;
using Microsoft.Extensions.Logging;

namespace Sight.Logging.Extensions;

/// <summary>
/// <see cref="ILoggerProvider"/> that writes the messages of Microsoft.Extensions.Logging to a Sight logger
/// </summary>
public sealed class SightLoggerProvider : ILoggerProvider
{
    private readonly Sight.Logging.ILogger _logger;
    private readonly bool _disposeLogger;

    /// <summary>
    /// Initialize a new instance of the class <see cref="SightLoggerProvider"/>
    /// </summary>
    /// <param name="logger">Sight logger that receives the messages</param>
    /// <param name="disposeLogger">Indicates if <paramref name="logger"/> is disposed with the provider</param>
    public SightLoggerProvider(Sight.Logging.ILogger logger, bool disposeLogger = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _disposeLogger = disposeLogger;
    }

    /// <inheritdoc />
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new SightLoggerAdapter(string.IsNullOrEmpty(categoryName) ? _logger : _logger.For(categoryName));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposeLogger)
            (_logger as IDisposable)?.Dispose();
    }
}
