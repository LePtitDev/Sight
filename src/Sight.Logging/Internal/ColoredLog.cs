using Sight.Logging.Logs;

namespace Sight.Logging.Internal
{
    internal class ColoredLog : IColoredLog
    {
        public ColoredLog(LogColor color, object content)
        {
            Color = color;
            Content = content;
        }

        public LogColor Color { get; }

        public object Content { get; }

        public override string ToString()
        {
            return Content!.ToString()!;
        }
    }
}
