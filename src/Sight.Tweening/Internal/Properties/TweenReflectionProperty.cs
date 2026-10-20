using System.Reflection;

namespace Sight.Tweening.Internal.Properties;

internal class TweenReflectionProperty : TweenProperty
{
    public TweenReflectionProperty(PropertyInfo property)
        : base(property.PropertyType)
    {
        Property = property;
    }

    public PropertyInfo Property { get; }

    public override object? Get(object? target) => Property.GetValue(target);

    public override void Set(object? target, object? value) => Property.SetValue(target, value);
}
