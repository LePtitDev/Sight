using Sight.Logging.Logs;

namespace Sight.Logging.Internal
{
    internal class LeveledLog : ILeveledLog
    {
        public LeveledLog(LogLevel level, object content)
        {
            Level = level;
            Content = content;
        }

        public LogLevel Level { get; }

        public object Content { get; }

        public override string ToString()
        {
            return Content!.ToString()!;
        }
    }
}
