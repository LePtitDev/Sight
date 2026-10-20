namespace Sight.Tweening.Internal.Properties;

internal class TweenDelegateProperty : TweenProperty
{
    private readonly Func<object?, object?> _getter;
    private readonly Action<object?, object?> _setter;

    public TweenDelegateProperty(Type type, Func<object?> getter, Action<object?> setter)
        : this(type, _ => getter(), (_, v) => setter(v))
    {
    }

    public TweenDelegateProperty(Type type, Func<object?, object?> getter, Action<object?, object?> setter)
        : base(type)
    {
        _getter = getter;
        _setter = setter;
    }

    public override object? Get(object? target) => _getter(target);

    public override void Set(object? target, object? value) => _setter(target, value);
}
