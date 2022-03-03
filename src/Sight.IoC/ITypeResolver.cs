namespace Sight.IoC
{
    /// <summary>
    /// Describe a container that can resolve services
    /// </summary>
    public interface ITypeResolver
    {
        /// <summary>
        /// Registrations
        /// </summary>
        public IEnumerable<Registration> Registrations { get; }

        /// <summary>
        /// Indicates if a service is registered
        /// </summary>
        public bool IsRegistered(Type type);

        /// <summary>
        /// Indicates if a service is resolvable
        /// </summary>
        /// <exception cref="IoCException"/>
        public bool IsResolvable(Type type, ResolveOptions resolveOptions);

        /// <summary>
        /// Resolve a service
        /// </summary>
        /// <exception cref="IoCException"/>
        public object? Resolve(Type type, ResolveOptions resolveOptions);
    }
}
