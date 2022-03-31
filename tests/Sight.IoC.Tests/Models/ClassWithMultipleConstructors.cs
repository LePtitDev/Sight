namespace Sight.IoC.Tests.Models
{
    internal class ClassWithMultipleConstructors
    {
        public ClassWithMultipleConstructors()
        {
            UsedConstructor = "ClassWithMultipleConstructors()";
        }

        public ClassWithMultipleConstructors(SimpleClassWithInterface simpleClass)
        {
            UsedConstructor = "ClassWithMultipleConstructors(SimpleClassWithInterface simpleClass)";
        }

        public ClassWithMultipleConstructors(ISimpleClassWithStringDependency simpleClass)
        {
            UsedConstructor = "ClassWithMultipleConstructors(ISimpleClassWithStringDependency simpleClass)";
        }

        public ClassWithMultipleConstructors(ISimpleClassWithInterface simpleClass1, ISimpleClassWithDependency simpleClass2 = null)
        {
            UsedConstructor = "ClassWithMultipleConstructors(ISimpleClassWithInterface simpleClass1, ISimpleClassWithDependency simpleClass2 = null)";
        }

        public ClassWithMultipleConstructors(ISimpleClassWithInterface simpleClass1, ISimpleClassWithStringDependency simpleClass2)
        {
            UsedConstructor = "ClassWithMultipleConstructors(ISimpleClassWithInterface simpleClass1, ISimpleClassWithStringDependency simpleClass2)";
        }

        public ClassWithMultipleConstructors(ISimpleClassWithInterface simpleClass1, ISimpleClassWithDependency simpleClass2, IGenericClassWithInterface<string> simpleClass3)
        {
            UsedConstructor = "ClassWithMultipleConstructors(ISimpleClassWithInterface simpleClass1, ISimpleClassWithDependency simpleClass2, IGenericClassWithInterface<string> simpleClass3)";
        }

        public string UsedConstructor { get; }
    }
}
