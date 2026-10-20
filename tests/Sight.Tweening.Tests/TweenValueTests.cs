using System.Numerics;

namespace Sight.Tweening.Tests;

public class TweenValueTests
{
    [Test]
    public void Test_By()
    {
        var state = new TestState { X = 5 };
        var context = Tween.Property<TestState, double>(s => s.X)
            .By(10, TimeSpan.FromSeconds(1))
            .By(-5, TimeSpan.FromSeconds(1))
            .Build()
            .CreateManualContext(state);

        context.Seek(TimeSpan.FromSeconds(1));
        Assert.That(state.X, Is.EqualTo(15).Within(1e-9));

        context.Seek(TimeSpan.FromSeconds(2));
        Assert.That(state.X, Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void Test_FromToCurrentValue()
    {
        var state = new TestState { X = 5 };
        var context = Tween.Property<TestState, double>(s => s.X)
            .From(25, TimeSpan.FromSeconds(2))
            .Build()
            .CreateManualContext(state);

        context.Start();
        Assert.That(state.X, Is.EqualTo(25).Within(1e-9), "The start value is applied immediately");

        context.Tick(1);
        Assert.That(state.X, Is.EqualTo(15).Within(1e-9));

        context.Tick(1);
        Assert.That(state.X, Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void Test_LinearPath()
    {
        var state = new TestState { V = Vector2.Zero };
        var context = Tween.Property<TestState, Vector2>(s => s.V)
            .Path([new Vector2(10, 0), new Vector2(10, 10)], TimeSpan.FromSeconds(2))
            .Build()
            .CreateManualContext(state);

        context.Seek(TimeSpan.FromSeconds(0.5));
        Assert.That(state.V, Is.EqualTo(new Vector2(5, 0)));

        context.Seek(TimeSpan.FromSeconds(1.5));
        Assert.That(state.V, Is.EqualTo(new Vector2(10, 5)));
    }

    [Test]
    public void Test_BezierPath()
    {
        var state = new TestState { X = 0 };
        var context = Tween.Property<TestState, double>(s => s.X)
            .Path([10.0, 0.0], TimeSpan.FromSeconds(1), TweenPathType.Bezier)
            .Build()
            .CreateManualContext(state);

        context.Seek(TimeSpan.FromSeconds(0.5));
        Assert.That(state.X, Is.EqualTo(5).Within(1e-9), "Quadratic Bézier (0, 10, 0) at t=0.5");

        context.Seek(TimeSpan.FromSeconds(1));
        Assert.That(state.X, Is.EqualTo(0).Within(1e-9));
    }

    [Test]
    public void Test_CatmullRomPath()
    {
        var state = new TestState { X = 0 };
        var context = Tween.Property<TestState, double>(s => s.X)
            .Path([10.0, 20.0, 30.0], TimeSpan.FromSeconds(3), TweenPathType.CatmullRom)
            .Build()
            .CreateManualContext(state);

        foreach (var (time, expected) in new[] { (1.0, 10.0), (1.5, 15.0), (2.0, 20.0), (3.0, 30.0) })
        {
            context.Seek(TimeSpan.FromSeconds(time));
            Assert.That(state.X, Is.EqualTo(expected).Within(1e-9), $"t={time}");
        }
    }

    [Test]
    public void Test_Shake()
    {
        var state = new TestState { V = new Vector2(100, 100) };
        var strength = new Vector2(5, 2);
        var context = Tween.Property<TestState, Vector2>(s => s.V)
            .Shake(strength, TimeSpan.FromSeconds(1), vibrato: 20, seed: 42)
            .Build()
            .CreateManualContext(state);

        var moved = false;
        context.Start();
        for (var i = 0; i < 20; i++)
        {
            context.Tick(0.05);
            var offset = state.V - new Vector2(100, 100);
            moved |= offset != Vector2.Zero;
            Assert.That(Math.Abs(offset.X) <= strength.X + 1e-4 && Math.Abs(offset.Y) <= strength.Y + 1e-4, Is.True, $"Offset {offset} exceeds the strength");
        }

        Assert.Multiple(() =>
        {
            Assert.That(moved, Is.True);
            Assert.That(state.V, Is.EqualTo(new Vector2(100, 100)));
        });
    }

    [Test]
    public void Test_ShakeIsReproducibleWithSeed()
    {
        double Sample()
        {
            var state = new TestState();
            var context = Tween.Property<TestState, double>(s => s.X).Shake(10, TimeSpan.FromSeconds(1), seed: 7).Build().CreateManualContext(state);
            context.Seek(TimeSpan.FromSeconds(0.33));
            return state.X;
        }

        var first = Sample();
        Assert.That(Sample(), Is.EqualTo(first));
    }

    [Test]
    public void Test_Punch()
    {
        var state = new TestState { X = 10 };
        var context = Tween.Property<TestState, double>(s => s.X)
            .Punch(5, TimeSpan.FromSeconds(1), vibrato: 4, elasticity: 0.5)
            .Build()
            .CreateManualContext(state);

        var samples = new List<double>();
        context.Start();
        for (var i = 0; i < 100; i++)
        {
            context.Tick(0.01);
            samples.Add(state.X);
        }

        Assert.Multiple(() =>
        {
            Assert.That(samples.Max(), Is.GreaterThan(12).And.LessThanOrEqualTo(15));
            Assert.That(samples.Min(), Is.LessThan(10).And.GreaterThanOrEqualTo(7.5));
            Assert.That(state.X, Is.EqualTo(10));
        });
    }

    [Test]
    public void Test_ArithmeticRequired()
    {
        Assert.Throws<NotSupportedException>(() => Tween.Property<TestState, string>(s => s.Text).By("a", TimeSpan.FromSeconds(1)).Build());
    }

    [Test]
    public void Test_CustomInterpolator()
    {
        var state = new TestState();
        var context = Tween.Property<TestState, double>(s => s.X)
            .WithInterpolator(TweenInterpolator.Create<double>((d, a, b) => d < 1 ? a : b))
            .To(10, TimeSpan.FromSeconds(1))
            .Build()
            .CreateManualContext(state);

        context.Seek(TimeSpan.FromSeconds(0.9));
        Assert.That(state.X, Is.EqualTo(0));
    }

    private class TestState
    {
        public double X { get; set; }

        public Vector2 V { get; set; }

        public string Text { get; set; } = "";
    }
}
