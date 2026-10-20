namespace Sight.Tweening.Internal.Properties;

internal sealed class TweenDelegateProperty<TTarget, TValue> : TweenProperty<TValue>
    where TTarget : class
{
    private readonly Func<TTarget, TValue> _getter;
    private readonly Action<TTarget, TValue> _setter;

    public TweenDelegateProperty(Func<TTarget, TValue> getter, Action<TTarget, TValue> setter)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

    public override TValue? Get(object? target) => _getter(Cast(target));

    public override void Set(object? target, TValue? value) => _setter(Cast(target), value!);

    private static TTarget Cast(object? target)
    {
        return target as TTarget ?? throw new ArgumentException($"Target argument must be of type '{typeof(TTarget).FullName}'");
    }
}

internal sealed class TweenDelegateProperty<TValue> : TweenProperty<TValue>
{
    private readonly Func<TValue> _getter;
    private readonly Action<TValue> _setter;

    public TweenDelegateProperty(Func<TValue> getter, Action<TValue> setter)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

    public override TValue? Get(object? target) => _getter();

    public override void Set(object? target, TValue? value) => _setter(value!);
}

/// <summary>
/// Adapts an untyped property to the typed API (values are boxed).
/// </summary>
internal sealed class TweenPropertyAdapter<TValue> : TweenProperty<TValue>
{
    private readonly ITweenProperty _property;

    public TweenPropertyAdapter(ITweenProperty property)
    {
        if (!typeof(TValue).IsAssignableFrom(property.Type))
            throw new ArgumentException($"Property type '{property.Type.FullName}' is not compatible with '{typeof(TValue).FullName}'", nameof(property));

        _property = property;
    }

    public override TValue? Get(object? target) => (TValue?)_property.Get(target);

    public override void Set(object? target, TValue? value) => _property.Set(target, value);
}
