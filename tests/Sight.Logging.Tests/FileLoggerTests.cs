using System.IO;
using Sight.Logging.Loggers;

namespace Sight.Logging.Tests;

[TestFixture]
public class FileLoggerTests
{
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
    public void Constructor_MissingDirectory_CreatesIt()
    {
        var path = Path.Combine(_directory, "sub", "log.txt");

        using (var logger = new FileLogger(path, new FileLoggerOptions { TimestampFormat = null }))
            logger.Log("hello");

        Assert.IsTrue(File.Exists(path));
    }

    [Test]
    public void Constructor_ExistingFileByDefault_AppendsToIt()
    {
        var path = Path.Combine(_directory, "log.txt");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "previous line\n");

        using (var logger = new FileLogger(path, new FileLoggerOptions { TimestampFormat = null }))
            logger.Log("new line");

        Assert.AreEqual(new[] { "previous line", "new line" }, File.ReadAllLines(path));
    }

    [Test]
    public void Constructor_ExistingFileWithoutAppend_TruncatesIt()
    {
        var path = Path.Combine(_directory, "log.txt");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "a very long previous content that must not survive\nsecond line\n");

        using (var logger = new FileLogger(path, new FileLoggerOptions { Append = false, TimestampFormat = null }))
            logger.Log("new");

        Assert.AreEqual(new[] { "new" }, File.ReadAllLines(path));
    }

    [Test]
    public void Log_WhileOpen_FileCanBeRead()
    {
        var path = Path.Combine(_directory, "log.txt");

        using var logger = new FileLogger(path, new FileLoggerOptions { TimestampFormat = null });
        logger.Log("tailed");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        Assert.AreEqual("tailed", reader.ReadLine());
    }

    [Test]
    public void Log_FromManyThreads_WritesEachMessageAsOneLine()
    {
        var path = Path.Combine(_directory, "log.txt");
        const int threads = 8;
        const int count = 500;

        using (var logger = new FileLogger(path, new FileLoggerOptions { TimestampFormat = null }))
        {
            Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
            {
                for (var i = 0; i < count; i++)
                    logger.Log($"thread {t} message {i}");
            });
        }

        var lines = File.ReadAllLines(path);
        Assert.AreEqual(threads * count, lines.Length);
        Assert.AreEqual(threads * count, lines.Distinct().Count());
        Assert.IsTrue(lines.All(l => l.StartsWith("thread ")));
    }
}
