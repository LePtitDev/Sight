using System.Reflection;

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
        /// Object that can be used to synchronize registration access
        /// </summary>
        public object? SyncRoot { get; }

        /// <summary>
        /// Registration predicate for service search
        /// </summary>
        public RegistrationPredicate? Predicate { get; }

        /// <summary>
        /// Resolution fallback when no registration found
        /// </summary>
        public ResolveFallback? Fallback { get; }

        /// <summary>
        /// Indicates if a service is registered
        /// </summary>
        public bool IsRegistered(RegistrationId identifier);

        /// <summary>
        /// Try to resolve a delegate that can initialize a new instance of the registration
        /// </summary>
        /// <exception cref="IoCException"/>
        public bool TryResolveActivator(RegistrationId identifier, ResolveOptions resolveOptions, out Func<object>? activator);

        /// <summary>
        /// Try to resolve a delegate that can invoke a method with dependency injection
        /// </summary>
        /// <exception cref="IoCException"/>
        public bool TryResolveInvoker(MethodInfo method, object? instance, ResolveOptions resolveOptions, out Func<object?>? invoker);
    }
}
