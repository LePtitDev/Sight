using System.IO;
using System.Text.RegularExpressions;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Tests;

[TestFixture]
[NonParallelizable]
public class TimestampAndCategoryTests
{
    private static readonly DateTimeOffset Moment = new DateTimeOffset(2020, 1, 2, 3, 4, 5, 6, TimeSpan.Zero);

    private string _path = null!;

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(Path.GetTempPath(), "sight-logging-tests-" + Guid.NewGuid().ToString("N") + ".log");
    }

    [TearDown]
    public void TearDown()
    {
        File.Delete(_path);
    }

    [Test]
    public void FileLogger_ByDefault_WritesUtcIsoTimestamp()
    {
        using (var logger = new FileLogger(_path))
            logger.LogInformation("hello");

        StringAssert.IsMatch(@"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d{3}Z .*hello$", File.ReadAllText(_path).TrimEnd());
    }

    [Test]
    public void FileLogger_UsesTimestampCapturedWhenMessageWasCreated()
    {
        using (var logger = new FileLogger(_path))
            logger.Log(Log.TimestampedLog(Moment, "late"));

        Assert.AreEqual("2020-01-02T03:04:05.006Z late", File.ReadAllText(_path).TrimEnd());
    }

    [Test]
    public void FileLogger_CustomFormat()
    {
        using (var logger = new FileLogger(_path, new FileLoggerOptions { TimestampFormat = "yyyyMMdd HHmmss" }))
            logger.Log(Log.TimestampedLog(Moment, "x"));

        Assert.AreEqual("20200102 030405 x", File.ReadAllText(_path).TrimEnd());
    }

    [Test]
    public void FileLogger_LocalTime()
    {
        using (var logger = new FileLogger(_path, new FileLoggerOptions { TimestampFormat = "HH:mm", UtcTimestamp = false }))
            logger.Log(Log.TimestampedLog(Moment, "x"));

        Assert.AreEqual(Moment.ToLocalTime().ToString("HH:mm") + " x", File.ReadAllText(_path).TrimEnd());
    }

    [Test]
    public void FileLogger_ContinuationLinesAreIndentedUnderTheHeaderAfterTheTimestamp()
    {
        using (var logger = new FileLogger(_path, new FileLoggerOptions { TimestampFormat = "HH:mm" }))
            logger.Log(Log.TimestampedLog(Moment, "a\nb"));

        var lines = File.ReadAllLines(_path);
        Assert.AreEqual("03:04 a", lines[0]);
        Assert.AreEqual(new string(' ', 6 + 14) + "b", lines[1]);
    }

    [Test]
    public void ConsoleLogger_ByDefault_WritesLocalTimeWithMilliseconds()
    {
        var output = Capture(new ConsoleLogger(new ConsoleLoggerOptions { ColorMode = ConsoleColorMode.Always }), l => l.Log("hello"));

        StringAssert.IsMatch(@"^(\u001b\[90m)?\d\d:\d\d:\d\d\.\d{3} ", output);
    }

    [Test]
    public void Category_IsWrittenAfterTimestamp()
    {
        using (var logger = new FileLogger(_path))
            logger.For("Http").Log(Log.TimestampedLog(Moment, "x"));

        Assert.AreEqual("2020-01-02T03:04:05.006Z [Http] x", File.ReadAllText(_path).TrimEnd());
    }

    [Test]
    public void Category_NestedCategoriesAreJoined()
    {
        var memory = new MemoryLogger();

        memory.For("Http").For("Client").LogInformation("x");

        Assert.AreEqual("Http.Client", memory.GetMessages().Single().Find<ICategorizedLog>().Category);
    }

    [Test]
    public void Category_ForType_UsesTypeName()
    {
        var memory = new MemoryLogger();

        memory.For<TimestampAndCategoryTests>().LogInformation("x");

        Assert.AreEqual(nameof(TimestampAndCategoryTests), memory.GetMessages().Single().Find<ICategorizedLog>().Category);
    }

    [Test]
    public void Category_IsSanitized()
    {
        using (var logger = new FileLogger(_path, new FileLoggerOptions { TimestampFormat = null }))
            logger.For("a\u001b[2Jb").Log("x");

        Assert.AreEqual("[a\\x1B[2Jb] x", File.ReadAllText(_path).TrimEnd());
    }

    [Test]
    public void Category_FollowsLevelFiltering()
    {
        var logger = new MemoryLogger().WithMinimumLevel(LogLevels.Error).For("Http");

        logger.LogInformation("ignored");
        logger.LogError("kept");

        Assert.IsFalse(logger.IsEnabled(LogLevels.Warning));
        Assert.IsTrue(logger.IsEnabled(LogLevels.Error));
    }

    [Test]
    public void Category_DoesNotChangeMessageLevel()
    {
        var memory = new MemoryLogger();
        var logger = memory.For("Http").WithMinimumLevel(LogLevels.Warning);

        logger.LogInformation("ignored");
        logger.LogWarning("kept");

        Assert.AreEqual(1, memory.GetMessages().Length);
    }

    private static string Capture(ConsoleLogger logger, Action<ILogger> log)
    {
        var originalOut = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);
        try
        {
            log(logger);
            return output.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
