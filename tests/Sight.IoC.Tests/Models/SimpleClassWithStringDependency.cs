namespace Sight.IoC.Tests.Models
{
    internal interface ISimpleClassWithStringDependency
    {
        public string Value { get; }
    }

    internal class SimpleClassWithStringDependency : ISimpleClassWithStringDependency

    {
    public SimpleClassWithStringDependency(string value)
    {
        Value = value;
    }

    public string Value { get; }
    }
}
