using Sight.IoC.Tests.Models;

namespace Sight.IoC.Tests
{
    public class RegisterTypeTests
    {
        [Test]
        public void Test_can_register_concrete_type_without_provider()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClass>();

            var testClass = container.Resolve<SimpleClass>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_can_register_concrete_type_with_base_type_without_provider()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClassWithInterface, ISimpleClassWithInterface>();

            var testClass = container.Resolve<ISimpleClassWithInterface>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_can_register_generic_type_without_provider()
        {
            var container = new TypeContainer();
            container.RegisterType(typeof(GenericClass<>));

            var testClass = container.Resolve<GenericClass<string>>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_can_register_generic_type_with_base_type_without_provider()
        {
            var container = new TypeContainer();
            container.RegisterType(typeof(GenericClassWithInterface<>), typeof(IGenericClassWithInterface<>));

            var testClass = container.Resolve<IGenericClassWithInterface<string>>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_resolved_type_as_lazy_instanced_once()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClass>(lazy: true);

            var testClass1 = container.Resolve<SimpleClass>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var expected = Guid.NewGuid().ToString("D");
            testClass1.Value = expected;

            var testClass2 = container.Resolve<SimpleClass>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreEqual(expected, testClass2.Value);
        }

        [Test]
        public void Test_resolved_type_as_not_lazy_instanced_each_time()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClass>(lazy: false);

            var testClass1 = container.Resolve<SimpleClass>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var expected = Guid.NewGuid().ToString("D");
            testClass1.Value = expected;

            var testClass2 = container.Resolve<SimpleClass>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreNotEqual(expected, testClass2.Value);
        }

        [Test]
        public void Test_resolved_generic_type_as_lazy_instanced_once()
        {
            var container = new TypeContainer();
            container.RegisterType(typeof(GenericClass<>), lazy: true);

            var testClass1 = container.Resolve<GenericClass<string>>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var expected = Guid.NewGuid().ToString("D");
            testClass1.Value = expected;

            var testClass2 = container.Resolve<GenericClass<string>>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreEqual(expected, testClass2.Value);
        }

        [Test]
        public void Test_resolved_generic_type_as_not_lazy_instanced_each_time()
        {
            var container = new TypeContainer();
            container.RegisterType(typeof(GenericClass<>), lazy: false);

            var testClass1 = container.Resolve<GenericClass<string>>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var expected = Guid.NewGuid().ToString("D");
            testClass1.Value = expected;

            var testClass2 = container.Resolve<GenericClass<string>>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreNotEqual(expected, testClass2.Value);
        }

        [Test]
        public void Test_lazy_generic_can_resolve_multiple_type_parameters()
        {
            var container = new TypeContainer();
            container.RegisterType(typeof(GenericClassWithInterface<>), typeof(IGenericClassWithInterface<>), lazy: true);

            var testClass1 = container.Resolve<IGenericClassWithInterface<SimpleClass>>();
            var testClass2 = container.Resolve<IGenericClassWithInterface<SimpleClassWithInterface>>();

            Assert.IsNotNull(testClass1);
            Assert.IsNotNull(testClass2);
        }
    }
}
