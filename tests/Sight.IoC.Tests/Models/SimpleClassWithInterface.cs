namespace Sight.IoC.Tests.Models
{
    internal interface ISimpleClassWithInterface
    {
    }

    internal class SimpleClassWithInterface : ISimpleClassWithInterface
    {
    }

    internal class SimpleClassWithSameInterface : ISimpleClassWithInterface
    {
    }

    internal class SimpleClassWithSameInterfaceAndDependency : ISimpleClassWithInterface
    {
        public SimpleClassWithSameInterfaceAndDependency(SimpleClass simpleClass)
        {
            SimpleClass = simpleClass;
        }

        public SimpleClass SimpleClass { get; }
    }
}
