using Sight.Tweening.Internal.Animations;
using Sight.Tweening.Internal.Properties;

namespace Sight.Tweening.Tests;

public class TweenAnimationTests
{
    [Test]
    public void Test_AnimateDouble()
    {
        var property = new TweenReflectionProperty(typeof(TestState).GetProperty(nameof(TestState.X))!);
        var animation = new TweenPropertyAnimation(TimeSpan.FromSeconds(3), property, TweenTransition.To(24.0));
        var state = new TestState { X = 12 };
        var context = new TweenStateContext(state);
        animation.SetUp(context);

        var expected = new[] { 12.0, 16.0, 20.0, 24.0 };
        for (var i = 0; i < expected.Length; i++)
        {
            context.Time = TimeSpan.FromSeconds(i);
            animation.Update(context);
            Assert.That(state.X, Is.EqualTo(expected[i]).Within(0.0001));
        }
    }

    [Test]
    public void Test_AnimateSequence()
    {
        var property = new TweenReflectionProperty(typeof(TestState).GetProperty(nameof(TestState.X))!);
        var animation1 = new TweenPropertyAnimation(TimeSpan.FromSeconds(3), property, TweenTransition.To(24.0));
        var animation2 = new TweenPropertyAnimation(TimeSpan.FromSeconds(3), property, TweenTransition.To(12.0));
        var sequenceAnimation = new TweenSequenceAnimation();
        sequenceAnimation.Add(animation1);
        sequenceAnimation.Add(animation2);

        var state = new TestState { X = 12 };
        var context = new TweenStateContext(state);
        sequenceAnimation.SetUp(context);

        var expected = new[] { 12.0, 16.0, 20.0, 24.0, 20.0, 16.0, 12.0 };
        for (var i = 0; i < expected.Length; i++)
        {
            context.Time = TimeSpan.FromSeconds(i);
            sequenceAnimation.Update(context);
            Assert.That(state.X, Is.EqualTo(expected[i]).Within(0.0001));
        }
    }

    [Test]
    public void Test_AnimateParallel()
    {
        var property1 = new TweenReflectionProperty(typeof(TestState).GetProperty(nameof(TestState.X))!);
        var property2 = new TweenReflectionProperty(typeof(TestState).GetProperty(nameof(TestState.Y))!);
        var animation1 = new TweenPropertyAnimation(TimeSpan.FromSeconds(3), property1, TweenTransition.To(24.0));
        var animation2 = new TweenPropertyAnimation(TimeSpan.FromSeconds(3), property2, TweenTransition.To(12.0));
        var parallelAnimation = new TweenParallelAnimation();
        parallelAnimation.Add(animation1);
        parallelAnimation.Add(animation2);

        var state = new TestState { X = 12, Y = 24 };
        var context = new TweenStateContext(state);
        parallelAnimation.SetUp(context);

        var expected1 = new[] { 12.0, 16.0, 20.0, 24.0 };
        var expected2 = new[] { 24.0, 20.0, 16.0, 12.0 };
        for (var i = 0; i < expected1.Length; i++)
        {
            context.Time = TimeSpan.FromSeconds(i);
            parallelAnimation.Update(context);
            Assert.That(state.X, Is.EqualTo(expected1[i]).Within(0.0001));
            Assert.That(state.Y, Is.EqualTo(expected2[i]).Within(0.0001));
        }
    }

    private class TestState
    {
        public double X { get; set; }

        public double Y { get; set; }
    }
}
