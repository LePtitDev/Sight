using System.IO;
using Sight.Logging.Loggers;

namespace Sight.Logging.Tests;

[TestFixture]
[NonParallelizable]
public class SanitizationTests
{
    private const string Esc = "\u001b";

    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "sight-logging-tests-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [Test]
    public void Console_CsiSequenceInText_IsEscaped()
    {
        var output = ConsoleOutput(l => l.Log("before " + Esc + "[2J" + Esc + "[31mred after"));

        Assert.IsTrue(Has(output, "before \\x1B[2J\\x1B[31mred after"), "missing: " + "before \\x1B[2J\\x1B[31mred after");
        Assert.AreEqual(0, CountOccurrences(output, Esc + "[2J"));
    }

    [Test]
    public void Console_OscSequenceInText_IsEscaped()
    {
        var output = ConsoleOutput(l => l.Log(Esc + "]0;owned\u0007" + Esc + "]8;;http://evil" + Esc + "\\click"));

        Assert.IsFalse(Has(output, "\u0007"), "unexpected: " + "\u0007");
        Assert.IsFalse(Has(output, Esc + "]"), "unexpected: " + Esc + "]");
        Assert.IsTrue(Has(output, "\\x1B]0;owned\\x07"), "missing: " + "\\x1B]0;owned\\x07");
    }

    [Test]
    public void Console_C1ControlAndBidiOverride_AreEscaped()
    {
        var output = ConsoleOutput(l => l.Log("a\u009b31mb\u202ec"));

        Assert.AreEqual("a\\x9B31mb\\u202Ec", output.TrimEnd());
    }

    [Test]
    public void Console_LoggerOwnColors_AreKept()
    {
        var output = ConsoleOutput(l => l.LogError("failure"));

        Assert.IsTrue(Has(output, Esc + "[31m"), "missing: " + Esc + "[31m");
        Assert.IsTrue(Has(output, "failure"), "missing: " + "failure");
    }

    [Test]
    public void Console_ExceptionMessageWithEscape_IsEscaped()
    {
        var output = ConsoleOutput(l => l.LogError(new InvalidOperationException("bad " + Esc + "[2J input")));

        Assert.IsTrue(Has(output, "bad \\x1B[2J input"), "missing: " + "bad \\x1B[2J input");
        Assert.AreEqual(0, CountOccurrences(output, Esc + "[2J"));
    }

    [Test]
    public void Console_NewLineInText_ContinuesIndentedUnderHeader()
    {
        var output = ConsoleOutput(l => l.LogInformation("login ok\ninfo      forged line"));

        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Assert.AreEqual(3, lines.Length);
        Assert.AreEqual(new string(' ', 14) + "info      forged line", lines[1]);
    }

    [Test]
    public void Console_CarriageReturn_CannotRewriteTheLine()
    {
        var output = ConsoleOutput(l => l.Log("visible\rhidden\r\nnext"));

        Assert.IsFalse(Has(output, "\r\n\r"), "unexpected: " + "\r\n\r");
        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Assert.AreEqual("visible", lines[0]);
        Assert.AreEqual(new string(' ', 14) + "hidden", lines[1]);
        Assert.AreEqual(new string(' ', 14) + "next", lines[2]);
    }

    [Test]
    public void Console_Tab_IsKept()
    {
        Assert.AreEqual("a\tb", ConsoleOutput(l => l.Log("a\tb")).TrimEnd());
    }

    [Test]
    public void File_ControlCharactersAndNewLines_AreSanitized()
    {
        var path = Path.Combine(_directory, "log.txt");

        using (var logger = new FileLogger(path, new FileLoggerOptions { TimestampFormat = null }))
            logger.Log("a " + Esc + "[2J b\nforged");

        var lines = File.ReadAllLines(path);
        Assert.AreEqual(2, lines.Length);
        Assert.AreEqual("a \\x1B[2J b", lines[0]);
        Assert.AreEqual(new string(' ', 14) + "forged", lines[1]);
    }

    [Test]
    public void File_HideStackTracePaths_RemovesDirectories()
    {
        var path = Path.Combine(_directory, "log.txt");
        var link = Log.HyperlinkLog("C:\\build\\agent\\Foo.cs#L12", "C:\\build\\agent\\Foo.cs:line 12");
        var urlLink = Log.HyperlinkLog("https://example.com/docs", "https://example.com/docs");

        using (var logger = new FileLogger(path, new FileLoggerOptions { HideStackTracePaths = true, TimestampFormat = null }))
            logger.Log(Log.RichLog("at ", link, " see ", urlLink));

        Assert.AreEqual("at Foo.cs:line 12 see https://example.com/docs", File.ReadAllText(path).TrimEnd());
    }

    [Test]
    public void File_ShowStackTracePathsByDefault()
    {
        var path = Path.Combine(_directory, "log.txt");

        using (var logger = new FileLogger(path, new FileLoggerOptions { TimestampFormat = null }))
            logger.Log(Log.HyperlinkLog("C:\\build\\Foo.cs#L12", "C:\\build\\Foo.cs:line 12"));

        Assert.AreEqual("C:\\build\\Foo.cs:line 12", File.ReadAllText(path).TrimEnd());
    }

    [Test]
    public void Console_Utf8Output_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new ConsoleLogger(new ConsoleLoggerOptions { Utf8Output = true }));
    }

    private static string ConsoleOutput(Action<ILogger> log)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter();
        Console.SetOut(output);
        Console.SetError(output);
        try
        {
            log(new ConsoleLogger(new ConsoleLoggerOptions { TimestampFormat = null, ColorMode = ConsoleColorMode.Always }));
            return output.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static bool Has(string text, string value) => text.IndexOf(value, StringComparison.Ordinal) >= 0;

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            count++;

        return count;
    }
}
