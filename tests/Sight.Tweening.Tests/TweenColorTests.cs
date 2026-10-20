using System.Drawing;
using System.Numerics;

namespace Sight.Tweening.Tests;

public class TweenColorTests
{
    private static readonly Color s_red = Color.FromArgb(255, 255, 0, 0);
    private static readonly Color s_lime = Color.FromArgb(255, 0, 255, 0);
    private static readonly Color s_blue = Color.FromArgb(255, 0, 0, 255);
    private static readonly Color s_black = Color.FromArgb(255, 0, 0, 0);
    private static readonly Color s_white = Color.FromArgb(255, 255, 255, 255);

    private static Color Mix(TweenColorSpace space, Color begin, Color end, double delta = 0.5, TweenHuePath huePath = TweenHuePath.Shorter)
    {
        return TweenInterpolator.Color(space, huePath).Interpolate(delta, begin, end);
    }

    private static void AssertColor(Color actual, int a, int r, int g, int b)
    {
        Assert.That((actual.A, actual.R, actual.G, actual.B), Is.EqualTo((a, r, g, b)));
    }

    [TestCase(TweenColorSpace.Srgb, 128)]
    [TestCase(TweenColorSpace.LinearRgb, 188)]
    [TestCase(TweenColorSpace.Hsl, 128)]
    [TestCase(TweenColorSpace.Oklab, 99)]
    [TestCase(TweenColorSpace.Oklch, 99)]
    public void Test_BlackToWhite(TweenColorSpace space, int expected)
    {
        AssertColor(Mix(space, s_black, s_white), 255, expected, expected, expected);
    }

    [Test]
    public void Test_RedToLime()
    {
        Assert.Multiple(() =>
        {
            AssertColor(Mix(TweenColorSpace.Srgb, s_red, s_lime), 255, 128, 128, 0);
            AssertColor(Mix(TweenColorSpace.LinearRgb, s_red, s_lime), 255, 188, 188, 0);
            AssertColor(Mix(TweenColorSpace.Hsl, s_red, s_lime), 255, 255, 255, 0);
        });
    }

    [TestCase(TweenHuePath.Shorter, 255, 0, 255)]
    [TestCase(TweenHuePath.Longer, 0, 255, 0)]
    [TestCase(TweenHuePath.Increasing, 0, 255, 0)]
    [TestCase(TweenHuePath.Decreasing, 255, 0, 255)]
    public void Test_HuePaths(TweenHuePath huePath, int r, int g, int b)
    {
        AssertColor(Mix(TweenColorSpace.Hsl, s_red, s_blue, huePath: huePath), 255, r, g, b);
    }

    [Test]
    public void Test_AchromaticColorTakesOtherHue()
    {
        // Without the missing hue rule, white (hue 0) to lime (hue 120) would go through yellow
        AssertColor(Mix(TweenColorSpace.Hsl, s_white, s_lime), 255, 159, 223, 159);

        var oklch = Mix(TweenColorSpace.Oklch, s_white, s_lime);
        Assert.That(oklch.G, Is.GreaterThan(oklch.R).And.GreaterThan(oklch.B));
    }

    [TestCase(TweenColorSpace.Srgb)]
    [TestCase(TweenColorSpace.LinearRgb)]
    [TestCase(TweenColorSpace.Hsl)]
    [TestCase(TweenColorSpace.Oklab)]
    [TestCase(TweenColorSpace.Oklch)]
    public void Test_PremultipliedAlpha(TweenColorSpace space)
    {
        // Transparent is black with alpha 0: its color must not darken the transition
        AssertColor(Mix(space, Color.Transparent, s_white), 128, 255, 255, 255);
    }

