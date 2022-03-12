namespace Sight.IoC.Tests
{
    public class TypeContainerTests
    {
        [Test]
        public void Test_type_container_can_resolve_registered_type()
        {
            var container = new TypeContainer();
            container.RegisterInstance<ITestInterface01>(new TestClass01("test-01"));

            var testClass = container.Resolve<ITestInterface01>();

            Assert.NotNull(testClass, "testClass != null");
            Assert.AreEqual("test-01", testClass.Value);
        }

        [Test]
        public void Test_type_container_can_auto_resolve_class()
        {
            var container = new TypeContainer();
            var testClass = container.Resolve<TestClass01>(resolveOptions: new ResolveOptions
            {
                AutoResolve = true,
                AdditionalParameters =
                {
                    "test-02"
                }
            });

            Assert.AreEqual("test-02", testClass!.Value);
        }

        [Test]
        public void Test_type_container_can_resolve_generic_type()
        {
            var called = false;
            var container = new TypeContainer();
            container.RegisterGenericProvider(typeof(TestClass02<>), types =>
            {
                called = true;
                return Activator.CreateInstance(typeof(TestClass02<>).MakeGenericType(types));
            });

            var testClass = container.Resolve<TestClass02<string>>();

            Assert.NotNull(testClass, "testClass != null");
            Assert.IsTrue(called, "called");
        }

        [Test]
        public void Test_type_container_can_register_concrete_type_without_provider()
        {
            var container = new TypeContainer();
            container.RegisterType<TestClass03>();

            var testClass = container.Resolve<TestClass03>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_type_container_can_register_concrete_type_with_base_type_without_provider()
        {
            var container = new TypeContainer();
            container.RegisterType<ITestInterface01, TestClass03>();

            var testClass = container.Resolve<ITestInterface01>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_type_container_can_register_generic_type_without_provider()
        {
            var container = new TypeContainer();
            container.RegisterGenericType(typeof(TestClass02<>));

            var testClass = container.Resolve<TestClass02<string>>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_type_container_can_register_generic_type_with_base_type_without_provider()
        {
            var container = new TypeContainer();
            container.RegisterGenericType(typeof(TestClass02<>), typeof(ITestInterface02<>));

            var testClass = container.Resolve<ITestInterface02<string>>();

            Assert.NotNull(testClass, "testClass != null");
        }

        private interface ITestInterface01
        {
            public string Value { get; }
        }

        private interface ITestInterface02<T>
        {
            public T Value { get; }
        }

        private class TestClass01 : ITestInterface01
        {
            public TestClass01(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        private class TestClass02<T> : ITestInterface02<T>
        {
            public T Value { get; set; }
        }

        private class TestClass03 : ITestInterface01
        {
            public string Value { get; set; }
        }
    }
}
