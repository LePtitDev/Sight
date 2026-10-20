using System.Diagnostics;
using System.IO;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Tests;

[TestFixture]
public class LazyBuildingTests
{
    [Test]
    public void Extensions_DisabledLevel_DoNotBuildNorLogMessage()
    {
        var inner = new MemoryLogger();
        var logger = inner.WithMinimumLevel(LogLevels.Critical);
        var param = new CountingParam();

        logger.LogInformation("value: %t", param);
        logger.LogTimeStop(Stopwatch.StartNew(), "stop %t", param);
        logger.LogError(new InvalidOperationException(), "failure %t", param);

        Assert.AreEqual(0, param.ToStringCalls);
        Assert.AreEqual(0, inner.GetMessages().Length);
    }

    [Test]
    public void Extensions_EnabledLevel_BuildMessage()
    {
        var logger = new MemoryLogger().WithMinimumLevel(LogLevels.Error);
        var param = new CountingParam();

        logger.LogError("value: %t", param);

        Assert.AreEqual(1, param.ToStringCalls);
    }

    [Test]
    public void FileLogger_WithoutAutoFlush_WritesOnFlush()
    {
        var path = Path.Combine(Path.GetTempPath(), "sight-logging-tests-" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            using var logger = new FileLogger(path, new FileLoggerOptions { AutoFlush = false, TimestampFormat = null });
            logger.Log("buffered");

            Assert.AreEqual(string.Empty, ReadShared(path));

            logger.Flush();

            Assert.AreEqual("buffered", ReadShared(path).TrimEnd());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void FileLogger_BackgroundMode_FlushesWhenQueueIsDrained()
    {
        var path = Path.Combine(Path.GetTempPath(), "sight-logging-tests-" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            using var logger = new FileLogger(path, new FileLoggerOptions { CreateThread = true, TimestampFormat = null });
            logger.Log("queued");

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (ReadShared(path).Length == 0 && DateTime.UtcNow < deadline)
                System.Threading.Thread.Sleep(10);

            Assert.AreEqual("queued", ReadShared(path).TrimEnd());
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class CountingParam
    {
        public int ToStringCalls { get; private set; }

        public override string ToString()
        {
            ToStringCalls++;
            return "param";
        }
    }
}
