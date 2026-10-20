namespace Sight.Tweening;

/// <summary>
/// Type able to interpolate itself, used when no interpolator is registered for it.
/// </summary>
/// <typeparam name="T">The implementing type.</typeparam>
public interface ITweenInterpolable<T>
{
    /// <summary>
    /// Interpolates from this value to <paramref name="end"/>.
    /// </summary>
    /// <param name="delta">Progress, usually between 0 and 1 (can overshoot with some easings).</param>
    /// <param name="end">Value at progress 1.</param>
    public T? Interpolate(double delta, T? end);
}
