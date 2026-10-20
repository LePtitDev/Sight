using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Tests;

[TestFixture]
public class MixedLoggerTests
{
    [Test]
    public void Log_FailingLogger_DoesNotPreventTheNextOnes()
    {
        var memory = new MemoryLogger();
        var mixed = new MixedLogger(new ThrowingLogger(), memory);

        Assert.DoesNotThrow(() => mixed.LogInformation("hello"));

        Assert.AreEqual(1, memory.GetMessages().Length);
    }

    [Test]
    public void Dispose_DoesNotDisposeLoggersByDefault()
    {
        var inner = new TrackedLogger();

        new MixedLogger(inner).Dispose();

        Assert.AreEqual(0, inner.Disposed);
    }

    [Test]
    public void Dispose_OwnedLoggers_AreDisposedEvenIfOneThrows()
    {
        var first = new TrackedLogger { ThrowOnDispose = true };
        var second = new TrackedLogger();

        new MixedLogger(new ILogger[] { first, second }, ownsLoggers: true).Dispose();

        Assert.AreEqual(1, first.Disposed);
        Assert.AreEqual(1, second.Disposed);
    }

#if !NETFRAMEWORK
    [Test]
    public async Task DisposeAsync_OwnedLoggers_AreDisposed()
    {
        var inner = new TrackedLogger();

        await new MixedLogger(new ILogger[] { inner }, ownsLoggers: true).DisposeAsync();

        Assert.AreEqual(1, inner.Disposed);
    }
#endif

    [Test]
    public void LoggersList_IsCopied()
    {
        var list = new List<ILogger>();
        var mixed = new MixedLogger(list);
        var memory = new MemoryLogger();

        list.Add(memory);
        mixed.LogInformation("ignored");

        Assert.AreEqual(0, memory.GetMessages().Length);
    }

    [Test]
    public void LogPercentTime_IsATimedLog()
    {
        var memory = new MemoryLogger();

        memory.LogInformation("took %t", TimeSpan.FromSeconds(3));

        var timed = memory.GetMessages().Single().Find<ILeveledLog>().Content;
        var part = ((IRichLog)timed).Select(p => p.Find<ITimedLog>(true)).First(p => p != null);
        Assert.AreEqual(TimeSpan.FromSeconds(3), part!.Time);
    }

    private sealed class ThrowingLogger : ILogger
    {
        public void Log(object message) => throw new InvalidOperationException();
    }

    private sealed class TrackedLogger : ILogger, IDisposable
    {
        public int Disposed { get; private set; }

        public bool ThrowOnDispose { get; set; }

        public void Log(object message)
        {
        }

        public void Dispose()
        {
            Disposed++;
            if (ThrowOnDispose)
                throw new InvalidOperationException();
        }
    }
}
