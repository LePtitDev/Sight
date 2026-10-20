namespace Sight.Tweening;

/// <summary>
/// Accessor of an animated value on a target.
/// </summary>
public interface ITweenProperty
{
    /// <summary>
    /// Gets the type of the value.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Reads the value from <paramref name="target"/>.
    /// </summary>
    public object? Get(object? target);

    /// <summary>
    /// Writes <paramref name="value"/> to <paramref name="target"/>.
    /// </summary>
    public void Set(object? target, object? value);
}

/// <summary>
/// Strongly typed accessor of an animated value, used to animate without boxing.
/// </summary>
/// <typeparam name="T">Type of the value.</typeparam>
public interface ITweenProperty<T> : ITweenProperty
{
    /// <summary>
    /// Reads the value from <paramref name="target"/>.
    /// </summary>
    public new T? Get(object? target);

    /// <summary>
    /// Writes <paramref name="value"/> to <paramref name="target"/>.
    /// </summary>
    public void Set(object? target, T? value);
}
