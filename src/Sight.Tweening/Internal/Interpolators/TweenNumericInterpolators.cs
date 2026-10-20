namespace Sight.Tweening.Internal.Interpolators;

/// <summary>
/// Interpolates numeric values through <see cref="double"/>. Endpoints are returned exactly.
/// </summary>
internal abstract class TweenNumericInterpolator<T> : TweenInterpolator<T>, ITweenArithmetic<T>
    where T : struct
{
    public override T Interpolate(double delta, T begin, T end)
    {
        if (delta == 0)
            return begin;

        if (delta == 1)
            return end;

        return FromDouble((1 - delta) * ToDouble(begin) + delta * ToDouble(end));
    }

    public T Add(T left, T right) => FromDouble(ToDouble(left) + ToDouble(right));

    public T Subtract(T left, T right) => FromDouble(ToDouble(left) - ToDouble(right));

    public T Scale(T value, double factor) => FromDouble(ToDouble(value) * factor);

    public T RandomOffset(T amplitude, Random random) => FromDouble(ToDouble(amplitude) * (random.NextDouble() * 2 - 1));

    protected abstract double ToDouble(T value);

    protected abstract T FromDouble(double value);

    protected static double Round(double value, double min, double max)
    {
        if (double.IsNaN(value))
            return 0;

        return Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), min, max);
    }
}

internal sealed class TweenInt8Interpolator : TweenNumericInterpolator<sbyte>
{
    protected override double ToDouble(sbyte value) => value;

    protected override sbyte FromDouble(double value) => (sbyte)Round(value, sbyte.MinValue, sbyte.MaxValue);
}

internal sealed class TweenUInt8Interpolator : TweenNumericInterpolator<byte>
{
    protected override double ToDouble(byte value) => value;

    protected override byte FromDouble(double value) => (byte)Round(value, byte.MinValue, byte.MaxValue);
}

internal sealed class TweenInt16Interpolator : TweenNumericInterpolator<short>
{
    protected override double ToDouble(short value) => value;

    protected override short FromDouble(double value) => (short)Round(value, short.MinValue, short.MaxValue);
}

internal sealed class TweenUInt16Interpolator : TweenNumericInterpolator<ushort>
{
    protected override double ToDouble(ushort value) => value;

    protected override ushort FromDouble(double value) => (ushort)Round(value, ushort.MinValue, ushort.MaxValue);
}

internal sealed class TweenInt32Interpolator : TweenNumericInterpolator<int>
{
    protected override double ToDouble(int value) => value;

    protected override int FromDouble(double value) => (int)Round(value, int.MinValue, int.MaxValue);
}

internal sealed class TweenUInt32Interpolator : TweenNumericInterpolator<uint>
{
    protected override double ToDouble(uint value) => value;

    protected override uint FromDouble(double value) => (uint)Round(value, uint.MinValue, uint.MaxValue);
}

internal sealed class TweenInt64Interpolator : TweenNumericInterpolator<long>
{
    // long.MaxValue is not representable as a double: the closest double below it is used as upper bound.
    private const double MaxValue = 9223372036854774784d;

    protected override double ToDouble(long value) => value;

    protected override long FromDouble(double value) => (long)Round(value, long.MinValue, MaxValue);
}

internal sealed class TweenUInt64Interpolator : TweenNumericInterpolator<ulong>
{
    // ulong.MaxValue is not representable as a double: the closest double below it is used as upper bound.
    private const double MaxValue = 18446744073709549568d;

    protected override double ToDouble(ulong value) => value;

    protected override ulong FromDouble(double value) => (ulong)Round(value, ulong.MinValue, MaxValue);
}

internal sealed class TweenSingleInterpolator : TweenNumericInterpolator<float>
{
    protected override double ToDouble(float value) => value;

    protected override float FromDouble(double value) => (float)value;
}

internal sealed class TweenDoubleInterpolator : TweenNumericInterpolator<double>
{
    protected override double ToDouble(double value) => value;

    protected override double FromDouble(double value) => value;
}

#if NET5_0_OR_GREATER
internal sealed class TweenHalfInterpolator : TweenNumericInterpolator<Half>
{
    protected override double ToDouble(Half value) => (double)value;

    protected override Half FromDouble(double value) => (Half)value;
}
#endif

internal sealed class TweenTimeSpanInterpolator : TweenNumericInterpolator<TimeSpan>
{
    protected override double ToDouble(TimeSpan value) => value.Ticks;

    protected override TimeSpan FromDouble(double value) => new((long)Round(value, long.MinValue, 9223372036854774784d));
}

internal sealed class TweenDecimalInterpolator : TweenInterpolator<decimal>, ITweenArithmetic<decimal>
{
    public override decimal Interpolate(double delta, decimal begin, decimal end)
    {
        if (delta == 0)
            return begin;

        if (delta == 1)
            return end;

        return begin + (end - begin) * (decimal)delta;
    }

    public decimal Add(decimal left, decimal right) => left + right;

    public decimal Subtract(decimal left, decimal right) => left - right;

    public decimal Scale(decimal value, double factor) => value * (decimal)factor;

    public decimal RandomOffset(decimal amplitude, Random random) => amplitude * (decimal)(random.NextDouble() * 2 - 1);
}

#if NET7_0_OR_GREATER
internal sealed class TweenGenericNumberInterpolator<T> : TweenNumericInterpolator<T>
    where T : struct, System.Numerics.INumberBase<T>
{
    // Converting 0.5 to an integral type truncates it to zero.
    private static readonly bool s_isIntegral = T.IsZero(T.CreateTruncating(0.5));

    protected override double ToDouble(T value) => double.CreateSaturating(value);

    protected override T FromDouble(double value)
    {
        if (s_isIntegral)
            value = Math.Round(value, MidpointRounding.AwayFromZero);

        return T.CreateSaturating(value);
    }
}
#endif
