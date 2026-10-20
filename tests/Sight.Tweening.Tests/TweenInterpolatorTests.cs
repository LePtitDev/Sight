using System.Drawing;
using System.Numerics;

namespace Sight.Tweening.Tests;

public class TweenInterpolatorTests
{
    [TestCase(0.5, 10, 0, 20)]
    [TestCase(0.5, 5, 0, 10)]
    [TestCase(0.95, 10, 0, 10)]
    [TestCase(1.5, 15, 0, 10)]
    public void Test_InterpolateInt32(double delta, int expected, int begin, int end)
    {
        var value = TweenInterpolator<int>.Default.Interpolate(delta, begin, end);
        Assert.That(value, Is.EqualTo(expected));
    }

    [TestCase(2.0, (byte)255, (byte)0, (byte)200)]
    [TestCase(-1.0, (byte)0, (byte)0, (byte)200)]
    public void Test_InterpolateByteSaturates(double delta, byte expected, byte begin, byte end)
    {
        var value = TweenInterpolator<byte>.Default.Interpolate(delta, begin, end);
        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    public void Test_InterpolateInt64KeepsEndpoints()
    {
        var interpolator = TweenInterpolator<long>.Default;
        Assert.Multiple(() =>
        {
            Assert.That(interpolator.Interpolate(0, long.MinValue, long.MaxValue), Is.EqualTo(long.MinValue));
            Assert.That(interpolator.Interpolate(1, long.MinValue, long.MaxValue), Is.EqualTo(long.MaxValue));
        });
    }

    [TestCase(0.5, 0.5, 0, 1)]
    [TestCase(0.5, 1, 0, 2)]
    public void Test_InterpolateDouble(double delta, double expected, double begin, double end)
    {
        var value = TweenInterpolator<double>.Default.Interpolate(delta, begin, end);
        Assert.That(value, Is.EqualTo(expected).Within(0.00001));
    }

    [Test]
    public void Test_InterpolateSingleAndDecimal()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TweenInterpolator<float>.Default.Interpolate(0.25, 0f, 8f), Is.EqualTo(2f));
            Assert.That(TweenInterpolator<decimal>.Default.Interpolate(0.25, 0m, 8m), Is.EqualTo(2m));
        });
    }

    [TestCase(0.5, "my t", "", "my text.")]
    [TestCase(0.5, "my t", "my text.", "")]
    public void Test_InterpolateString(double delta, string expected, string begin, string end)
    {
        var value = TweenInterpolator<string>.Default.Interpolate(delta, begin, end);
        Assert.That(value, Is.EqualTo(expected));
    }

    [TestCase(0.2, true)]
    [TestCase(0.8, false)]
    public void Test_InterpolateBoolean(double delta, bool expected)
    {
        var value = TweenInterpolator<bool>.Default.Interpolate(delta, true, false);
        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    public void Test_InterpolateNullable()
    {
        var interpolator = TweenInterpolator<double?>.Default;
        Assert.Multiple(() =>
        {
            Assert.That(interpolator.Interpolate(0.5, 0, 10), Is.EqualTo(5));
            Assert.That(interpolator.Interpolate(0.4, null, 10), Is.Null);
            Assert.That(interpolator.Interpolate(0.6, null, 10), Is.EqualTo(10));
        });
    }

    [Test]
    public void Test_InterpolateTime()
    {
        var begin = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var offset = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));
        Assert.Multiple(() =>
        {
            Assert.That(TweenInterpolator<TimeSpan>.Default.Interpolate(0.5, TimeSpan.Zero, TimeSpan.FromHours(2)), Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(TweenInterpolator<DateTime>.Default.Interpolate(0.5, begin, begin.AddDays(2)), Is.EqualTo(begin.AddDays(1)));
            Assert.That(TweenInterpolator<DateTimeOffset>.Default.Interpolate(0.5, offset, offset.AddHours(4)), Is.EqualTo(offset.AddHours(2)));
        });
    }

    [Test]
    public void Test_InterpolateVectors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TweenInterpolator<Vector2>.Default.Interpolate(0.5, Vector2.Zero, new Vector2(2, 4)), Is.EqualTo(new Vector2(1, 2)));
            Assert.That(TweenInterpolator<Vector3>.Default.Interpolate(0.5, Vector3.Zero, new Vector3(2, 4, 6)), Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(TweenInterpolator<Vector4>.Default.Interpolate(0.5, Vector4.Zero, new Vector4(2)), Is.EqualTo(new Vector4(1)));
        });
    }

    [Test]
    public void Test_InterpolateQuaternion()
    {
        var end = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        var value = TweenInterpolator<Quaternion>.Default.Interpolate(0.5, Quaternion.Identity, end);
        var expected = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4);

        Assert.Multiple(() =>
        {
            Assert.That(value.Z, Is.EqualTo(expected.Z).Within(0.0001));
            Assert.That(value.W, Is.EqualTo(expected.W).Within(0.0001));
        });
    }

    [Test]
    public void Test_InterpolateColor()
    {
        var value = TweenInterpolator<Color>.Default.Interpolate(0.5, Color.FromArgb(255, 0, 0, 0), Color.FromArgb(255, 200, 100, 50));
        Assert.That(value.ToArgb(), Is.EqualTo(Color.FromArgb(255, 100, 50, 25).ToArgb()));
    }