    [TestCase(TweenColorSpace.Srgb)]
    [TestCase(TweenColorSpace.LinearRgb)]
    [TestCase(TweenColorSpace.Hsl)]
    [TestCase(TweenColorSpace.Oklab)]
    [TestCase(TweenColorSpace.Oklch)]
    public void Test_Endpoints(TweenColorSpace space)
    {
        var begin = Color.FromArgb(200, 12, 34, 56);
        var end = Color.FromArgb(100, 210, 180, 20);
        Assert.Multiple(() =>
        {
            Assert.That(Mix(space, begin, end, 0), Is.EqualTo(begin));
            Assert.That(Mix(space, begin, end, 1), Is.EqualTo(end));
            Assert.That(Mix(space, begin, end, 1e-9).ToArgb(), Is.EqualTo(begin.ToArgb()));
            Assert.That(Mix(space, begin, end, 1 - 1e-9).ToArgb(), Is.EqualTo(end.ToArgb()));
        });
    }

    [Test]
    public void Test_OklchKeepsSaturation()
    {
        // In sRGB, red to blue goes through a dark purple; Oklch keeps a vivid color
        var srgb = Mix(TweenColorSpace.Srgb, s_red, s_blue);
        var oklch = Mix(TweenColorSpace.Oklch, s_red, s_blue);

        Assert.Multiple(() =>
        {
            Assert.That(oklch.R, Is.GreaterThan(oklch.G));
            Assert.That(oklch.B, Is.GreaterThan(oklch.G));
            Assert.That(oklch.GetBrightness(), Is.GreaterThan(srgb.GetBrightness()));
        });
    }

    [TestCase(TweenColorSpace.Oklch, -0.3)]
    [TestCase(TweenColorSpace.Oklch, 1.3)]
    [TestCase(TweenColorSpace.Oklab, 1.3)]
    [TestCase(TweenColorSpace.Hsl, 1.3)]
    [TestCase(TweenColorSpace.LinearRgb, -0.3)]
    public void Test_OvershootStaysValid(TweenColorSpace space, double delta)
    {
        var interpolator = TweenInterpolator.Color<Vector4>(v => v, v => v, space);
        var value = interpolator.Interpolate(delta, new Vector4(1, 0.2f, 0, 1), new Vector4(0, 0.4f, 1, 0.5f));

        Assert.That(new[] { value.X, value.Y, value.Z, value.W }, Has.All.InRange(0f, 1f));
    }

    [Test]
    public void Test_GenericColorType()
    {
        var interpolator = TweenInterpolator.Color<Vector4>(v => v, v => v, TweenColorSpace.LinearRgb);
        var value = interpolator.Interpolate(0.5, new Vector4(0, 0, 0, 1), Vector4.One);

        Assert.That(value.X, Is.EqualTo(0.7354f).Within(0.001f));
    }

    [Test]
    public void Test_BuilderColorSpace()
    {
        var state = new TestState { Color = s_black };
        var context = Tween.Property<TestState, Color>(s => s.Color)
            .To(s_white, TimeSpan.FromSeconds(1))
            .InColorSpace(TweenColorSpace.Oklab)
            .Build()
            .CreateManualContext(state);

        context.Start();
        context.Tick(0.5);

        AssertColor(state.Color, 255, 99, 99, 99);
    }

#if NET5_0_OR_GREATER
    [Test]
    public void Test_UpdatesDoNotAllocate()
    {
        var context = Tween.Property<TestState, Color>(s => s.Color)
            .To(s_blue, TimeSpan.FromSeconds(1))
            .InColorSpace(TweenColorSpace.Oklch, TweenHuePath.Longer)
            .Loops(Tween.Infinite, TweenLoopType.Yoyo)
            .Build()
            .CreateManualContext(new TestState { Color = s_red });

        context.Start();
        AllocationAssert.SteadyStateDoesNotAllocate(context);
    }
#endif

    [Test]
    public void Test_DefaultIsSrgb()
    {
        AssertColor(TweenInterpolator<Color>.Default.Interpolate(0.5, s_black, s_white), 255, 128, 128, 128);
    }

    [Test]
    public void Test_InvalidArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TweenInterpolator.Color((TweenColorSpace)42));
            Assert.Throws<ArgumentOutOfRangeException>(() => TweenInterpolator.Color(TweenColorSpace.Hsl, (TweenHuePath)42));
            Assert.Throws<ArgumentNullException>(() => TweenInterpolator.Color<Vector4>(null!, v => v));
        });
    }

    private class TestState
    {
        public Color Color { get; set; }
    }
}
