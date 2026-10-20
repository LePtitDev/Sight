using System.Diagnostics;
using System.Runtime.InteropServices;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Sample;

public static class Program
{
    private const string OutputFilePath = "logs.txt";

    public static void Main()
    {
        using var consoleLogger = new ConsoleLogger(new ConsoleLoggerOptions { Utf8Output = true });
        using var fileLogger = new FileLogger(OutputFilePath, new FileLoggerOptions
        {
            CreateThread = true,
            Append = false,
            MaxFileSize = 1024 * 1024,
            MaxFiles = 3
        });

        // Both loggers receive each message, the console hides debug messages
        var logger = new MixedLogger(consoleLogger.WithMinimumLevel(LogLevels.Information), fileLogger).For("Sample");

        var stopwatch = logger.LogTimeStart("Start program");
        logger.LogInformation("Hello World!");
        logger.LogWarning("This logger support warnings");
        logger.LogError("...and errors!");

        // Placeholders: {0}, {name} and %t / %e, or an interpolated string that is not even evaluated when filtered
        var user = "bob";
        logger.LogInformation("User {user} logged in {elapsed} ago", user, TimeSpan.FromMilliseconds(1500));
        logger.LogInformation($"User {user} logged in {TimeSpan.FromMilliseconds(1500)} ago");

        // Untrusted text can neither forge a log line nor send terminal sequences
        logger.LogInformation("{0}", "login failed\ninfo      forged line \u001b[2J");

        try
        {
            throw new InvalidOperationException("An unhandled exception");
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
        }

        logger.For("Http").LogDebug("Exit sample...");
        logger.LogTimeStop(stopwatch, "Program finished");

        // Wait for the queued messages to be written
        fileLogger.Dispose();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo("notepad.exe", OutputFilePath) { UseShellExecute = true });
    }
}
