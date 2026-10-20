#nullable enable
#pragma warning disable CA2017 // Tests log messages whose placeholders deliberately do not match the arguments

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sight.Logging.Extensions;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Sight.Logging.Extensions.Tests;

[TestFixture]
public class SightLoggerProviderTests
{
    private MemoryLogger _memory = null!;

    [SetUp]
    public void SetUp()
    {
        _memory = new MemoryLogger();
    }

    [TestCase(LogLevel.Trace, LogLevels.Trace)]
    [TestCase(LogLevel.Debug, LogLevels.Debug)]
    [TestCase(LogLevel.Information, LogLevels.Information)]
    [TestCase(LogLevel.Warning, LogLevels.Warning)]
    [TestCase(LogLevel.Error, LogLevels.Error)]
    [TestCase(LogLevel.Critical, LogLevels.Critical)]
    public void Log_MapsLevels(LogLevel level, LogLevels expected)
    {
        using var factory = CreateFactory();

        factory.CreateLogger("Test").Log(level, "message");

        Assert.AreEqual(expected, Single().Find<ILeveledLog>().Level.DefinedLevel);
    }

    [Test]
    public void Log_FormatsStructuredMessage()
    {
        using var factory = CreateFactory();

        factory.CreateLogger("Test").LogInformation("User {User} has {Count} items", "bob", 3);

        Assert.AreEqual("User bob has 3 items", Text(Single()));
    }

    [Test]
    public void Log_MessageWithSpecialCharacters_IsWrittenAsIs()
    {
        using var factory = CreateFactory();

        factory.CreateLogger("Test").LogInformation("100% {braces} %t {0}");

        Assert.AreEqual("100% {braces} %t {0}", Text(Single()));
    }

    [Test]
    public void Log_ExceptionWithSpecialCharacters_IsAppended()
    {
        using var factory = CreateFactory();

        factory.CreateLogger("Test").LogWarning(new InvalidOperationException("boom"), "failed 100% {x}");

        StringAssert.StartsWith("failed 100% {x}: System.InvalidOperationException: boom", Text(Single()));
    }

    [Test]
    public void Log_ExceptionWithoutMessage_WritesTheException()
    {
        using var factory = CreateFactory();

        factory.CreateLogger("Test").Log(LogLevel.Error, new InvalidOperationException("boom"), string.Empty);

        StringAssert.StartsWith("System.InvalidOperationException: boom", Text(Single()));
    }

    [Test]
    public void Log_UsesTheCategoryName()
    {
        using var factory = CreateFactory();

        factory.CreateLogger("My.Category").LogInformation("x");

        Assert.AreEqual("My.Category", Single().Find<ICategorizedLog>().Category);
    }

    [Test]
    public void IsEnabled_FollowsTheSightLogger()
    {
        using var factory = CreateFactory(_memory.WithMinimumLevel(LogLevels.Warning));
        var logger = factory.CreateLogger("Test");

        Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Warning));
        Assert.IsFalse(logger.IsEnabled(LogLevel.None));

        logger.LogInformation("ignored");
        Assert.AreEqual(0, _memory.GetMessages().Length);
    }

    [Test]
    public void BeginScope_IsSupportedAsNoOp()
    {
        using var factory = CreateFactory();

        using (factory.CreateLogger("Test").BeginScope("scope"))
        {
        }
    }

    [Test]
    public void AddSight_WithFactory_DisposesTheLoggerWithTheProvider()
    {
        var tracked = new TrackedLogger();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSight(_ => tracked));

        using (var provider = services.BuildServiceProvider())
            provider.GetRequiredService<ILoggerFactory>().CreateLogger("Test").LogInformation("x");

        Assert.AreEqual(1, tracked.Messages);
        Assert.AreEqual(1, tracked.Disposed);
    }

    [Test]
    public void AddSight_WithInstance_DoesNotDisposeTheLogger()
    {
        var tracked = new TrackedLogger();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSight(tracked));

        using (var provider = services.BuildServiceProvider())
            provider.GetRequiredService<ILoggerFactory>().CreateLogger("Test").LogInformation("x");

        Assert.AreEqual(0, tracked.Disposed);
    }

    [Test]
    public void AddSight_NullArguments_Throw()
    {
        var services = new ServiceCollection();
        services.AddLogging(b =>
        {
            Assert.Throws<ArgumentNullException>(() => b.AddSight((Sight.Logging.ILogger)null!));
            Assert.Throws<ArgumentNullException>(() => b.AddSight((Func<IServiceProvider, Sight.Logging.ILogger>)null!));
        });
    }

    private ILoggerFactory CreateFactory(Sight.Logging.ILogger? logger = null)
    {
        return LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace).AddSight(logger ?? _memory));
    }

    private object Single() => _memory.GetMessages().Single();

    private static string Text(object message)
    {
        var parts = ((IRichLog)message.Find<ILeveledLog>().Content).ToList();
        return string.Concat(parts.Skip(2));
    }

    private sealed class TrackedLogger : Sight.Logging.ILogger, IDisposable
    {
        public int Messages { get; private set; }

        public int Disposed { get; private set; }

        public void Log(object message) => Messages++;

        public void Dispose() => Disposed++;
    }
}
