namespace Sight.Tweening;

/// <summary>
/// Untyped transition computing its end value from its start value.
/// </summary>
public class TweenTransition : ITweenTransition
{
    private readonly Func<object?, object?> _func;
    private readonly object? _initialValue;

    /// <summary>
    /// Initializes a transition.
    /// </summary>
    /// <param name="func">Computes the end value from the start value.</param>
    /// <param name="initialValue">Start value, or <see langword="null"/> to start from the current value.</param>
    public TweenTransition(Func<object?, object?> func, object? initialValue = null)
    {
        _func = func;
        _initialValue = initialValue;
    }

    /// <inheritdoc />
    public object? GetSourceValue() => _initialValue;

    /// <inheritdoc />
    public object? GetTargetValue(object? initialValue) => _func(initialValue);

    /// <summary>
    /// Creates a transition to <paramref name="value"/>.
    /// </summary>
    public static ITweenTransition To(object? value, object? initialValue = null) => new TweenTransition(_ => value, initialValue);

    /// <summary>
    /// Creates a typed transition to <paramref name="value"/>.
    /// </summary>
    public static ITweenTransition To<T>(T? value) => new TweenTransition<T>(_ => value);

    /// <summary>
    /// Creates a typed transition from <paramref name="initialValue"/> to <paramref name="value"/>.
    /// </summary>
    public static ITweenTransition To<T>(T? value, T? initialValue) => new TweenTransition<T>(_ => value, initialValue);
}

/// <summary>
/// Typed transition computing its end value from its start value.
/// </summary>
/// <typeparam name="T">Type of the value.</typeparam>
public class TweenTransition<T> : ITweenTransition
{
    private readonly Func<T?, T?> _func;
    private readonly T? _initialValue;
    private readonly bool _hasInitialValue;

    /// <summary>
    /// Initializes a transition starting from the current value.
    /// </summary>
    public TweenTransition(Func<T?, T?> func)
    {
        _func = func;
        _initialValue = default;
        _hasInitialValue = false;
    }

    /// <summary>
    /// Initializes a transition starting from <paramref name="initialValue"/>.
    /// </summary>
    public TweenTransition(Func<T?, T?> func, T? initialValue)
    {
        _func = func;
        _initialValue = initialValue;
        _hasInitialValue = true;
    }

    /// <inheritdoc />
    public object? GetSourceValue() => _hasInitialValue ? _initialValue : null;

    /// <inheritdoc />
    public object? GetTargetValue(object? initialValue)
    {
        if (initialValue is null)
        {
            if (typeof(T).IsValueType)
                throw new ArgumentException($"Value must be of type '{typeof(T).FullName}'");

            return _func(default);
        }

        return _func(initialValue is T val ? val : throw new ArgumentException($"Value must be of type '{typeof(T).FullName}'"));
    }

    /// <summary>
    /// Creates a transition to <paramref name="value"/>.
    /// </summary>
    public static ITweenTransition To(T? value) => new TweenTransition<T>(_ => value);
}
