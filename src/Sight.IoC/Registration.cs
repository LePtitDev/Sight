namespace Sight.IoC
{
    public class Registration
    {
        public Registration(Type type, ResolveDelegate resolver)
        {
            Type = type;
            Resolver = resolver;
        }

        public Type Type { get; }

        public ResolveDelegate Resolver { get; }
    }
}
