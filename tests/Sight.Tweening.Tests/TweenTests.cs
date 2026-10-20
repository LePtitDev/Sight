namespace Sight.Tweening.Tests;

public class TweenTests
{
    [Test]
    public void Test_01()
    {
        var state = new TestState { X = 8 };
        var context = Tween.Sequence(
            Tween.Property<TestState, double>(s => s.X)
                .To(24, TimeSpan.FromSeconds(3))
                .To(12, TimeSpan.FromSeconds(3)))
            .Build()
            .CreateManualContext(state);

        context.Start();

        context.Tick(TimeSpan.FromSeconds(3));

        Assert.That(state.X, Is.EqualTo(24).Within(0.01));

        context.Tick(TimeSpan.FromSeconds(3));

        Assert.That(state.X, Is.EqualTo(12).Within(0.01));
    }

    [Test]
    [Ignore("This test is only made for manual test")]
    public async Task Test_02()
    {
        var state = new TestState { X = 8 };
        var context = Tween.Sequence(
            Tween.Property<TestState, double>(s => s.X)
                .To(24, TimeSpan.FromSeconds(1))
                .To(12, TimeSpan.FromSeconds(1)))
            .Build()
            .CreateTimerContext(state, 100);

        context.Start();

        await Task.Delay(1000);

        Assert.That(state.X, Is.EqualTo(24).Within(0.5));

        await Task.Delay(1000);

        Assert.That(state.X, Is.EqualTo(12).Within(0.5));
    }

    [Test]
    public void Test_03()
    {
        var state = new TestState { X = 8 };
        var context = Tween.Sequence(
            Tween.Property<TestState, string>(s => s.Str)
                .To("my text.", TimeSpan.FromSeconds(3))
                .To("", TimeSpan.FromSeconds(3)))
            .Build()
            .CreateManualContext(state);

        context.Start();

        context.Tick(TimeSpan.FromSeconds(3));

        Assert.That(state.Str, Is.EqualTo("my text."));

        context.Tick(TimeSpan.FromSeconds(3));

        Assert.That(state.Str, Is.EqualTo(""));
    }

    [Test]
    public void Test_Float()
    {
        var state = new TestState();
        var context = Tween.Property<TestState, float>(s => s.F).To(10f, TimeSpan.FromSeconds(2)).Build().CreateManualContext(state);

        context.Start();
        context.Tick(1);

        Assert.That(state.F, Is.EqualTo(5f).Within(0.0001f));
    }

    [Test]
    public void Test_UntypedBuilder()
    {
        var state = new TestState { X = 3 };
        var context = Tween.Property(typeof(TestState), nameof(TestState.X))
            .From(0.0)
            .To(10.0, TimeSpan.FromSeconds(2))
            .To(20.0, TimeSpan.FromSeconds(2), TweenEase.InQuad)
            .Build()
            .CreateManualContext(state);

        context.Start();
        Assert.That(state.X, Is.EqualTo(0).Within(0.0001));

        context.Tick(1);
        Assert.That(state.X, Is.EqualTo(5).Within(0.0001));

        context.Tick(2);
        Assert.That(state.X, Is.EqualTo(12.5).Within(0.0001));
    }

    [Test]
    public void Test_DelegateProperty()
    {
        var state = new TestState { X = 0 };
        var context = Tween.Property<TestState, double>(s => s.X, (s, v) => s.X = v)
            .To(10, TimeSpan.FromSeconds(1))
            .Build()
            .CreateManualContext(state);

        context.Start();
        context.Tick(0.5);

        Assert.That(state.X, Is.EqualTo(5).Within(0.0001));
    }

    [Test]
    public void Test_ClosureProperty()
    {
        var value = 2.0;
        var context = Tween.Property(() => value, v => value = v)
            .To(4, TimeSpan.FromSeconds(1))
            .Build()
            .CreateManualContext(null);

        context.Start();
        context.Tick(0.5);

        Assert.That(value, Is.EqualTo(3).Within(0.0001));
    }

    [Test]
    public void Test_NullReferenceValue()
    {
        var state = new TestState { Str = "abcd" };
        var context = Tween.Property<TestState, string?>(s => s.NullableStr, (s, v) => s.NullableStr = v)
            .From("abcd")
            .To(null, TimeSpan.FromSeconds(1))
            .Build()
            .CreateManualContext(state);

        context.Start();
        context.Tick(1);

        Assert.That(state.NullableStr, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Test_EasingOvershootIsNotClamped()
    {
        var state = new TestState();
        var context = Tween.Property<TestState, double>(s => s.X)
            .To(100, TimeSpan.FromSeconds(1), TweenEase.OutBack)
            .Build()
            .CreateManualContext(state);

        context.Start();
        context.Tick(0.7);

        Assert.That(state.X, Is.GreaterThan(100));
    }

    [Test]
    public void Test_ZeroDuration()
    {
        var state = new TestState { X = 1 };
        var context = Tween.Property<TestState, double>(s => s.X).To(5, TimeSpan.Zero).Build().CreateManualContext(state);

        context.Start();

        Assert.Multiple(() =>
        {
            Assert.That(state.X, Is.EqualTo(5));
            Assert.That(context.State, Is.EqualTo(TweenAnimationState.Completed));
        });
    }

    [Test]
    public void Test_BuilderDuration()
    {
        var builder = Tween.Sequence(
                Tween.Property<TestState, double>(s => s.X).To(1, TimeSpan.FromSeconds(1)).To(2, TimeSpan.FromSeconds(2)),
                Tween.Parallel(
                    Tween.Property<TestState, double>(s => s.Y).To(1, TimeSpan.FromSeconds(1)),
                    Tween.Delay(TimeSpan.FromSeconds(4))))
            .Loops(2)
            .Delay(TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(builder.Duration, Is.EqualTo(TimeSpan.FromSeconds(15)));
            Assert.That(builder.Build().Duration, Is.EqualTo(TimeSpan.FromSeconds(15)));
        });
    }

    private class TestState
    {
        public double X { get; set; }

        public double Y { get; set; }

        public float F { get; set; }

        public string Str { get; set; } = "";

        public string? NullableStr { get; set; }
    }
}
