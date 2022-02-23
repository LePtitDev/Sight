namespace Sight.IoC
{
    public interface ITypeResolver
    {
        public IEnumerable<Registration> Registrations { get; }

        public bool IsRegistered(Type type);

        public bool IsResolvable(Type type, ResolveOptions resolveOptions);

        public object? Resolve(Type type, ResolveOptions resolveOptions);
    }
}
