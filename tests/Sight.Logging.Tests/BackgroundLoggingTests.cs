using System.IO;
using System.Threading;
using Sight.Logging.Loggers;

namespace Sight.Logging.Tests;

[TestFixture]
public class BackgroundLoggingTests
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
    public void FileLogger_ConcurrentProducersThenDispose_WritesEveryMessageOnceInProducerOrder()
    {
        var path = Path.Combine(_directory, "log.txt");
        const int producers = 8;
        const int count = 2000;

        using (var logger = new FileLogger(path, new FileLoggerOptions { CreateThread = true, TimestampFormat = null }))
        {
            var tasks = Enumerable.Range(0, producers)
                .Select(p => Task.Run(() =>
                {
                    for (var i = 0; i < count; i++)
                        logger.Log($"p{p} {i}");
                }))
                .ToArray();

            Task.WaitAll(tasks);
        }

        var lines = File.ReadAllLines(path);
        Assert.AreEqual(producers * count, lines.Length);
        for (var p = 0; p < producers; p++)
        {
            var sequence = lines.Where(l => l.StartsWith($"p{p} ")).Select(l => int.Parse(l.Substring(l.IndexOf(' ') + 1))).ToArray();
            Assert.AreEqual(Enumerable.Range(0, count).ToArray(), sequence, $"producer {p}");
        }
    }

    [Test]
    public void FileLogger_ProducersRunningDuringDispose_DoNotThrow()
    {
        var path = Path.Combine(_directory, "log.txt");
        var logger = new FileLogger(path, new FileLoggerOptions { CreateThread = true, TimestampFormat = null });
        var stop = false;
        var producer = Task.Run(() =>
        {
            while (!Volatile.Read(ref stop))
                logger.Log("message");
        });

        Thread.Sleep(50);
        logger.Dispose();
        Assert.DoesNotThrow(() => logger.Log("after dispose"));
        Volatile.Write(ref stop, true);

        Assert.DoesNotThrow(() => producer.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(File.ReadAllLines(path).All(l => l == "message"));
    }

    [Test]
    public void FileLogger_LogAfterDispose_DoesNotThrow([Values] bool createThread)
    {
        var logger = new FileLogger(Path.Combine(_directory, "log.txt"), new FileLoggerOptions { CreateThread = createThread, TimestampFormat = null });
        logger.Dispose();

        Assert.DoesNotThrow(() => logger.Log("ignored"));
        Assert.DoesNotThrow(() => logger.Dispose());
    }

    [Test]
    public void FileLogger_BoundedQueueDropOldest_KeepsProcessing()
    {
        var path = Path.Combine(_directory, "log.txt");

        using (var logger = new FileLogger(path, new FileLoggerOptions { CreateThread = true, Queue = new QueueOptions { Capacity = 4, FullMode = QueueFullMode.DropOldest }, TimestampFormat = null }))
        {
            for (var i = 0; i < 1000; i++)
                logger.Log($"message {i}");
        }

        var lines = File.ReadAllLines(path);
        Assert.IsNotEmpty(lines);
        Assert.AreEqual("message 999", lines.Last());
    }

#if !NETFRAMEWORK
    [Test]
    public async Task FileLogger_DisposeAsync_DrainsQueue()
    {
        var path = Path.Combine(_directory, "log.txt");
        var logger = new FileLogger(path, new FileLoggerOptions { CreateThread = true, TimestampFormat = null });
        for (var i = 0; i < 100; i++)
            logger.Log($"message {i}");

        await logger.DisposeAsync();

        Assert.AreEqual(100, File.ReadAllLines(path).Length);
    }
#endif

    [Test]
    [NonParallelizable]
    public void ConsoleLogger_ConcurrentProducersThenDispose_WritesEveryMessageOnce()
    {
        var original = Console.Out;
        var output = new StringWriter();
        Console.SetOut(output);
        try
        {
            const int producers = 4;
            const int count = 500;
            using (var logger = new ConsoleLogger(new ConsoleLoggerOptions { CreateThread = true, TimestampFormat = null }))
            {
                Task.WaitAll(Enumerable.Range(0, producers).Select(p => Task.Run(() =>
                {
                    for (var i = 0; i < count; i++)
                        logger.Log($"p{p} {i}");
                })).ToArray());
            }

            var lines = output.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(producers * count, lines.Length);
            Assert.AreEqual(producers * count, lines.Distinct().Count());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Test]
    public void ConsoleLogger_LogAfterDispose_DoesNotThrow()
    {
        var logger = new ConsoleLogger(new ConsoleLoggerOptions { CreateThread = true, TimestampFormat = null });
        logger.Dispose();

        Assert.DoesNotThrow(() => logger.Log("ignored"));
    }
}
