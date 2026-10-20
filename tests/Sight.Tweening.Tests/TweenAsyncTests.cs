namespace Sight.Tweening.Tests;

public class TweenAsyncTests
{
    private static ITweenAnimation Animation(TimeSpan duration) =>
        Tween.Property<TestState, double>(s => s.X).To(10, duration).Build();

    [Test]
    [CancelAfter(10_000)]
    public async Task Test_PlayAsync()
    {
        var state = new TestState();
        await Animation(TimeSpan.FromMilliseconds(100)).PlayAsync(state, TimeSpan.FromMilliseconds(5));

        Assert.That(state.X, Is.EqualTo(10));
    }

    [Test]
    [CancelAfter(10_000)]
    public void Test_PlayAsyncCanceled()
    {
        var state = new TestState();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        Assert.That(
            async () => await Animation(TimeSpan.FromSeconds(30)).PlayAsync(state, TimeSpan.FromMilliseconds(5), cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(state.X, Is.LessThan(10));
    }

    [Test]
    [CancelAfter(10_000)]
    public async Task Test_TimerContextCompletes()
    {
        var state = new TestState();
        using var context = Animation(TimeSpan.FromMilliseconds(100)).CreateTimerContext(state, TimeSpan.FromMilliseconds(5));

        context.Start();
        await context.Completion;

        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(10));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Completed));
        });
    }

    [Test]
    [CancelAfter(10_000)]
    public async Task Test_TimerContextPostsToSynchronizationContext()
    {
        var state = new TestState();
        var synchronizationContext = new CountingSynchronizationContext();
        using var context = Animation(TimeSpan.FromMilliseconds(100)).CreateTimerContext(state, TimeSpan.FromMilliseconds(5), synchronizationContext);

        context.Start();
        await context.Completion;

        Assert.That(synchronizationContext.PostCount, Is.GreaterThan(0));
    }

    private sealed class CountingSynchronizationContext : SynchronizationContext
    {
        private int _postCount;

        public int PostCount => _postCount;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _postCount);
            base.Post(d, state);
        }
    }

    private class TestState
    {
        public double X { get; set; }
    }
}
