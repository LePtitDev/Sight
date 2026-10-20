namespace Sight.Tweening;

/// <summary>
/// Optional capability of a <see cref="TweenInterpolator{T}"/> providing the arithmetic required by
/// relative tweens (<c>By</c>), shakes, punches and Catmull-Rom paths.
/// </summary>
/// <typeparam name="T">Animated value type.</typeparam>
public interface ITweenArithmetic<T>
{
    /// <summary>
    /// Adds two values.
    /// </summary>
    public T Add(T left, T right);

    /// <summary>
    /// Subtracts <paramref name="right"/> from <paramref name="left"/>.
    /// </summary>
    public T Subtract(T left, T right);

    /// <summary>
    /// Multiplies a value by a scalar factor.
    /// </summary>
    public T Scale(T value, double factor);

    /// <summary>
    /// Creates a random offset whose components are uniformly distributed in [-amplitude, amplitude].
    /// </summary>
    public T RandomOffset(T amplitude, Random random);
}
