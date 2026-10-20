using System.IO;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Tests;

[TestFixture]
[NonParallelizable]
public class ConsoleRenderingTests
{
    private const string Esc = "\u001b";

    [Test]
    public void ColorModeNever_WritesNoSequence()
    {
        var output = Write(ConsoleColorMode.Never, l =>
        {
            l.LogError("failure");
            l.Log(Log.HyperlinkLog("C:\\x\\Foo.cs#L1", "C:\\x\\Foo.cs:line 1"));
        });

        Assert.IsFalse(Has(output, Esc));
        Assert.IsTrue(Has(output, "failure"));
        Assert.IsTrue(Has(output, "C:\\x\\Foo.cs:line 1"));
    }

    [Test]
    public void ColorModeAlways_WritesColors()
    {
        var output = Write(ConsoleColorMode.Always, l => l.LogWarning("careful"));

        Assert.IsTrue(Has(output, Esc + "[33m"));
    }

    [Test]
    public void ColorModeAuto_RedirectedOutput_WritesNoSequence()
    {
        // Test runners redirect the standard streams
        if (!Console.IsOutputRedirected)
            Assert.Ignore("Standard output is not redirected");

        var output = Write(ConsoleColorMode.Auto, l => l.LogInformation("plain"));

        Assert.IsFalse(Has(output, Esc));
    }

    [Test]
    public void ColorModeAuto_NoColorEnvironmentVariable_WritesNoSequence()
    {
        var previous = Environment.GetEnvironmentVariable("NO_COLOR");
        Environment.SetEnvironmentVariable("NO_COLOR", "1");
        try
        {
            var output = Write(ConsoleColorMode.Auto, l => l.LogInformation("plain"));

            Assert.IsFalse(Has(output, Esc));
        }
        finally
        {
            Environment.SetEnvironmentVariable("NO_COLOR", previous);
        }
    }

    [Test]
    public void Hyperlink_FilePath_IsWrittenAsOsc8Link()
    {
        var path = Path.Combine(Path.GetTempPath(), "Foo.cs");
        var output = Write(ConsoleColorMode.Always, l => l.Log(Log.HyperlinkLog(path + "#L12", path + ":line 12")));

        var expectedUri = new Uri(path).AbsoluteUri + "#L12";
        Assert.IsTrue(Has(output, Esc + "]8;;" + expectedUri + Esc + "\\"), output);
        Assert.IsTrue(Has(output, Esc + "]8;;" + Esc + "\\"), "link must be closed");
    }

    [Test]
    public void Hyperlink_Url_IsKept()
    {
        var output = Write(ConsoleColorMode.Always, l => l.Log(Log.HyperlinkLog("https://example.com/a b", "docs")));

        Assert.IsTrue(Has(output, Esc + "]8;;https://example.com/a%20b" + Esc + "\\"), output);
    }

    [Test]
    public void Hyperlink_WithControlCharacters_AreEscapedInTheLink()
    {
        var output = Write(ConsoleColorMode.Always, l => l.Log(Log.HyperlinkLog("https://evil/" + Esc + "\\" + Esc + "[2J", "click")));

        Assert.IsTrue(Has(output, "https://evil/%1B"));
        Assert.IsFalse(Has(output, Esc + "[2J"));
        Assert.IsTrue(Has(output, "click"));
    }

    [Test]
    public void Hyperlink_RelativeReference_IsNotLinked()
    {
        var output = Write(ConsoleColorMode.Always, l => l.Log(Log.HyperlinkLog("relative/path", "text")));

        Assert.IsFalse(Has(output, Esc + "]8"));
    }

    [Test]
    public void Hyperlink_HiddenPath_DoesNotLeakTheDirectoryInTheLink()
    {
        var output = Write(
            new ConsoleLoggerOptions { TimestampFormat = null, ColorMode = ConsoleColorMode.Always, HideStackTracePaths = true },
            l => l.Log(Log.HyperlinkLog("C:\\secret\\Foo.cs#L1", "C:\\secret\\Foo.cs:line 1")));

        Assert.IsFalse(Has(output, "secret"));
        Assert.IsTrue(Has(output, "Foo.cs:line 1"));
    }

    [Test]
    public void Hyperlinks_Disabled_OnlyUnderlines()
    {
        var output = Write(
            new ConsoleLoggerOptions { TimestampFormat = null, ColorMode = ConsoleColorMode.Always, Hyperlinks = false },
            l => l.Log(Log.HyperlinkLog("https://example.com", "docs")));

        Assert.IsFalse(Has(output, Esc + "]8"));
        Assert.IsTrue(Has(output, Esc + "[4mdocs" + Esc + "[24m"));
    }

    [Test]
    public void CustomIcon_IsRenderedWithItsValue_AndSanitized()
    {
        var output = Write(ConsoleColorMode.Never, l => l.Log(Log.RichLog(Log.Icon("*"), " a ", Log.Icon("b" + Esc + "c"))));

        Assert.AreEqual("* a b\\x1Bc", output.TrimEnd());
    }

    [Test]
    public void CustomColor_ConsoleColorAndRgb()
    {
        var output = Write(ConsoleColorMode.Always, l => l.Log(Log.RichLog(
            Log.ColoredLog(Log.Color(ConsoleColor.Red), "a"),
            Log.ColoredLog(Log.Color(((byte)1, (byte)2, (byte)3)), "b"),
            Log.ColoredLog(Log.Color("#0A0B0C"), "c"),
            Log.ColoredLog(Log.Color(new object()), "d"))));

        Assert.IsTrue(Has(output, Esc + "[91ma" + Esc + "[0m"));
        Assert.IsTrue(Has(output, Esc + "[38;2;1;2;3mb" + Esc + "[0m"));
        Assert.IsTrue(Has(output, Esc + "[38;2;10;11;12mc" + Esc + "[0m"));
        Assert.IsTrue(Has(output, "d"));
    }

    [Test]
    public void LogIconToString_ReturnsTheValue()
    {
        Assert.AreEqual("Warning", Log.Icon(LogIcons.Warning).ToString());
        Assert.AreEqual("*", Log.Icon("*").ToString());
    }

    [Test]
    public void File_CustomIcon_IsRenderedWithItsValue()
    {
        var path = Path.Combine(Path.GetTempPath(), "sight-logging-tests-" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            using (var logger = new FileLogger(path, new FileLoggerOptions { TimestampFormat = null }))
                logger.Log(Log.RichLog(Log.Icon("*"), " text"));

            Assert.AreEqual("* text", File.ReadAllText(path).TrimEnd());
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string Write(ConsoleColorMode mode, Action<ILogger> log)
    {
        return Write(new ConsoleLoggerOptions { TimestampFormat = null, ColorMode = mode }, log);
    }

    private static string Write(ConsoleLoggerOptions options, Action<ILogger> log)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter();
        Console.SetOut(output);
        Console.SetError(output);
        try
        {
            log(new ConsoleLogger(options));
            return output.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static bool Has(string text, string value) => text.IndexOf(value, StringComparison.Ordinal) >= 0;
}
