namespace Sight.Tweening;

/// <summary>
/// Base class of untyped property accessors.
/// </summary>
public abstract class TweenProperty : ITweenProperty
{
    /// <summary>
    /// Initializes the accessor of a value of type <paramref name="type"/>.
    /// </summary>
    protected TweenProperty(Type type)
    {
        Type = type;
    }

    /// <inheritdoc />
    public Type Type { get; }

    /// <inheritdoc />
    public abstract object? Get(object? target);

    /// <inheritdoc />
    public abstract void Set(object? target, object? value);
}

/// <summary>
/// Base class of typed property accessors.
/// </summary>
/// <typeparam name="T">Type of the value.</typeparam>
public abstract class TweenProperty<T> : ITweenProperty<T>
{
    Type ITweenProperty.Type => typeof(T);

    /// <inheritdoc />
    public abstract T? Get(object? target);

    /// <inheritdoc />
    public abstract void Set(object? target, T? value);

    object? ITweenProperty.Get(object? target) => Get(target);

    void ITweenProperty.Set(object? target, object? value)
    {
        if (value is null && default(T) is null)
        {
            Set(target, default);
            return;
        }

        Set(target, value is T x ? x : throw new ArgumentException($"Value must be of type '{typeof(T).FullName}'"));
    }
}
