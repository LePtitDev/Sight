using System.Reflection;
using Sight.Linq;

namespace Sight.Linq.Tests;

public class ReflectionExtHardeningTests
{
    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    private sealed class MarkAttribute : Attribute;

    [AttributeUsage(AttributeTargets.All)]
    private sealed class ExplodingAttribute : Attribute
    {
        public ExplodingAttribute() => throw new InvalidOperationException("constructed");
    }

    private class BaseWithProperty
    {
        [Mark]
        public virtual int Value { get; set; }

        [Mark]
        public virtual event EventHandler? Changed;

        [Exploding]
        public int Fragile { get; set; }

        protected void Raise() => Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class DerivedWithProperty : BaseWithProperty
    {
        public override int Value { get; set; }

        public override event EventHandler? Changed;

        public void RaiseDerived() => Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>A provider that is not a runtime reflection object</summary>
    private sealed class CustomProvider : ICustomAttributeProvider
    {
        public object[] GetCustomAttributes(bool inherit) => [new MarkAttribute()];

        // The runtime type of this array is object[], not MarkAttribute[]
        public object[] GetCustomAttributes(Type attributeType, bool inherit) => [new MarkAttribute()];

        public bool IsDefined(Type attributeType, bool inherit) => attributeType == typeof(MarkAttribute);
    }

    [Test]
    public void Test_get_attributes_works_with_custom_providers()
    {
        var attributes = new CustomProvider().GetAttributes<MarkAttribute>();

        Assert.AreEqual(1, attributes.Length);
        Assert.IsTrue(new CustomProvider().HasAttribute<MarkAttribute>());
    }

    [Test]
    public void Test_inherit_is_honored_for_overridden_properties()
    {
        var property = typeof(DerivedWithProperty).GetProperty(nameof(DerivedWithProperty.Value))!;

        Assert.IsTrue(property.HasAttribute<MarkAttribute>(inherit: true));
        Assert.IsNotNull(property.GetAttribute<MarkAttribute>(inherit: true));
        Assert.IsFalse(property.HasAttribute<MarkAttribute>(inherit: false));
        Assert.IsNull(property.GetAttribute<MarkAttribute>(inherit: false));
    }

    [Test]
    public void Test_inherit_is_honored_for_overridden_events()
    {
        var evt = typeof(DerivedWithProperty).GetEvent(nameof(DerivedWithProperty.Changed))!;

        Assert.IsTrue(evt.HasAttribute<MarkAttribute>(inherit: true));
        Assert.IsFalse(evt.HasAttribute<MarkAttribute>(inherit: false));
    }

    [Test]
    public void Test_has_attribute_does_not_instantiate_attribute()
    {
        var property = typeof(BaseWithProperty).GetProperty(nameof(BaseWithProperty.Fragile))!;

        Assert.IsTrue(property.HasAttribute<ExplodingAttribute>());
        Assert.IsTrue(property.HasAttribute(typeof(ExplodingAttribute)));
    }

    [Test]
    public void Test_get_types_of_finds_nested_derived_types()
    {
        var types = typeof(ReflectionExtHardeningTests).Assembly.GetTypesOf<BaseWithProperty>();

        CollectionAssert.AreEquivalent(new[] { typeof(DerivedWithProperty) }, types);
    }
}

public interface IRepository<T>;

public abstract class RepositoryBase<T> : IRepository<T>;

public sealed class UserRepository : RepositoryBase<string>;

public sealed class OtherRepository : IRepository<int>;

public sealed class OpenGenericTests
{
    [Test]
    public void Test_get_types_of_supports_open_generic_interface()
    {
        var types = typeof(OpenGenericTests).Assembly.GetTypesOf(typeof(IRepository<>));

        CollectionAssert.AreEquivalent(new[] { typeof(RepositoryBase<>), typeof(UserRepository), typeof(OtherRepository) }, types);
    }

    [Test]
    public void Test_get_types_of_supports_open_generic_base_class()
    {
        var types = typeof(OpenGenericTests).Assembly.GetTypesOf(typeof(RepositoryBase<>));

        CollectionAssert.AreEquivalent(new[] { typeof(UserRepository) }, types);
    }

    [Test]
    public void Test_get_types_of_can_exclude_generic_types()
    {
        var types = typeof(OpenGenericTests).Assembly.GetTypesOf(typeof(IRepository<>), withGeneric: false);

        CollectionAssert.AreEquivalent(new[] { typeof(UserRepository), typeof(OtherRepository) }, types);
    }
}
