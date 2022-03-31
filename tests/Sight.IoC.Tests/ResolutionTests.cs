using Sight.IoC.Tests.Models;

namespace Sight.IoC.Tests
{
    public class ResolutionTests
    {
        [Test]
        public void Test_can_resolve_registered_type()
        {
            var container = new TypeContainer();
            container.RegisterInstance<ISimpleClassWithStringDependency>(new SimpleClassWithStringDependency("test-01"));

            var testClass = container.Resolve<ISimpleClassWithStringDependency>();

            Assert.NotNull(testClass, "testClass != null");
            Assert.AreEqual("test-01", testClass.Value);
        }

        [Test]
        public void Test_instantiate_lazy_once()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new SimpleClassWithStringDependency(Guid.NewGuid().ToString("D")), lazy: true);

            var testClass1 = container.Resolve<SimpleClassWithStringDependency>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var testClass2 = container.Resolve<SimpleClassWithStringDependency>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreEqual(testClass1.Value, testClass2.Value);
        }

        [Test]
        public void Test_instantiate_not_lazy_each_time()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new SimpleClassWithStringDependency(Guid.NewGuid().ToString("D")), lazy: false);

            var testClass1 = container.Resolve<SimpleClassWithStringDependency>();

            Assert.NotNull(testClass1, "testClass1 != null");

            var testClass2 = container.Resolve<SimpleClassWithStringDependency>();

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreNotEqual(testClass1.Value, testClass2.Value);
        }

        [Test]
        public void Test_can_auto_resolve_class()
        {
            var container = new TypeContainer();
            var testClass = container.Resolve<SimpleClassWithStringDependency>(resolveOptions: new ResolveOptions
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
        public void Test_can_resolve_generic_type()
        {
            var called = false;
            var container = new TypeContainer();
            container.RegisterProvider(typeof(GenericClass<>), (type, _) =>
            {
                called = true;
                return Activator.CreateInstance(typeof(GenericClass<>).MakeGenericType(type.GetGenericArguments()));
            });

            var testClass = container.Resolve<GenericClass<string>>();

            Assert.NotNull(testClass, "testClass != null");
            Assert.IsTrue(called, "called");
        }

        [Test]
        public void Test_can_resolve_type_when_dependency_found()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClass>();
            container.RegisterType<SimpleClassWithDependency>();

            var testClass = container.Resolve<SimpleClassWithDependency>();

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_cannot_resolve_type_when_no_dependency_found()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClassWithDependency>();

            Assert.Throws<IoCException>(() => container.Resolve<SimpleClassWithDependency>());
        }

        [Test]
        public void Test_can_auto_resolve_type_when_dependency_found()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClass>();

            var testClass = container.Resolve<SimpleClassWithDependency>(resolveOptions: new ResolveOptions { AutoResolve = true });

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_do_not_throw_when_cannot_resolve_type()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClassWithDependency>();

            var testClass = container.Resolve<SimpleClassWithDependency>(resolveOptions: new ResolveOptions { IsOptional = true });

            Assert.Null(testClass, "testClass == null");
        }

        [Test]
        public void Test_resolve_named_service_correctly()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new SimpleClassWithStringDependency("test 01"), "TEST01");
            container.RegisterProvider((_, _) => new SimpleClassWithStringDependency("test 02"), "TEST02");

            var testClass1 = container.Resolve<SimpleClassWithStringDependency>("TEST01");

            Assert.NotNull(testClass1, "testClass1 != null");
            Assert.AreEqual("test 01", testClass1.Value);

            var testClass2 = container.Resolve<SimpleClassWithStringDependency>("TEST02");

            Assert.NotNull(testClass2, "testClass2 != null");
            Assert.AreEqual("test 02", testClass2.Value);
        }

        [Test]
        public void Test_can_resolve_multiple_services()
        {
            var container = new TypeContainer();
            container.RegisterProvider<ISimpleClassWithInterface>((_, _) => new SimpleClassWithInterface());
            container.RegisterProvider<ISimpleClassWithInterface>((_, _) => new SimpleClassWithSameInterface());

            var testClasses = container.Resolve<ISimpleClassWithInterface[]>();

            Assert.NotNull(testClasses);
            Assert.AreEqual(2, testClasses.Length, "testClasses.Length == 2");
        }

        [Test]
        public void Test_can_auto_wire_constructor_parameters()
        {
            var container = new TypeContainer();

            var testClass = container.Resolve<SimpleClassWithDependency>(resolveOptions: new ResolveOptions
            {
                AutoResolve = true,
                AutoWiring = true
            });

            Assert.NotNull(testClass, "testClass != null");
        }

        [Test]
        public void Test_can_resolve_all_registered_services()
        {
            var container = new TypeContainer();
            container.RegisterProvider<ISimpleClassWithInterface>((_, _) => new SimpleClassWithInterface());
            container.RegisterProvider<ISimpleClassWithInterface>((_, _) => new SimpleClassWithSameInterface());

            var testClasses = container.Resolve<IReadOnlyList<ISimpleClassWithInterface>>();

            Assert.NotNull(testClasses, "testClasses != null");
            Assert.AreEqual(2, testClasses.Count);
            Assert.IsTrue(testClasses.OfType<SimpleClassWithInterface>().Any(), "testClasses.OfType<SimpleClassWithInterface>().Any()");
            Assert.IsTrue(testClasses.OfType<SimpleClassWithSameInterface>().Any(), "testClasses.OfType<SimpleClassWithSameInterface>().Any()");
        }

        [Test]
        public void Test_cannot_resolve_registered_services_if_one_not_resolvable()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClassWithInterface, ISimpleClassWithInterface>();
            container.RegisterType<SimpleClassWithSameInterfaceAndDependency, ISimpleClassWithInterface>();

            Assert.Throws<IoCException>(() => container.Resolve<IReadOnlyList<ISimpleClassWithInterface>>());
        }

        [Test]
        public void Test_can_resolve_registered_services_if_one_not_resolvable_but_is_optional()
        {
            var container = new TypeContainer();
            container.RegisterType<SimpleClassWithInterface, ISimpleClassWithInterface>();
            container.RegisterType<SimpleClassWithSameInterfaceAndDependency, ISimpleClassWithInterface>();

            var testClasses = container.Resolve<IReadOnlyList<ISimpleClassWithInterface>>(resolveOptions: new ResolveOptions { IsOptional = true });

            Assert.NotNull(testClasses, "testClasses != null");
            Assert.AreEqual(1, testClasses.Count);
            Assert.IsTrue(testClasses[0] is SimpleClassWithInterface, "testClasses[0] is SimpleClassWithInterface");
        }

        [Test]
        public void Test_used_constructor_is_the_most_complex()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new SimpleClassWithInterface());
            container.RegisterProvider<ISimpleClassWithInterface>((_, _) => new SimpleClassWithInterface());
            container.RegisterProvider<ISimpleClassWithDependency>((_, _) => new SimpleClassWithDependency(new SimpleClass()));
            container.RegisterProvider<ISimpleClassWithStringDependency>((_, _) => new SimpleClassWithStringDependency(string.Empty));
            container.RegisterProvider<IGenericClassWithInterface<string>>((_, _) => new GenericClassWithInterface<string>());

            var testClass = container.Resolve<ClassWithMultipleConstructors>(resolveOptions: new ResolveOptions { AutoResolve = true });

            Assert.IsNotNull(testClass);
            Assert.AreEqual("ClassWithMultipleConstructors(ISimpleClassWithInterface simpleClass1, ISimpleClassWithDependency simpleClass2, IGenericClassWithInterface<string> simpleClass3)", testClass.UsedConstructor);
        }

        [Test]
        public void Test_used_constructor_is_with_less_default_values()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new SimpleClassWithInterface());
            container.RegisterProvider<ISimpleClassWithInterface>((_, _) => new SimpleClassWithInterface());
            container.RegisterProvider<ISimpleClassWithDependency>((_, _) => new SimpleClassWithDependency(new SimpleClass()));
            container.RegisterProvider<ISimpleClassWithStringDependency>((_, _) => new SimpleClassWithStringDependency(string.Empty));

            var testClass = container.Resolve<ClassWithMultipleConstructors>(resolveOptions: new ResolveOptions { AutoResolve = true });

            Assert.IsNotNull(testClass);
            Assert.AreEqual("ClassWithMultipleConstructors(ISimpleClassWithInterface simpleClass1, ISimpleClassWithStringDependency simpleClass2)", testClass.UsedConstructor);
        }

        [Test]
        public void Test_used_constructor_is_with_better_corresponding_resolve_options()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new SimpleClassWithInterface());
            container.RegisterProvider<ISimpleClassWithInterface>((_, _) => new SimpleClassWithInterface());
            container.RegisterProvider<ISimpleClassWithStringDependency>((_, _) => new SimpleClassWithStringDependency(string.Empty));

            var testClass = container.Resolve<ClassWithMultipleConstructors>(resolveOptions: new ResolveOptions
            {
                AutoResolve = true,
                TypedParameters =
                {
                    { typeof(ISimpleClassWithDependency), new SimpleClassWithDependency(new SimpleClass()) }
                }
            });

            Assert.IsNotNull(testClass);
            Assert.AreEqual("ClassWithMultipleConstructors(ISimpleClassWithInterface simpleClass1, ISimpleClassWithDependency simpleClass2 = null)", testClass.UsedConstructor);
        }

        [Test]
        public void Test_used_constructor_is_with_more_interface_services()
        {
            var container = new TypeContainer();
            container.RegisterProvider((_, _) => new SimpleClassWithInterface());
            container.RegisterProvider<ISimpleClassWithStringDependency>((_, _) => new SimpleClassWithStringDependency(string.Empty));

            var testClass = container.Resolve<ClassWithMultipleConstructors>(resolveOptions: new ResolveOptions { AutoResolve = true });

            Assert.IsNotNull(testClass);
            Assert.AreEqual("ClassWithMultipleConstructors(ISimpleClassWithStringDependency simpleClass)", testClass.UsedConstructor);
        }
    }
}
