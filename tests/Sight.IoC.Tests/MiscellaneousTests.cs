using Sight.IoC.Tests.Models;

namespace Sight.IoC.Tests
{
    public class MiscellaneousTests
    {
        [Test]
        public void Test_as_readonly_resolve_same_types()
        {
            var container = new TypeContainer();
            var resolver = container.AsReadOnly();

            container.RegisterType<SimpleClass>();

            var testClass = resolver.Resolve<SimpleClass>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_as_immutable_resolve_copy_of_container()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClass>();

            var resolver = container.AsImmutable();

            var testClass = resolver.Resolve<SimpleClass>();

            Assert.NotNull(testClass, "testClass != null");

            container.RegisterType<SimpleClassWithDependency>();

            Assert.Throws<IoCException>(() => resolver.Resolve<SimpleClassWithDependency>());
        }

        [Test]
        public void Test_fallback_provider()
        {
            var fallback = new ResolveFallback((_, _) => true, (_, _) => new SimpleClass());
            var container = new TypeContainer(new TypeContainer.CreateOptions { Fallback = fallback });

            var testClass = container.Resolve<SimpleClass>();

            Assert.NotNull(testClass, "testClass != null");
        }
    }
}
