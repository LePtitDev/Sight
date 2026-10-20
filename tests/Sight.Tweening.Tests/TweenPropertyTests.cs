using Sight.Tweening.Internal.Animations;
using Sight.Tweening.Internal.Properties;

namespace Sight.Tweening.Tests;

public class TweenPropertyTests
{
    [Test]
    public void Test_ExpressionProperty()
    {
        var property = new TweenExpressionProperty<TestState, double>(s => s.Sub().Value);
        var animation = new TweenPropertyAnimation(TimeSpan.FromSeconds(3), property, TweenTransition.To(24.0));
        var state = new TestState();
        state.Sub().Value = 12;
        var context = new TweenStateContext(state);
        animation.SetUp(context);

        var expected = new[] { 12.0, 16.0, 20.0, 24.0 };
        for (var i = 0; i < expected.Length; i++)
        {
            context.Time = TimeSpan.FromSeconds(i);
            animation.Update(context);
            Assert.That(state.Sub().Value, Is.EqualTo(expected[i]).Within(0.0001));
        }
    }

    private class TestState
    {
        private Lazy<TestState> _lazy = new(() => new TestState());

        public double Value { get; set; }

        public TestState Sub() => _lazy.Value;
    }
}
