namespace Sight.Tweening.Internal.Interpolators;

internal sealed class TweenDelegateInterpolator<T> : TweenInterpolator<T>
{
    public override T? Interpolate(double delta, T? begin, T? end)
    {
        if (begin is null)
            return delta >= 0.5 ? end : begin;

        return ((ITweenInterpolable<T>)begin).Interpolate(delta, end);
    }
}

internal sealed class TweenFuncInterpolator<T> : TweenInterpolator<T>
{
    private readonly Func<double, T?, T?, T?> _interpolate;

    public TweenFuncInterpolator(Func<double, T?, T?, T?> interpolate)
    {
        _interpolate = interpolate;
    }

    public override T? Interpolate(double delta, T? begin, T? end) => _interpolate(delta, begin, end);
}

internal sealed class TweenNullableInterpolator<T> : TweenInterpolator<T?>
    where T : struct
{
    private readonly TweenInterpolator<T> _interpolator;

    public TweenNullableInterpolator(TweenInterpolator<T> interpolator)
    {
        _interpolator = interpolator;
    }

    public override T? Interpolate(double delta, T? begin, T? end)
    {
        if (begin.HasValue && end.HasValue)
            return _interpolator.Interpolate(delta, begin.Value, end.Value);

        return delta >= 0.5 ? end : begin;
    }
}
