namespace Sight.IoC.Tests.Models
{
    internal interface ISimpleClassWithDependency
    {
        public SimpleClass SimpleClass { get; }
    }

    internal class SimpleClassWithDependency : ISimpleClassWithDependency
    {
        public SimpleClassWithDependency(SimpleClass simpleClass)
        {
            SimpleClass = simpleClass;
        }

        public SimpleClass SimpleClass { get; }
    }
}
