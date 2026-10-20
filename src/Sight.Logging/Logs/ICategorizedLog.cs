namespace Sight.Logging.Logs;

/// <summary>
/// Describe a log message with a category (source of the message)
/// </summary>
public interface ICategorizedLog : IContentLog
{
    /// <summary>
    /// Log category
    /// </summary>
    public string Category { get; }
}
