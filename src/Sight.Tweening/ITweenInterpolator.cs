namespace Sight.Tweening;

/// <summary>
/// Untyped interpolator.
/// </summary>
public interface ITweenInterpolator
{
    /// <summary>
    /// Interpolates between <paramref name="begin"/> and <paramref name="end"/>.
    /// </summary>
    /// <param name="delta">Progress, usually between 0 and 1 (can overshoot with some easings).</param>
    /// <param name="begin">Value at progress 0.</param>
    /// <param name="end">Value at progress 1.</param>
    public object? Interpolate(double delta, object? begin, object? end);
}
