namespace Sight.IoC
{
    /// <summary>
    /// Extension methods for <see cref="ITypeResolver"/>, <see cref="ITypeRegistrar"/> and <see cref="ITypeContainer"/>
    /// </summary>
    public static class IoCExtensions
    {
        /// <summary>
        /// Initialize a <see cref="ITypeResolver"/> with an immutable copy of registrations from input resolver
        /// </summary>
        public static ITypeResolver AsImmutable(this ITypeResolver resolver)
        {
            var registrations = SafeGetRegistrations(resolver);
            return new TypeResolver(new TypeResolver.CreateOptions(registrations)
            {
                IsImmutable = true,
                Predicate = resolver.Predicate
            });
        }

        /// <summary>
        /// Indicates if a typed service is registered in the resolver
        /// </summary>
        public static bool IsRegistered<T>(this ITypeResolver typeResolver)
        {
            return typeResolver.IsRegistered(typeof(T));
        }

        /// <summary>
        /// Indicates if a typed service is resolvable with some options
        /// </summary>
        /// <exception cref="IoCException"/>
        public static bool IsResolvable<T>(this ITypeResolver typeResolver, ResolveOptions resolveOptions)
        {
            return typeResolver.IsResolvable(typeof(T), resolveOptions);
        }

        /// <summary>
        /// Resolve a typed service with some options
        /// </summary>
        /// <exception cref="IoCException"/>
        public static T? Resolve<T>(this ITypeResolver typeResolver, ResolveOptions resolveOptions) where T : class
        {
            return (T?)typeResolver.Resolve(typeof(T), resolveOptions);
        }

        /// <summary>
        /// Resolve a typed service
        /// </summary>
        /// <exception cref="IoCException"/>
        public static T Resolve<T>(this ITypeResolver typeResolver, params object[] additionalParameters)
        {
            return (T)Resolve(typeResolver, typeof(T), additionalParameters);
        }

        /// <summary>
        /// Resolve a typed service
        /// </summary>
        /// <exception cref="IoCException"/>
        public static object Resolve(this ITypeResolver typeResolver, Type type, params object[] additionalParameters)
        {
            var resolveOptions = new ResolveOptions();
            resolveOptions.AdditionalParameters.AddRange(additionalParameters);
            return typeResolver.Resolve(type, resolveOptions)!;
        }

        /// <summary>
        /// Register a service resolver
        /// </summary>
        public static void Register(this ITypeRegistrar typeRegistrar, Type type, ResolveDelegate resolver)
        {
            typeRegistrar.Register(new Registration(type, resolver));
        }

        /// <summary>
        /// Register a service instance
        /// </summary>
        public static void Register(this ITypeRegistrar typeRegistrar, Type type, object value)
        {
            if (!type.IsInstanceOfType(value))
                throw new IoCException($"Value is not instance of type '{type}' (Current: {value})");

            typeRegistrar.Register(type, (_, _) => value);
        }

        /// <summary>
        /// Register a service instance
        /// </summary>
        public static void Register<T>(this ITypeRegistrar typeRegistrar, T value) where T : class
        {
            Register(typeRegistrar, typeof(T), value);
        }

        /// <summary>
        /// Register a service resolver with instance storing
        /// </summary>
        public static void RegisterLazy(this ITypeRegistrar typeRegistrar, Type type, Func<object> provider)
        {
            var lazy = new Lazy<object>(provider);
            typeRegistrar.Register(type, (_, _) => lazy.Value);
        }

        /// <summary>
        /// Register a service resolver with instance storing
        /// </summary>
        public static void RegisterLazy<T>(this ITypeRegistrar typeRegistrar, Func<T> provider) where T : class
        {
            var lazy = new Lazy<T>(provider);
            typeRegistrar.Register(typeof(T), (_, _) => lazy.Value);
        }

        /// <summary>
        /// Register a service resolver with generic parameters
        /// </summary>
        public static void RegisterGeneric(this ITypeRegistrar typeRegistrar, Type type, Func<Type[], object> provider)
        {
            if (!type.IsGenericTypeDefinition)
                throw new IoCException($"Type '{type}' is not a generic definition");

            typeRegistrar.Register(type, (t, _) => provider(t.GetGenericArguments()));
        }

        /// <summary>
        /// Get a copy of registrations by locking collection if required
        /// </summary>
        public static Registration[] SafeGetRegistrations(ITypeResolver resolver)
        {
            Registration[] registrations;
            if (resolver.SyncRoot == null)
            {
                registrations = GetRegistrations(resolver);
            }
            else
            {
                lock (resolver.SyncRoot)
                {
                    registrations = GetRegistrations(resolver);
                }
            }

            return registrations;

            static Registration[] GetRegistrations(ITypeResolver resolver)
            {
                return resolver.Registrations.ToArray();
            }
        }
    }
}
