using System.Diagnostics;
using System.Runtime.InteropServices;
using Sight.Logging.Loggers;

namespace Sight.Logging.Sample
{
    public static class Program
    {
        private const string OutputFilePath = "logs.txt";

        public static void Main()
        {
            using var consoleLogger = new ConsoleLogger();
            using var fileLogger = new FileLogger(OutputFilePath, createThread: true);

            var logger = new MixedLogger(consoleLogger, fileLogger);
            var stopwatch = logger.LogTimeStart("Start program");
            logger.LogInformation("Hello World!");
            logger.LogWarning("This logger support warnings");
            logger.LogError("...and errors!");

            try
            {
                throw new InvalidOperationException("An unhandled exception");
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }

            logger.LogDebug("Exit sample...");
            logger.LogTimeStop(stopwatch, "Program finished");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("notepad.exe", OutputFilePath);
        }
    }
}
