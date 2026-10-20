using System.Linq.Expressions;

namespace Sight.Tweening.Internal.Properties;

internal class TweenExpressionProperty<TTarget, TValue> : TweenProperty<TValue>
    where TTarget : class
{
    private readonly Func<TTarget, TValue?> _getter;
    private readonly Action<TTarget, TValue?> _setter;

    public TweenExpressionProperty(Expression<Func<TTarget, TValue?>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression)
            throw new NotSupportedException("Not supported expression");

        _getter = expression.Compile();

        var param = Expression.Parameter(typeof(TValue), "_value_");
        var assign = Expression.Assign(memberExpression, param);
        _setter = Expression.Lambda<Action<TTarget, TValue?>>(assign, [expression.Parameters[0], param]).Compile();
    }

    public override TValue? Get(object? target)
    {
        return _getter(target is TTarget t ? t : throw new ArgumentException($"Value must be of type '{typeof(TTarget).FullName}'"));
    }

    public override void Set(object? target, TValue? value)
    {
        _setter(target is TTarget t ? t : throw new ArgumentException($"Value must be of type '{typeof(TTarget).FullName}'"), value);
    }
}
