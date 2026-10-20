using System.Diagnostics;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Tests;

[TestFixture]
public class LoggerExtTests
{
    private static readonly TimeSpan Elapsed = TimeSpan.FromSeconds(2);

    [Test]
    public void LogInformation_PlainMessage_KeepsMessage()
    {
        Assert.AreEqual("Hello World!", Render(l => l.LogInformation("Hello World!")));
    }

    [Test]
    public void LogInformation_PlainMessageWithPercent_IsNotParsedWithoutParams()
    {
        Assert.AreEqual("100% done in %t", Render(l => l.LogInformation("100% done in %t")));
    }

    [Test]
    public void LogInformation_TimePlaceholderAmongPlainText_DoesNotMistakeLettersForPlaceholders()
    {
        Assert.AreEqual("Took 2s total", Render(l => l.LogInformation("Took %t total", Elapsed)));
    }

    [Test]
    public void LogInformation_PercentThenPlaceholder_ConsumesParam()
    {
        Assert.AreEqual("100% done in 2s", Render(l => l.LogInformation("100% done in %t", Elapsed)));
    }

    [Test]
    public void LogInformation_DoublePercent_IsLiteralPercentAndLeavesEscapeMode()
    {
        Assert.AreEqual("100% test 2s", Render(l => l.LogInformation("100%% test %t", Elapsed)));
    }

    [Test]
    public void LogInformation_UnknownEscape_IsKeptLiteral()
    {
        Assert.AreEqual("a %x b 2s", Render(l => l.LogInformation("a %x b %t", Elapsed)));
    }

    [Test]
    public void LogInformation_TrailingPercent_IsKeptLiteral()
    {
        Assert.AreEqual("done 2s %", Render(l => l.LogInformation("done %t %", Elapsed)));
    }

    [Test]
    public void LogInformation_PlaceholderWithoutParam_IsKeptLiteral()
    {
        Assert.AreEqual("a 2s b %t c %e", Render(l => l.LogInformation("a %t b %t c %e", Elapsed)));
    }

    [Test]
    public void LogInformation_ParamOfWrongType_IsRenderedWithToString()
    {
        Assert.AreEqual("time: 42 error: oops", Render(l => l.LogInformation("time: %t error: %e", 42, "oops")));
    }

    [Test]
    public void LogInformation_Exception_IsRendered()
    {
        var exception = new InvalidOperationException("boom");

        var text = Render(l => l.LogInformation("failed: %e!", exception));

        StringAssert.StartsWith("failed: System.InvalidOperationException: boom", text);
        StringAssert.EndsWith("!", text);
    }

    [Test]
    public void LogInformation_TextIsNotSplitPerCharacter()
    {
        var logger = new MemoryLogger();

        logger.LogInformation("Took %t total, really", Elapsed);

        // icon+type, padding, "Took ", time, " total, really"
        Assert.AreEqual(5, GetParts(logger.GetMessages().Single()).Count);
    }

    [Test]
    public void LogTimeStop_WithUserParams_KeepsUserParamAndAppendsElapsed()
    {
        var stopwatch = Stopwatch.StartNew();

        var text = Render(l => l.LogTimeStop(stopwatch, "Request %t", TimeSpan.FromSeconds(1)));

        StringAssert.StartsWith("Request 1s: ", text);
        Assert.AreNotEqual("Request 1s: ", text);
    }

    [Test]
    public void LogTimeStop_WithoutParams_AppendsElapsed()
    {
        var text = Render(l => l.LogTimeStop(Stopwatch.StartNew(), "Done"));

        StringAssert.StartsWith("Done: ", text);
        Assert.Greater(text.Length, "Done: ".Length);
    }

    [Test]
    public void LogError_ExceptionWithUserParams_KeepsUserParamAndAppendsException()
    {
        var text = Render(l => l.LogError(new InvalidOperationException("boom"), "Request %t", TimeSpan.FromSeconds(1)));

        StringAssert.StartsWith("Request 1s: System.InvalidOperationException: boom", text);
    }

    [Test]
    public void LogError_ExceptionWithoutMessage_IsRenderedAlone()
    {
        var text = Render(l => l.LogError(new InvalidOperationException("boom")));

        StringAssert.StartsWith("System.InvalidOperationException: boom", text);
    }

    [TestCase(nameof(LoggerExt.LogDebug), LogLevels.Debug)]
    [TestCase(nameof(LoggerExt.LogInformation), LogLevels.Information)]
    [TestCase(nameof(LoggerExt.LogWarning), LogLevels.Warning)]
    [TestCase(nameof(LoggerExt.LogError), LogLevels.Error)]
    public void LogMethods_UseMatchingLevel(string method, LogLevels expected)
    {
        var logger = new MemoryLogger();

        typeof(LoggerExt).GetMethod(method, new[] { typeof(ILogger), typeof(string), typeof(object[]) })!
            .Invoke(null, new object[] { logger, "message", Array.Empty<object>() });

        var message = logger.GetMessages().Single().Find<ILeveledLog>();
        Assert.AreEqual(expected, message.Level.DefinedLevel);
    }

    private static IReadOnlyList<object> GetParts(object message)
    {
        return ((IRichLog)message.Find<ILeveledLog>().Content).ToList();
    }

    /// <summary>
    /// Render the message text, without the icon/type/padding prefix
    /// </summary>
    private static string Render(Action<ILogger> log)
    {
        var logger = new MemoryLogger();
        log(logger);
        return string.Concat(GetParts(logger.GetMessages().Single()).Skip(2));
    }
}
