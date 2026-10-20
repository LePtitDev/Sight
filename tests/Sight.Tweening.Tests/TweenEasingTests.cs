namespace Sight.Tweening.Tests;

public class TweenEasingTests
{
    private static IEnumerable<TweenEase> AllEases => Enum.GetValues(typeof(TweenEase)).Cast<TweenEase>();

    [TestCaseSource(nameof(AllEases))]
    public void Test_EndpointsOfPredefinedEases(TweenEase ease)
    {
        var func = TweenEasing.Get(ease);
        Assert.Multiple(() =>
        {
            Assert.That(func(0), Is.EqualTo(0).Within(1e-9));
            Assert.That(func(1), Is.EqualTo(1).Within(1e-9));
        });
    }

    [Test]
    public void Test_Combinators()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TweenEasing.Out(TweenEasing.InCubic)(0.3), Is.EqualTo(TweenEasing.OutCubic(0.3)).Within(1e-12));
            Assert.That(TweenEasing.InOut(TweenEasing.InCubic)(0.3), Is.EqualTo(TweenEasing.InOutCubic(0.3)).Within(1e-12));
            Assert.That(TweenEasing.InOut(TweenEasing.InCubic)(0.8), Is.EqualTo(TweenEasing.InOutCubic(0.8)).Within(1e-12));
            Assert.That(TweenEasing.OutIn(TweenEasing.InQuad)(0.25), Is.EqualTo(TweenEasing.OutQuad(0.5) / 2).Within(1e-12));
            Assert.That(TweenEasing.OutIn(TweenEasing.InQuad)(0.5), Is.EqualTo(0.5).Within(1e-12));
        });
    }

    [Test]
    public void Test_Factories()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TweenEasing.Power(2)(0.5), Is.EqualTo(0.25).Within(1e-12));
            Assert.That(TweenEasing.Back()(0.4), Is.EqualTo(TweenEasing.InBack(0.4)).Within(1e-12));
            Assert.That(TweenEasing.Back(3)(0.2), Is.LessThan(TweenEasing.InBack(0.2)));
            Assert.That(TweenEasing.Elastic()(0), Is.EqualTo(0));
            Assert.That(TweenEasing.Elastic()(1), Is.EqualTo(1));
            Assert.That(TweenEasing.Out(TweenEasing.Elastic(1.5, 0.4))(0.2), Is.GreaterThan(1));
        });
    }

    [Test]
    public void Test_CubicBezier()
    {
        var linear = TweenEasing.CubicBezier(0, 0, 1, 1);
        var ease = TweenEasing.CubicBezier(0.25, 0.1, 0.25, 1);
        Assert.Multiple(() =>
        {
            Assert.That(linear(0.3), Is.EqualTo(0.3).Within(1e-6));
            Assert.That(ease(0), Is.EqualTo(0));
            Assert.That(ease(1), Is.EqualTo(1));
            Assert.That(ease(0.5), Is.EqualTo(0.8024).Within(0.001));
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => TweenEasing.CubicBezier(1.5, 0, 0, 1));
    }

    [TestCase(TweenStepPosition.End, 0.3, 0.25)]
    [TestCase(TweenStepPosition.End, 0.99, 0.75)]
    [TestCase(TweenStepPosition.End, 1.0, 1.0)]
    [TestCase(TweenStepPosition.Start, 0.0, 0.25)]
    [TestCase(TweenStepPosition.Start, 0.3, 0.5)]
    [TestCase(TweenStepPosition.Both, 0.0, 0.2)]
    [TestCase(TweenStepPosition.Both, 1.0, 1.0)]
    [TestCase(TweenStepPosition.None, 0.0, 0.0)]
    [TestCase(TweenStepPosition.None, 0.5, 2.0 / 3)]
    public void Test_Steps(TweenStepPosition position, double t, double expected)
    {
        Assert.That(TweenEasing.Steps(4, position)(t), Is.EqualTo(expected).Within(1e-12));
    }

    [Test]
    public void Test_Spring()
    {
        var bouncy = TweenEasing.Spring(stiffness: 200, damping: 5);
        var overdamped = TweenEasing.Spring(stiffness: 100, damping: 40);
        var samples = Enumerable.Range(0, 101).Select(i => bouncy(i / 100.0)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(bouncy(0), Is.EqualTo(0));
            Assert.That(bouncy(1), Is.EqualTo(1));
            Assert.That(bouncy(0.99), Is.EqualTo(1).Within(0.002));
            Assert.That(samples.Max(), Is.GreaterThan(1));
            Assert.That(overdamped(0.99), Is.EqualTo(1).Within(0.002));
            Assert.That(Enumerable.Range(0, 101).Select(i => overdamped(i / 100.0)).Max(), Is.LessThanOrEqualTo(1));
        });
    }
}
