using Sight.Logging.Logs;

namespace Sight.Logging;

/// <summary>
/// Optional interface of a <see cref="ILogger"/> that can tell if a level is ignored, so the
/// logging extensions skip the creation of the message when nobody will use it
/// </summary>
public interface ILogLevelFilter
{
    /// <summary>
    /// Indicates if a message of the specified level is not ignored by the logger
    /// </summary>
    public bool IsEnabled(LogLevels level);
}
