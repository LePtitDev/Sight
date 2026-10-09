using Sight.IoC.Tests.Models;

namespace Sight.IoC.Tests
{
    public class ExceptionMessageTests
    {
        [Test]
        public void Test_message_explains_missing_registration()
        {
            var container = new TypeContainer();

            var ex = Assert.Throws<IoCException>(() => container.Resolve<ISimpleClassWithInterface>())!;

            StringAssert.Contains("Cannot resolve 'Sight.IoC.Tests.Models.ISimpleClassWithInterface'", ex.Message);
            StringAssert.Contains("No registration found", ex.Message);
        }

        [Test]
        public void Test_message_names_the_missing_dependency_of_a_registered_type()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClassWithDependency, ISimpleClassWithDependency>();
            // SimpleClass is a concrete class but auto wiring is off and it is not registered

            var ex = Assert.Throws<IoCException>(() => container.Resolve<ISimpleClassWithDependency>())!;

            TestContext.Out.WriteLine(ex.Message);
            StringAssert.Contains("Registration [Sight.IoC.Tests.Models.ISimpleClassWithDependency] cannot be resolved", ex.Message);
            StringAssert.Contains("Constructor SimpleClassWithDependency(Sight.IoC.Tests.Models.SimpleClass simpleClass)", ex.Message);
            StringAssert.Contains("Parameter 'simpleClass'", ex.Message);
            StringAssert.Contains("No registration found for 'Sight.IoC.Tests.Models.SimpleClass'", ex.Message);
        }

        [Test]
        public void Test_message_follows_nested_dependencies()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClassWithDependency, ISimpleClassWithDependency>();
            container.RegisterExpression<ISimpleClassWithInterface>(() => new SimpleClassWithSameInterfaceAndDependency(Arg.Of<SimpleClass>()));
            container.RegisterExpression<string>(() => Arg.Of<ISimpleClassWithDependency>().ToString()!, "text");

            var ex = Assert.Throws<IoCException>(() => container.Resolve<string>("text"))!;

            TestContext.Out.WriteLine(ex.Message);
            StringAssert.Contains("Dependency 'Sight.IoC.Tests.Models.ISimpleClassWithDependency' cannot be resolved", ex.Message);
            StringAssert.Contains("Parameter 'simpleClass'", ex.Message);
        }

        [Test]
        public void Test_message_explains_invoke_failure()
        {
            var container = new TypeContainer();
            Func<SimpleClass, int> method = x => 1;

            var ex = Assert.Throws<IoCException>(() => container.Invoke(method))!;

            TestContext.Out.WriteLine(ex.Message);
            StringAssert.Contains("Parameter 'x'", ex.Message);
            StringAssert.Contains("No registration found for 'Sight.IoC.Tests.Models.SimpleClass'", ex.Message);
        }

        [Test]
        public void Test_message_explains_abstract_type()
        {
            var container = new TypeContainer();

            var ex = Assert.Throws<IoCException>(() => container.Resolve<ISimpleClassWithInterface>(resolveOptions: new ResolveOptions { AutoResolve = true }))!;

            StringAssert.Contains("is an interface, it must be registered", ex.Message);
        }
    }
}
