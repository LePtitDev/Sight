namespace Sight.Tweening.Tests;

public class TweenRunnerTests
{
    private static ITweenAnimation Animation(double to, double seconds) =>
        Tween.Property<TestState, double>(s => s.X).To(to, TimeSpan.FromSeconds(seconds)).Build();

    [Test]
    public void Test_TickAndAutoRemove()
    {
        var runner = new TweenRunner();
        var a = new TestState();
        var b = new TestState();
        runner.Play(Animation(10, 1), a);
        var contextB = runner.Play(Animation(10, 2), b);

        runner.Tick(0.5);
        Assert.Multiple(() =>
        {
            Assert.That(a.X, Is.EqualTo(5).Within(1e-9));
            Assert.That(b.X, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(runner.Count, Is.EqualTo(2));
        });

        runner.Tick(0.5);
        Assert.That(runner.Count, Is.EqualTo(1));

        runner.Tick(1);
        Assert.Multiple(() =>
        {
            Assert.That(runner.Count, Is.EqualTo(0));
            Assert.That(contextB.Completion.IsCompletedSuccessfully, Is.True);
        });
    }

    [Test]
    public void Test_Kill()
    {
        var runner = new TweenRunner();
        var a = new TestState();
        var b = new TestState();
        runner.Play(Animation(10, 1), a, id: "first");
        runner.Play(Animation(10, 1), b, id: "second");
        runner.Play(Tween.Property<TestState, double>(s => s.Y).To(10, TimeSpan.FromSeconds(1)), a);

        Assert.That(runner.Kill(a, complete: true), Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(a.X, Is.EqualTo(10));
            Assert.That(a.Y, Is.EqualTo(10));
            Assert.That(runner.Count, Is.EqualTo(1));
        });

        Assert.That(runner.KillById("second"), Is.EqualTo(1));
        runner.Tick(0.5);
        Assert.Multiple(() =>
        {
            Assert.That(b.X, Is.EqualTo(0));
            Assert.That(runner.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void Test_PauseResume()
    {
        var runner = new TweenRunner();
        var a = new TestState();
        runner.Play(Animation(10, 1), a);

        runner.PauseAll();
        runner.Tick(0.5);
        Assert.Multiple(() =>
        {
            Assert.That(a.X, Is.EqualTo(0));
            Assert.That(runner.Count, Is.EqualTo(1));
        });

        runner.ResumeAll();
        runner.Tick(0.5);
        Assert.That(a.X, Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void Test_KillDuringTick()
    {
        var runner = new TweenRunner();
        var a = new TestState();
        var b = new TestState();
        runner.Play(Tween.Property<TestState, double>(s => s.X).To(10, TimeSpan.FromSeconds(1)).OnUpdate(() => runner.Kill(b)), a);
        runner.Play(Animation(10, 1), b);

        runner.Tick(0.5);

        Assert.Multiple(() =>
        {
            Assert.That(b.X, Is.EqualTo(0));
            Assert.That(runner.Count, Is.EqualTo(1));
        });
    }

    private class TestState
    {
        public double X { get; set; }

        public double Y { get; set; }
    }
}
