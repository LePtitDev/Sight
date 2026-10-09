using Sight.IoC.Tests.Models;

namespace Sight.IoC.Tests;

public class RegisterExpressionTests
{
    [Test]
    public void Test_can_register_expression_with_dependency()
    {
        var container = new TypeContainer();
        var dependency = new SimpleClass { Value = "dep" };
        container.RegisterInstance(dependency);
        container.RegisterExpression<ISimpleClassWithDependency>(() => new SimpleClassWithDependency(Arg.Of<SimpleClass>()));

        var testClass = container.Resolve<ISimpleClassWithDependency>();

        Assert.NotNull(testClass, "testClass != null");
        Assert.AreSame(dependency, testClass!.SimpleClass);
    }

    [Test]
    public void Test_can_register_expression_with_named_dependencies()
    {
        var container = new TypeContainer();
        container.RegisterInstance(new SimpleClass { Value = "a" }, "a");
        container.RegisterInstance(new SimpleClass { Value = "b" }, "b");
        container.RegisterExpression<string>(() => Arg.Of<SimpleClass>("b").Value + string.Concat(new[] { Arg.Of<SimpleClass>("a") }.Select(x => x.Value)));

        Assert.AreEqual("ba", container.Resolve<string>());
    }

    [Test]
    public void Test_expression_dependency_can_be_auto_wired()
    {
        var container = new TypeContainer();
        container.RegisterExpression<ISimpleClassWithDependency>(() => new SimpleClassWithDependency(Arg.Of<SimpleClass>()));

        Assert.IsFalse(container.IsResolvable<ISimpleClassWithDependency>());
        Assert.NotNull(container.Resolve<ISimpleClassWithDependency>(resolveOptions: new ResolveOptions { AutoWiring = true }), "service != null");
    }

    [Test]
    public void Test_expression_not_resolvable_when_dependency_missing()
    {
        var container = new TypeContainer();
        container.RegisterExpression<ISimpleClassWithDependency>(() => new SimpleClassWithDependency(Arg.Of<SimpleClass>()));

        Assert.IsFalse(container.IsResolvable<ISimpleClassWithDependency>());
    }

    [Test]
    public void Test_expression_creates_new_instance_each_time_unless_lazy()
    {
        var container = new TypeContainer();
        container.RegisterExpression<SimpleClass>(() => new SimpleClass(), "transient");
        container.RegisterExpression<SimpleClass>(() => new SimpleClass(), "lazy", lazy: true);

        Assert.AreNotSame(container.Resolve<SimpleClass>("transient"), container.Resolve<SimpleClass>("transient"));
        Assert.AreSame(container.Resolve<SimpleClass>("lazy"), container.Resolve<SimpleClass>("lazy"));
    }

    [Test]
    public void Test_non_constant_dependency_name_is_rejected()
    {
        var container = new TypeContainer();
        var name = "a";

        Assert.Throws<IoCException>(() => container.RegisterExpression<SimpleClass>(() => Arg.Of<SimpleClass>(name)));
    }
}