#if NET7_0_OR_GREATER
    [Test]
    public void Test_InterpolateGenericNumbers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TweenInterpolator<Int128>.Default.Interpolate(0.5, 0, 9), Is.EqualTo((Int128)5));
            Assert.That(TweenInterpolator.Number<Half>().Interpolate(0.5, (Half)0, (Half)2), Is.EqualTo((Half)1));
        });
    }
#endif

    [Test]
    public void Test_UntypedDefault()
    {
        var value = TweenInterpolator.GetDefault(typeof(int)).Interpolate(0.5, 0, 10);
        Assert.That(value, Is.EqualTo(5));
    }

    [Test]
    public void Test_InterpolableType()
    {
        var value = TweenInterpolator<Level>.Default.Interpolate(0.5, new Level(0), new Level(10));
        Assert.Multiple(() =>
        {
            Assert.That(value!.Value, Is.EqualTo(5));
            Assert.That(TweenInterpolator.GetDefault(typeof(Level)), Is.SameAs(TweenInterpolator<Level>.Default));
        });
    }

    [Test]
    public void Test_RegisterCustomInterpolator()
    {
        TweenInterpolator.Register(TweenInterpolator.Create<Size>((d, a, b) => new Size(
            (int)Math.Round(a.Width + (b.Width - a.Width) * d),
            (int)Math.Round(a.Height + (b.Height - a.Height) * d))));

        var state = new SizeHolder();
        var context = Tween.Property<SizeHolder, Size>(s => s.Size).To(new Size(10, 20), TimeSpan.FromSeconds(1)).Build().CreateManualContext(state);
        context.Start();
        context.Tick(0.5);

        Assert.Multiple(() =>
        {
            Assert.That(state.Size, Is.EqualTo(new Size(5, 10)));
            Assert.That(TweenInterpolator.GetDefault(typeof(Size)), Is.SameAs(TweenInterpolator<Size>.Default));
        });
    }

    [Test]
    public void Test_UnsupportedType()
    {
        Assert.Throws<NotSupportedException>(() => _ = TweenInterpolator<Unsupported>.Default);
    }

    private sealed class Level : ITweenInterpolable<Level>
    {
        public Level(double value)
        {
            Value = value;
        }

        public double Value { get; }

        public Level Interpolate(double delta, Level? end) => new(Value + ((end?.Value ?? 0) - Value) * delta);
    }

    private sealed class SizeHolder
    {
        public Size Size { get; set; }
    }

    private sealed class Unsupported
    {
    }
}
