using Sight.Logging.Loggers;

namespace Sight.Logging.Tests;

[TestFixture]
public class MemoryLoggerTests
{
    [Test]
    public void Log_WithMaxCount_KeepsLatestMessagesOnly()
    {
        var logger = new MemoryLogger(2);

        logger.Log("1");
        logger.Log("2");
        logger.Log("3");
        logger.Log("4");

        CollectionAssert.AreEqual(new object[] { "3", "4" }, logger.GetMessages());
    }

    [Test]
    public void Log_WithoutMaxCount_KeepsEverything()
    {
        var logger = new MemoryLogger();

        for (var i = 0; i < 100; i++)
            logger.Log(i);

        Assert.AreEqual(100, logger.GetMessages().Length);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_NonPositiveMaxCount_Throws(int maxCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryLogger(maxCount));
    }
}
