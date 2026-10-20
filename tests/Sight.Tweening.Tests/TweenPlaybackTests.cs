namespace Sight.Tweening.Tests;

public class TweenPlaybackTests
{
    private static TweenManualAnimationContext CreateContext(TestState state)
    {
        return Tween.Property<TestState, double>(s => s.X)
            .To(10, TimeSpan.FromSeconds(10))
            .Build()
            .CreateManualContext(state);
    }

    [Test]
    public void Test_StartStopResume()
    {
        var state = new TestState();
        var context = CreateContext(state);
        Assert.That(context.State, Is.EqualTo(TweenAnimationState.Idle));

        context.Tick(1);
        Assert.That(state.X, Is.EqualTo(0), "Ticks are ignored before start");

        context.Start();
        context.Tick(2);
        Assert.Multiple(() =>
        {
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Running));
            Assert.That(state.X, Is.EqualTo(2).Within(1e-9));
        });

        context.Stop();
        context.Tick(2);
        Assert.Multiple(() =>
        {
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Paused));
            Assert.That(state.X, Is.EqualTo(2).Within(1e-9));
        });

        context.Start();
        context.Tick(2);
        Assert.That(state.X, Is.EqualTo(4).Within(1e-9));
    }

    [Test]
    public void Test_SeekAndProgress()
    {
        var state = new TestState();
        var context = CreateContext(state);

        context.Seek(TimeSpan.FromSeconds(7));
        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(7).Within(1e-9));
            Assert.That(context.Progress, Is.EqualTo(0.7).Within(1e-9));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Paused));
        });

        context.SeekProgress(0.25);
        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(context.Time, Is.EqualTo(TimeSpan.FromSeconds(2.5)));
        });

        context.Seek(TimeSpan.FromSeconds(50));
        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(10).Within(1e-9));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Paused), "Seeking does not complete");
        });
    }

    [Test]
    public void Test_TimeScale()
    {
        var state = new TestState();
        var context = CreateContext(state);
        context.TimeScale = 2;
        context.Start();
        context.Tick(1);

        Assert.That(state.X, Is.EqualTo(2).Within(1e-9));
        Assert.Throws<ArgumentOutOfRangeException>(() => context.TimeScale = -1);
    }

    [Test]
    public void Test_Reverse()
    {
        var state = new TestState();
        var context = CreateContext(state);
        var completed = 0;
        context.Completed += (_, _) => completed++;

        context.Start();
        context.Tick(4);
        context.Reverse();
        context.Tick(1);
        Assert.That(state.X, Is.EqualTo(3).Within(1e-9));

        context.Tick(5);
        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(0).Within(1e-9));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Completed));
            Assert.That(completed, Is.EqualTo(1));
        });
    }

    [Test]
    public void Test_StartReversedFromIdle()
    {
        var state = new TestState();
        var context = CreateContext(state);
        context.IsReversed = true;
        context.Start();
        Assert.That(state.X, Is.EqualTo(10).Within(1e-9));

        context.Tick(4);
        Assert.That(state.X, Is.EqualTo(6).Within(1e-9));
    }

    [Test]
    public void Test_CompleteAndEvents()
    {
        var state = new TestState();
        var context = CreateContext(state);
        var events = new List<string>();
        context.Started += (_, _) => events.Add("started");
        context.Stopped += (_, _) => events.Add("stopped");
        context.Completed += (_, _) => events.Add("completed");

        context.Start();
        context.Stop();
        context.Start();
        context.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(10));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Completed));
            Assert.That(events, Is.EqualTo(new[] { "started", "stopped", "started", "completed" }));
            Assert.That(context.Completion.IsCompleted, Is.True);
        });
    }

    [Test]
    public void Test_UpdatedEvent()
    {
        var state = new TestState();
        var context = CreateContext(state);
        var count = 0;
        context.Updated += (_, _) => count++;

        context.Start();
        context.Tick(1);
        context.Tick(1);

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public void Test_ResetRestoresValues()
    {
        var state = new TestState { X = 3 };
        var context = CreateContext(state);
        var completion = context.Completion;

        context.Start();
        context.Tick(5);
        context.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(3).Within(1e-9));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Idle));
            Assert.That(context.Time, Is.EqualTo(TimeSpan.Zero));
            Assert.That(completion.IsCanceled, Is.True);
        });
    }

    [Test]
    public void Test_RestartAfterCompletion()
    {
        var state = new TestState();
        var context = CreateContext(state);
        context.Start();
        context.Tick(20);
        Assert.That(context.State, Is.EqualTo(TweenAnimationState.Completed));

        context.Start();
        Assert.Multiple(() =>
        {
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Running));
            Assert.That(state.X, Is.EqualTo(0).Within(1e-9));
            Assert.That(context.Completion.IsCompleted, Is.False);
        });

        context.Restart();
        context.Tick(3);
        Assert.That(state.X, Is.EqualTo(3).Within(1e-9));
    }

    [Test]
    public async Task Test_CompletionTask()
    {
        var state = new TestState();
        var context = CreateContext(state);
        context.Start();
        var completion = context.Completion;
        Assert.That(completion.IsCompleted, Is.False);

        context.Tick(10);
        await completion;

        Assert.That(state.X, Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void Test_DisposeCancelsCompletion()
    {
        var context = CreateContext(new TestState());
        context.Start();
        context.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(context.Completion.IsCanceled, Is.True);
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Paused));
        });
        Assert.Throws<ObjectDisposedException>(context.Start);
    }

    [Test]
    public void Test_InfiniteAnimation()
    {
        var context = Tween.Property<TestState, double>(s => s.X)
            .To(10, TimeSpan.FromSeconds(1))
            .Loops(Tween.Infinite)
            .Build()
            .CreateManualContext(new TestState());

        context.Start();
        context.Tick(1000);

        Assert.Multiple(() =>
        {
            Assert.That(context.Duration, Is.EqualTo(TimeSpan.MaxValue));
            Assert.That(context.Progress, Is.EqualTo(0));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Running));
        });
        Assert.Throws<InvalidOperationException>(context.Complete);
    }

#if NET5_0_OR_GREATER
    [Test]
    public void Test_TypedUpdatesDoNotAllocate()
    {
        var state = new TestState();
        var context = Tween.Sequence(
                Tween.Property<TestState, double>(s => s.X).To(10, TimeSpan.FromSeconds(10)).To(0, TimeSpan.FromSeconds(10)),
                Tween.Property<TestState, double>(s => s.X).Path([5.0, 2.0, 8.0], TimeSpan.FromSeconds(10), TweenPathType.CatmullRom))
            .Loops(Tween.Infinite, TweenLoopType.Yoyo)
            .Build()
            .CreateManualContext(state);

        context.Start();
        AllocationAssert.SteadyStateDoesNotAllocate(context);
    }
#endif

    private class TestState
    {
        public double X { get; set; }
    }
}
