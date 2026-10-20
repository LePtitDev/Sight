using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Tests;

[TestFixture]
public class FilteredLoggerTests
{
    [Test]
    public void Log_MessagesUnderMinimumLevel_AreIgnored()
    {
        var memory = new MemoryLogger();
        var logger = memory.WithMinimumLevel(LogLevels.Warning);

        logger.LogTrace("trace");
        logger.LogDebug("debug");
        logger.LogInformation("information");
        logger.LogWarning("warning");
        logger.LogError("error");
        logger.LogCritical("critical");

        var levels = memory.GetMessages().Select(m => m.Find<ILeveledLog>().Level.DefinedLevel).ToArray();
        Assert.AreEqual(new LogLevels?[] { LogLevels.Warning, LogLevels.Error, LogLevels.Critical }, levels);
    }

    [Test]
    public void Log_MessageWithoutLevel_IsInformation()
    {
        var memory = new MemoryLogger();

        memory.WithMinimumLevel(LogLevels.Information).Log("plain");
        memory.WithMinimumLevel(LogLevels.Warning).Log("plain but ignored");

        Assert.AreEqual(1, memory.GetMessages().Length);
    }

    [Test]
    public void MinimumLevel_CanBeChanged()
    {
        var memory = new MemoryLogger();
        var logger = memory.WithMinimumLevel(LogLevels.Error);

        logger.LogInformation("ignored");
        logger.MinimumLevel = LogLevels.Information;
        logger.LogInformation("kept");

        Assert.AreEqual(1, memory.GetMessages().Length);
    }

    [Test]
    public void IsEnabled_ReflectsMinimumLevel()
    {
        var logger = new MemoryLogger().WithMinimumLevel(LogLevels.Warning);

        Assert.IsFalse(logger.IsEnabled(LogLevels.Information));
        Assert.IsTrue(logger.IsEnabled(LogLevels.Warning));
        Assert.IsTrue(logger.IsEnabled(LogLevels.Critical));
    }

    [Test]
    public void IsEnabled_NestedFilters_UseTheMostRestrictive()
    {
        var logger = new MemoryLogger().WithMinimumLevel(LogLevels.Error).WithMinimumLevel(LogLevels.Debug);

        Assert.IsFalse(logger.IsEnabled(LogLevels.Warning));
        Assert.IsTrue(logger.IsEnabled(LogLevels.Error));
    }

    [Test]
    public void IsEnabled_LoggerWithoutFilter_IsAlwaysEnabled()
    {
        Assert.IsTrue(new MemoryLogger().IsEnabled(LogLevels.Trace));
    }

    [Test]
    public void MixedLogger_IsEnabled_WhenAnyLoggerIsEnabled()
    {
        var mixed = new MixedLogger(
            new MemoryLogger().WithMinimumLevel(LogLevels.Error),
            new MemoryLogger().WithMinimumLevel(LogLevels.Warning));

        Assert.IsFalse(mixed.IsEnabled(LogLevels.Information));
        Assert.IsTrue(mixed.IsEnabled(LogLevels.Warning));
    }

    [Test]
    public void MixedLogger_FiltersEachLoggerIndependently()
    {
        var verbose = new MemoryLogger();
        var quiet = new MemoryLogger();
        var mixed = new MixedLogger(verbose, quiet.WithMinimumLevel(LogLevels.Error));

        mixed.LogInformation("info");
        mixed.LogError("error");

        Assert.AreEqual(2, verbose.GetMessages().Length);
        Assert.AreEqual(1, quiet.GetMessages().Length);
    }

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FilteredLogger(null!, LogLevels.Debug));
    }

    [TestCase(LogLevels.Trace)]
    [TestCase(LogLevels.Critical)]
    public void NewLevels_AreOrderedAroundTheOthers(LogLevels level)
    {
        Assert.IsTrue(level == LogLevels.Trace ? level < LogLevels.Debug : level > LogLevels.Error);
    }
}
