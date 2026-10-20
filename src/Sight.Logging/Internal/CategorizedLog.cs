using Sight.Logging.Logs;

namespace Sight.Logging.Internal;

internal class CategorizedLog : ICategorizedLog
{
    public CategorizedLog(string category, object content)
    {
        Category = category;
        Content = content;
    }

    public string Category { get; }

    public object Content { get; }

    public override string ToString()
    {
        return Content!.ToString()!;
    }
}
