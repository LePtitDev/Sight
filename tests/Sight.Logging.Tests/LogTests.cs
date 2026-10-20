namespace Sight.Logging.Tests;

[TestFixture]
public class LogTests
{
    [TestCase(0, "0ms")]
    [TestCase(1, "1ms")]
    [TestCase(250, "250ms")]
    [TestCase(1000, "1s")]
    [TestCase(1500, "1.500s")]
    [TestCase(1005, "1.005s")]
    [TestCase(60_000, "1m")]
    [TestCase(60_500, "1m 0.500s")]
    [TestCase(61_500, "1m 1.500s")]
    [TestCase(3_600_000, "1h")]
    [TestCase(90_061_000, "1d 1h 1m 1s")]
    [TestCase(-1500, "-1.500s")]
    public void Format_Milliseconds(long milliseconds, string expected)
    {
        Assert.AreEqual(expected, Log.Format(TimeSpan.FromMilliseconds(milliseconds)));
    }

    [Test]
    public void Format_SubMillisecond_UsesMicroseconds()
    {
        Assert.AreEqual("500µs", Log.Format(TimeSpan.FromTicks(5000)));
        Assert.AreEqual("0ms", Log.Format(TimeSpan.FromTicks(5)));
    }

    [Test]
    public void Format_ExtremeValues_DoNotThrow()
    {
        Assert.DoesNotThrow(() => Log.Format(TimeSpan.MinValue));
        Assert.DoesNotThrow(() => Log.Format(TimeSpan.MaxValue));
        StringAssert.StartsWith("-", Log.Format(TimeSpan.MinValue));
    }
}
