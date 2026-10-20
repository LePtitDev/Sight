using Sight.Logging.Loggers;

namespace Sight.Logging.Tests;

[TestFixture]
public class QueueLoggerTests
{
    [Test]
    public void WaitForMessage_ReturnsMessagesInOrder()
    {
        using var queue = new QueueLogger();

        queue.Log("1");
        queue.Log("2");

        Assert.AreEqual("1", queue.WaitForMessage());
        Assert.AreEqual("2", queue.WaitForMessage());
    }

    [Test]
    public async Task WaitForMessageAsync_WaitsForProducer()
    {
        using var queue = new QueueLogger();

        var task = queue.WaitForMessageAsync();
        Assert.IsFalse(task.IsCompleted);
        queue.Log("late");

        Assert.AreEqual("late", await task);
    }

    [Test]
    public void WaitForMessage_Timeout_ReturnsNull()
    {
        using var queue = new QueueLogger();

        Assert.IsNull(queue.WaitForMessage(TimeSpan.FromMilliseconds(20)));
    }

    [Test]
    public async Task WaitForMessageAsync_Timeout_ReturnsNull()
    {
        using var queue = new QueueLogger();

        Assert.IsNull(await queue.WaitForMessageAsync(TimeSpan.FromMilliseconds(20)));
    }

    [Test]
    public void Dispose_WakesUpPendingWaiter()
    {
        var queue = new QueueLogger();
        var task = queue.WaitForMessageAsync();

        queue.Dispose();

        Assert.ThrowsAsync<ObjectDisposedException>(async () => await task);
    }

    [Test]
    public void Dispose_QueuedMessagesCanStillBeConsumed()
    {
        var queue = new QueueLogger();
        queue.Log("kept");

        queue.Dispose();

        Assert.AreEqual("kept", queue.WaitForMessage());
        Assert.Throws<ObjectDisposedException>(() => queue.WaitForMessage());
    }

    [Test]
    public void Log_AfterDispose_DoesNotThrowAndIsIgnored()
    {
        var queue = new QueueLogger();
        queue.Dispose();

        Assert.DoesNotThrow(() => queue.Log("ignored"));
        Assert.Throws<ObjectDisposedException>(() => queue.WaitForMessage());
    }

    [Test]
    public void Dispose_Twice_DoesNotThrow()
    {
        var queue = new QueueLogger();

        queue.Dispose();

        Assert.DoesNotThrow(() => queue.Dispose());
    }

    [Test]
    public void Bounded_DropOldest_KeepsLatestMessages()
    {
        using var queue = new QueueLogger(new QueueOptions { Capacity = 2, FullMode = QueueFullMode.DropOldest });

        queue.Log("1");
        queue.Log("2");
        queue.Log("3");

        Assert.AreEqual("2", queue.WaitForMessage());
        Assert.AreEqual("3", queue.WaitForMessage());
    }

    [Test]
    public void Bounded_DropNewest_ReplacesNewestQueuedMessage()
    {
        using var queue = new QueueLogger(new QueueOptions { Capacity = 2, FullMode = QueueFullMode.DropNewest });

        queue.Log("1");
        queue.Log("2");
        queue.Log("3");

        // DropNewest replaces the newest queued message
        Assert.AreEqual("1", queue.WaitForMessage());
        Assert.AreEqual("3", queue.WaitForMessage());
        Assert.IsNull(queue.WaitForMessage(TimeSpan.FromMilliseconds(20)));
    }

    [Test]
    public void Bounded_Wait_BlocksProducerUntilConsumed()
    {
        using var queue = new QueueLogger(new QueueOptions { Capacity = 1, FullMode = QueueFullMode.Wait });
        queue.Log("1");

        var producer = Task.Run(() => queue.Log("2"));
        Assert.IsFalse(producer.Wait(100));

        Assert.AreEqual("1", queue.WaitForMessage());
        Assert.IsTrue(producer.Wait(TimeSpan.FromSeconds(5)));
        Assert.AreEqual("2", queue.WaitForMessage());
    }

    [Test]
    public void Constructor_NonPositiveCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new QueueLogger(new QueueOptions { Capacity = 0 }));
    }
}
