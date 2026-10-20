using System.ComponentModel;
using System.Reflection;
using Sight.Linq;

namespace Sight.Linq.Tests;

public class ReflectionExtTests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    private sealed class ItemAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
    }

    private interface IShape;

    private abstract class ShapeBase : IShape;

    private sealed class Circle : ShapeBase;

    private sealed class Square : IShape;

    [DisplayName("My Class")]
    private sealed class Named;

    [Item("One")]
    [Item("Two")]
    private sealed class Multi;

    [Test]
    public void Test_get_attribute_returns_attribute()
    {
        Assert.AreEqual("My Class", typeof(Named).GetAttribute<DisplayNameAttribute>()!.DisplayName);
        Assert.IsNull(typeof(Square).GetAttribute<DisplayNameAttribute>());
    }

    [Test]
    public void Test_get_attributes_returns_all_attributes()
    {
        var names = typeof(Multi).GetAttributes<ItemAttribute>().Select(x => x.Name);

        CollectionAssert.AreEquivalent(new[] { "One", "Two" }, names);
    }

    [Test]
    public void Test_has_attribute()
    {
        Assert.IsTrue(typeof(Named).HasAttribute<DisplayNameAttribute>());
        Assert.IsTrue(typeof(Named).HasAttribute(typeof(DisplayNameAttribute)));
        Assert.IsFalse(typeof(Square).HasAttribute<DisplayNameAttribute>());
    }

    [Test]
    public void Test_get_types_of_returns_implementations()
    {
        var types = Assembly.GetExecutingAssembly().GetTypesOf<IShape>();

        CollectionAssert.AreEquivalent(new[] { typeof(ShapeBase), typeof(Circle), typeof(Square) }, types);
    }

    [Test]
    public void Test_get_types_of_can_exclude_abstract_types()
    {
        var types = Assembly.GetExecutingAssembly().GetTypesOf<IShape>(withAbstract: false);

        CollectionAssert.AreEquivalent(new[] { typeof(Circle), typeof(Square) }, types);
    }
}
