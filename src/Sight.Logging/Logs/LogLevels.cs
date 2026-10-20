namespace Sight.Logging.Logs;

/// <summary>
/// Define levels of a message, ordered from the most detailed to the most severe
/// </summary>
public enum LogLevels
{
    /// <summary>
    /// Very detailed diagnostic
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Detailed
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Standard
    /// </summary>
    Information = 2,

    /// <summary>
    /// Warning
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Error
    /// </summary>
    Error = 4,

    /// <summary>
    /// Critical error, the application cannot continue
    /// </summary>
    Critical = 5
}
