using Sight.Logging.Logs;

namespace Sight.Logging.Internal
{
    internal class HyperlinkLog : IHyperlinkLog
    {
        public HyperlinkLog(string href, object content)
        {
            Href = href;
            Content = content;
        }

        public string Href { get; }

        public object Content { get; }

        public override string ToString()
        {
            return Content!.ToString()!;
        }
    }
}
