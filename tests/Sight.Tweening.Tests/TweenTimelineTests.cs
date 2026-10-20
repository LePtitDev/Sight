namespace Sight.Tweening.Tests;

public class TweenTimelineTests
{
    private static Tween.PropertyBuilder<double> X() => Tween.Property<TestState, double>(s => s.X);

    private static Tween.PropertyBuilder<double> Y() => Tween.Property<TestState, double>(s => s.Y);

    [Test]
    public void Test_Delay()
    {
        var state = new TestState { X = 1 };
        var context = X().To(11, TimeSpan.FromSeconds(10)).Delay(TimeSpan.FromSeconds(2)).Build().CreateManualContext(state);
        context.Start();

        context.Tick(1);
        Assert.That(state.X, Is.EqualTo(1));

        context.Tick(6);
        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(6).Within(1e-9));
            Assert.That(context.Duration, Is.EqualTo(TimeSpan.FromSeconds(12)));
        });
    }

    [Test]
    public void Test_DelayCapturesValueWhenStarting()
    {
        var state = new TestState { X = 0 };
        var context = Tween.Parallel(
                X().To(10, TimeSpan.FromSeconds(2)),
                X().By(5, TimeSpan.FromSeconds(1)).Delay(TimeSpan.FromSeconds(2)))
            .Build()
            .CreateManualContext(state);

        context.Start();
        context.Tick(3);

        Assert.That(state.X, Is.EqualTo(15).Within(1e-9));
    }

    [Test]
    public void Test_LoopsRestart()
    {
        var state = new TestState();
        var iterations = new List<int>();
        var context = X().From(0).To(10, TimeSpan.FromSeconds(10))
            .Loops(3)
            .OnLoop(iterations.Add)
            .Build()
            .CreateManualContext(state);

        context.Start();
        context.Tick(12);
        Assert.That(state.X, Is.EqualTo(2).Within(1e-9));

        context.Tick(10);
        Assert.That(state.X, Is.EqualTo(2).Within(1e-9));

        context.Tick(10);
        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(10).Within(1e-9));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Completed));
            Assert.That(iterations, Is.EqualTo(new[] { 1, 2 }));
        });
    }

    [Test]
    public void Test_LoopsYoyo()
    {
        var state = new TestState();
        var context = X().To(10, TimeSpan.FromSeconds(10)).Loops(2, TweenLoopType.Yoyo).Build().CreateManualContext(state);

        context.Start();
        context.Tick(7);
        Assert.That(state.X, Is.EqualTo(7).Within(1e-9));

        context.Tick(6);
        Assert.That(state.X, Is.EqualTo(7).Within(1e-9));

        context.Tick(10);
        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(0).Within(1e-9));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Completed));
        });
    }

    [Test]
    public void Test_LoopRendersIterationEnd()
    {
        var state = new TestState();
        var completed = 0;
        var context = Tween.Sequence(X().To(10, TimeSpan.FromSeconds(10)).OnComplete(() => completed++))
            .Loops(3)
            .Build()
            .CreateManualContext(state);

        context.Start();
        context.Tick(9);
        context.Tick(2);

        Assert.That(completed, Is.EqualTo(1), "The end of the first iteration is rendered even if the tick jumps over it");
    }

    [Test]
    public void Test_Hooks()
    {
        var events = new List<string>();
        var context = X().To(10, TimeSpan.FromSeconds(2))
            .Delay(TimeSpan.FromSeconds(1))
            .OnStart(() => events.Add("start"))
            .OnUpdate(() => events.Add("update"))
            .OnComplete(() => events.Add("complete"))
            .Build()
            .CreateManualContext(new TestState());

        context.Start();
        Assert.That(events, Is.Empty, "Callbacks wait for the delay");

        context.Tick(2);
        context.Tick(2);
        context.Tick(2);

        Assert.That(events, Is.EqualTo(new[] { "start", "update", "update", "complete" }));
    }

    [Test]
    public void Test_CallbackCrossing()
    {
        var calls = 0;
        var context = Tween.Timeline()
            .Append(X().To(10, TimeSpan.FromSeconds(2)))
            .AppendCallback(() => calls++)
            .Append(X().To(0, TimeSpan.FromSeconds(2)))
            .Build()
            .CreateManualContext(new TestState());

        context.Seek(TimeSpan.FromSeconds(1));
        Assert.That(calls, Is.EqualTo(0));

        context.Seek(TimeSpan.FromSeconds(3));
        Assert.That(calls, Is.EqualTo(1));

        context.Seek(TimeSpan.FromSeconds(3.5));
        Assert.That(calls, Is.EqualTo(1));

        context.Seek(TimeSpan.FromSeconds(1));
        Assert.That(calls, Is.EqualTo(2));
    }

    [Test]
    public void Test_TimelinePlacement()
    {
        var builder = Tween.Timeline()
            .Append(X().To(10, TimeSpan.FromSeconds(4)))
            .Join(Y().To(10, TimeSpan.FromSeconds(2)))
            .Append(Y().To(0, TimeSpan.FromSeconds(2)), TimeSpan.FromSeconds(-1))
            .AppendInterval(TimeSpan.FromSeconds(1))
            .Insert(TimeSpan.FromSeconds(10), X().To(0, TimeSpan.FromSeconds(2)));

        Assert.That(builder.Duration, Is.EqualTo(TimeSpan.FromSeconds(12)));

        var state = new TestState();
        var context = builder.Build().CreateManualContext(state);
        context.Seek(TimeSpan.FromSeconds(1));
        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(state.Y, Is.EqualTo(5).Within(1e-9));
        });

        context.Seek(TimeSpan.FromSeconds(4));
        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(10).Within(1e-9));
            Assert.That(state.Y, Is.EqualTo(5).Within(1e-9), "Second Y tween starts at 3s (4s - 1s overlap)");
        });

        context.Seek(TimeSpan.FromSeconds(11));
        Assert.That(state.X, Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void Test_SeekBackwardRestoresValues()
    {
        var state = new TestState { X = 0 };
        var context = Tween.Sequence(
                X().To(10, TimeSpan.FromSeconds(1)),
                X().By(10, TimeSpan.FromSeconds(1)),
                X().By(10, TimeSpan.FromSeconds(1)))
            .Build()
            .CreateManualContext(state);

        context.Seek(TimeSpan.FromSeconds(3));
        Assert.That(state.X, Is.EqualTo(30).Within(1e-9));

        context.Seek(TimeSpan.FromSeconds(0.5));
        Assert.That(state.X, Is.EqualTo(5).Within(1e-9));

        context.Seek(TimeSpan.FromSeconds(2.5));
        Assert.That(state.X, Is.EqualTo(25).Within(1e-9), "Relative tweens keep the values captured on their first start");
    }

    [Test]
    public void Test_StaggerTargets()
    {
        var states = Enumerable.Range(0, 3).Select(_ => new TestState()).ToArray();
        var context = Tween.Stagger(states, TimeSpan.FromSeconds(1), _ => X().To(10, TimeSpan.FromSeconds(2)))
            .Build()
            .CreateManualContext(null);

        Assert.That(context.Duration, Is.EqualTo(TimeSpan.FromSeconds(4)));

        context.Seek(TimeSpan.FromSeconds(2));
        Assert.That(states.Select(s => s.X), Is.EqualTo(new[] { 10.0, 5.0, 0.0 }).Within(1e-9));
    }

    [Test]
    public void Test_WithTarget()
    {
        var main = new TestState();
        var other = new TestState();
        var context = Tween.Parallel(
                X().To(10, TimeSpan.FromSeconds(1)),
                X().To(20, TimeSpan.FromSeconds(1)).WithTarget(other))
            .Build()
            .CreateManualContext(main);

        context.Start();
        context.Tick(1);

        Assert.Multiple(() =>
        {
            Assert.That(main.X, Is.EqualTo(10).Within(1e-9));
            Assert.That(other.X, Is.EqualTo(20).Within(1e-9));
        });
    }

    [Test]
    public void Test_InvalidArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => X().Loops(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => X().Delay(TimeSpan.FromSeconds(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => X().To(1, TimeSpan.FromSeconds(-1)));
            Assert.Throws<InvalidOperationException>(() => X().Build());
        });
    }

    private class TestState
    {
        public double X { get; set; }

        public double Y { get; set; }
    }
}
