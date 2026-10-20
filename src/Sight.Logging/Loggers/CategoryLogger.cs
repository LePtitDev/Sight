using System;
using Sight.Logging.Internal;
using Sight.Logging.Logs;

namespace Sight.Logging.Loggers;

/// <summary>
/// Implement logger that tags each message with a category
/// </summary>
public sealed class CategoryLogger : ILogger, ILogLevelFilter
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initialize a new instance of the class <see cref="CategoryLogger"/>
    /// </summary>
    /// <param name="logger">Inner logger</param>
    /// <param name="category">Category added to each message</param>
    public CategoryLogger(ILogger logger, string category)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Category = category ?? throw new ArgumentNullException(nameof(category));
    }

    /// <summary>
    /// Category added to each message
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Logger the messages are forwarded to
    /// </summary>
    public ILogger Inner => _logger;

    /// <inheritdoc />
    public bool IsEnabled(LogLevels level) => _logger.IsEnabled(level);

    /// <inheritdoc />
    public void Log(object message)
    {
        _logger.Log(new CategorizedLog(Category, message));
    }
}
