using System.Numerics;

namespace Sight.Tweening.Internal.Interpolators;

internal sealed class TweenDateTimeInterpolator : TweenInterpolator<DateTime>
{
    public override DateTime Interpolate(double delta, DateTime begin, DateTime end)
    {
        if (delta == 0)
            return begin;

        if (delta == 1)
            return end;

        var ticks = (1 - delta) * begin.Ticks + delta * end.Ticks;
        return new DateTime((long)Math.Clamp(ticks, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks), begin.Kind);
    }
}

internal sealed class TweenDateTimeOffsetInterpolator : TweenInterpolator<DateTimeOffset>
{
    public override DateTimeOffset Interpolate(double delta, DateTimeOffset begin, DateTimeOffset end)
    {
        if (delta == 0)
            return begin;

        if (delta == 1)
            return end;

        var ticks = (1 - delta) * begin.UtcTicks + delta * end.UtcTicks;
        var utc = new DateTimeOffset((long)Math.Clamp(ticks, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks), TimeSpan.Zero);
        return utc.ToOffset(begin.Offset);
    }
}

internal sealed class TweenVector2Interpolator : TweenInterpolator<Vector2>, ITweenArithmetic<Vector2>
{
    public override Vector2 Interpolate(double delta, Vector2 begin, Vector2 end)
    {
        var t = (float)delta;
        return begin * (1 - t) + end * t;
    }

    public Vector2 Add(Vector2 left, Vector2 right) => left + right;

    public Vector2 Subtract(Vector2 left, Vector2 right) => left - right;

    public Vector2 Scale(Vector2 value, double factor) => value * (float)factor;

    public Vector2 RandomOffset(Vector2 amplitude, Random random) => amplitude * new Vector2(NextSigned(random), NextSigned(random));

    internal static float NextSigned(Random random) => (float)(random.NextDouble() * 2 - 1);
}

internal sealed class TweenVector3Interpolator : TweenInterpolator<Vector3>, ITweenArithmetic<Vector3>
{
    public override Vector3 Interpolate(double delta, Vector3 begin, Vector3 end)
    {
        var t = (float)delta;
        return begin * (1 - t) + end * t;
    }

    public Vector3 Add(Vector3 left, Vector3 right) => left + right;

    public Vector3 Subtract(Vector3 left, Vector3 right) => left - right;

    public Vector3 Scale(Vector3 value, double factor) => value * (float)factor;

    public Vector3 RandomOffset(Vector3 amplitude, Random random)
    {
        return amplitude * new Vector3(
            TweenVector2Interpolator.NextSigned(random),
            TweenVector2Interpolator.NextSigned(random),
            TweenVector2Interpolator.NextSigned(random));
    }
}

internal sealed class TweenVector4Interpolator : TweenInterpolator<Vector4>, ITweenArithmetic<Vector4>
{
    public override Vector4 Interpolate(double delta, Vector4 begin, Vector4 end)
    {
        var t = (float)delta;
        return begin * (1 - t) + end * t;
    }

    public Vector4 Add(Vector4 left, Vector4 right) => left + right;

    public Vector4 Subtract(Vector4 left, Vector4 right) => left - right;

    public Vector4 Scale(Vector4 value, double factor) => value * (float)factor;

    public Vector4 RandomOffset(Vector4 amplitude, Random random)
    {
        return amplitude * new Vector4(
            TweenVector2Interpolator.NextSigned(random),
            TweenVector2Interpolator.NextSigned(random),
            TweenVector2Interpolator.NextSigned(random),
            TweenVector2Interpolator.NextSigned(random));
    }
}

internal sealed class TweenQuaternionInterpolator : TweenInterpolator<Quaternion>
{
    public override Quaternion Interpolate(double delta, Quaternion begin, Quaternion end)
    {
        if (delta == 0)
            return begin;

        if (delta == 1)
            return end;

        return Quaternion.Slerp(begin, end, (float)delta);
    }
}
