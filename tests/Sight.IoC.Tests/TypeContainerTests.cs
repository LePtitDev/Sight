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
        public void Test_type_container_instantiate_lazy_once()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new TestClass01(Guid.NewGuid().ToString("D")), lazy: true);

            var testClass1 = container.Resolve<TestClass01>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var testClass2 = container.Resolve<TestClass01>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreEqual(testClass1.Value, testClass2.Value);
        }

        [Test]
        public void Test_type_container_instantiate_not_lazy_each_time()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new TestClass01(Guid.NewGuid().ToString("D")), lazy: false);

            var testClass1 = container.Resolve<TestClass01>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var testClass2 = container.Resolve<TestClass01>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreNotEqual(testClass1.Value, testClass2.Value);
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

        [Test]
        public void Test_resolved_type_as_lazy_instanced_once()
        {
            var container = new TypeContainer();
            container.RegisterType<TestClass03>(lazy: true);

            var testClass1 = container.Resolve<TestClass03>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var expected = Guid.NewGuid().ToString("D");
            testClass1.Value = expected;

            var testClass2 = container.Resolve<TestClass03>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreEqual(expected, testClass2.Value);
        }

        [Test]
        public void Test_resolved_type_as_not_lazy_instanced_each_time()
        {
            var container = new TypeContainer();
            container.RegisterType<TestClass03>(lazy: false);

            var testClass1 = container.Resolve<TestClass03>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var expected = Guid.NewGuid().ToString("D");
            testClass1.Value = expected;

            var testClass2 = container.Resolve<TestClass03>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreNotEqual(expected, testClass2.Value);
        }

        [Test]
        public void Test_resolved_generic_type_as_lazy_instanced_once()
        {
            var container = new TypeContainer();
            container.RegisterGenericType(typeof(TestClass02<>), lazy: true);

            var testClass1 = container.Resolve<TestClass02<string>>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var expected = Guid.NewGuid().ToString("D");
            testClass1.Value = expected;

            var testClass2 = container.Resolve<TestClass02<string>>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreEqual(expected, testClass2.Value);
        }

        [Test]
        public void Test_resolved_generic_type_as_not_lazy_instanced_each_time()
        {
            var container = new TypeContainer();
            container.RegisterGenericType(typeof(TestClass02<>), lazy: false);

            var testClass1 = container.Resolve<TestClass02<string>>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var expected = Guid.NewGuid().ToString("D");
            testClass1.Value = expected;

            var testClass2 = container.Resolve<TestClass02<string>>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreNotEqual(expected, testClass2.Value);
        }

        [Test]
        public void Test_container_can_resolve_type_when_dependency_found()
        {
            var container = new TypeContainer();
            container.RegisterType<TestClass03>();
            container.RegisterType<TestClass04>();

            var testClass = container.Resolve<TestClass04>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_container_cannot_resolve_type_when_no_dependency_found()
        {
            var container = new TypeContainer();
            container.RegisterType<TestClass04>();

            Assert.Throws<IoCException>(() => container.Resolve<TestClass04>());
        }

        [Test]
        public void Test_container_can_auto_resolve_type_when_dependency_found()
        {
            var container = new TypeContainer();
            container.RegisterType<TestClass03>();

            var testClass = container.Resolve<TestClass04>(resolveOptions: new ResolveOptions { AutoResolve = true });

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_container_do_not_throw_when_cannot_resolve_type()
        {
            var container = new TypeContainer();
            container.RegisterType<TestClass04>();

            var testClass = container.Resolve<TestClass04>(resolveOptions: new ResolveOptions { IsOptional = true });

            Assert.Null(testClass, "testClass == null");
        }

        [Test]
        public void Test_container_resolve_named_service_correctly()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new TestClass01("test 01"), "TEST01");
            container.RegisterProvider((_, _) => new TestClass01("test 02"), "TEST02");

            var testClass1 = container.Resolve<TestClass01>("TEST01");

            Assert.NotNull(testClass1, "testClass1 != null");
            Assert.AreEqual("test 01", testClass1.Value);

            var testClass2 = container.Resolve<TestClass01>("TEST02");

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreEqual("test 02", testClass2.Value);
        }

        [Test]
        public void Test_as_readonly_resolve_same_types()
        {
            var container = new TypeContainer();
            var resolver = container.AsReadOnly();

            container.RegisterType<TestClass03>();

            var testClass = resolver.Resolve<TestClass03>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_as_immutable_resolve_copy_of_container()
        {
            var container = new TypeContainer();
            container.RegisterType<TestClass03>();

            var resolver = container.AsImmutable();

            var testClass = resolver.Resolve<TestClass03>();

            Assert.NotNull(testClass, "testClass != null");

            container.RegisterType<TestClass04>();

            Assert.Throws<IoCException>(() => resolver.Resolve<TestClass04>());
        }

        private interface ITestInterface01
        {
            public string Value { get; }
        }

        private interface ITestInterface02<out T>
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

        private class TestClass04
        {
            private readonly TestClass03 _testClass03;

            public TestClass04(TestClass03 testClass03)
            {
                _testClass03 = testClass03;
            }

            public string Value => _testClass03.Value;
        }
    }
}
