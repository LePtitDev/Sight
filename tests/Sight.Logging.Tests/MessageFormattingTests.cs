using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Tests;

[TestFixture]
public class MessageFormattingTests
{
    [Test]
    public void PositionalHole_UsesParameterByIndex()
    {
        Assert.AreEqual("b a b", Render(l => l.LogInformation("{1} {0} {1}", "a", "b")));
    }

    [Test]
    public void NamedHoles_TakeParametersInOrder()
    {
        Assert.AreEqual("user bob logged from 10.0.0.1", Render(l => l.LogInformation("user {user} logged from {address}", "bob", "10.0.0.1")));
    }

    [Test]
    public void Hole_AcceptsFormat_WithInvariantCulture()
    {
        Assert.AreEqual("pi=3.14 id=0042", Render(l => l.LogInformation("pi={pi:F2} id={1:D4}", Math.PI, 42)));
    }

    [Test]
    public void Hole_TimeSpanAndExceptionGetRichRendering()
    {
        var text = Render(l => l.LogInformation("took {elapsed} failed {error}", TimeSpan.FromSeconds(2), new InvalidOperationException("boom")));

        StringAssert.StartsWith("took 2s failed System.InvalidOperationException: boom", text);
    }

    [Test]
    public void Hole_NullParameter_IsWrittenAsNull()
    {
        Assert.AreEqual("value=null", Render(l => l.LogInformation("value={value}", new object[] { null! })));
    }

    [Test]
    public void DoubleBraces_AreLiterals()
    {
        Assert.AreEqual("{literal} 1", Render(l => l.LogInformation("{{literal}} {0}", 1)));
    }

    [Test]
    public void MalformedOrUnresolvedHoles_AreKeptLiteral()
    {
        Assert.AreEqual("{ } {} {5} {a b} {unclosed 1", Render(l => l.LogInformation("{ } {} {5} {a b} {unclosed {0}", 1)));
    }

    [Test]
    public void NamedHole_WithoutRemainingParameter_IsKeptLiteral()
    {
        Assert.AreEqual("a {b}", Render(l => l.LogInformation("{x} {b}", "a")));
    }

    [Test]
    public void BracesInMessageWithoutParams_AreKeptAsIs()
    {
        Assert.AreEqual("{\"json\": {}} {0}", Render(l => l.LogInformation("{\"json\": {}} {0}")));
    }

    [Test]
    public void Json_WithParams_IsNotMistakenForHoles()
    {
        Assert.AreEqual("{\"a\":1} 2", Render(l => l.LogInformation("{\"a\":1} {0}", 2)));
    }

#if NET6_0_OR_GREATER
    [Test]
    public void Interpolation_BuildsMessage()
    {
        var user = "bob";
        var count = 3;

        Assert.AreEqual("user bob has 3 items (1.50)", Render(l => l.LogInformation($"user {user} has {count} items ({1.5:F2})")));
    }

    [Test]
    public void Interpolation_RichValues()
    {
        var elapsed = TimeSpan.FromMilliseconds(1500);

        var text = Render(l => l.LogError($"failed after {elapsed}: {new InvalidOperationException("boom")}"));

        StringAssert.StartsWith("failed after 1.500s: System.InvalidOperationException: boom", text);
    }

    [Test]
    public void Interpolation_Alignment()
    {
        Assert.AreEqual("[  7][7  ]", Render(l => l.LogInformation($"[{7,3}][{7,-3}]")));
    }

    [Test]
    public void Interpolation_UsesMatchingLevel()
    {
        var memory = new MemoryLogger();

        memory.LogTrace($"a");
        memory.LogDebug($"a");
        memory.LogInformation($"a");
        memory.LogWarning($"a");
        memory.LogError($"a");
        memory.LogCritical($"a");

        var levels = memory.GetMessages().Select(m => m.Find<ILeveledLog>().Level.DefinedLevel).ToArray();
        Assert.AreEqual(
            new LogLevels?[] { LogLevels.Trace, LogLevels.Debug, LogLevels.Information, LogLevels.Warning, LogLevels.Error, LogLevels.Critical },
            levels);
    }

    [Test]
    public void Interpolation_FilteredLevel_DoesNotEvaluateValues()
    {
        var memory = new MemoryLogger();
        var logger = memory.WithMinimumLevel(LogLevels.Error);
        var calls = 0;

        logger.LogInformation($"value {Compute()}");

        Assert.AreEqual(0, calls);
        Assert.AreEqual(0, memory.GetMessages().Length);

        int Compute()
        {
            calls++;
            return 1;
        }
    }

    [Test]
    public void Interpolation_EnabledLevel_EvaluatesValues()
    {
        var memory = new MemoryLogger();
        var calls = 0;

        memory.LogInformation($"value {Compute()}");

        Assert.AreEqual(1, calls);
        Assert.AreEqual(1, memory.GetMessages().Length);

        int Compute()
        {
            calls++;
            return 1;
        }
    }
#endif

    private static string Render(Action<ILogger> log)
    {
        var logger = new MemoryLogger();
        log(logger);
        var parts = ((IRichLog)logger.GetMessages().Single().Find<ILeveledLog>().Content).ToList();
        return string.Concat(parts.Skip(2));
    }
}
