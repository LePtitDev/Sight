namespace Sight.IoC
{
    public interface ITypeResolver
    {
        public bool IsRegistered(Type type);

        public bool IsResolvable(Type type, ResolveOptions resolveOptions);

        public object? Resolve(Type type, ResolveOptions resolveOptions);
    }
}
