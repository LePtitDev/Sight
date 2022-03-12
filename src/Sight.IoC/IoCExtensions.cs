﻿namespace Sight.IoC
{
    /// <summary>
    /// Extension methods for <see cref="ITypeResolver"/>, <see cref="ITypeRegistrar"/> and <see cref="ITypeContainer"/>
    /// </summary>
    public static class IoCExtensions
    {
        /// <summary>
        /// Initialize a <see cref="ITypeResolver"/> with from container
        /// </summary>
        public static ITypeResolver AsReadOnly(this ITypeContainer container)
        {
            return new TypeResolver(new TypeResolver.CreateOptions(() => container.Registrations)
            {
                IsImmutable = true,
                Predicate = container.Predicate,
                Fallback = container.Fallback
            });
        }

        /// <summary>
        /// Initialize a <see cref="ITypeResolver"/> with an immutable copy of registrations from input resolver
        /// </summary>
        public static ITypeResolver AsImmutable(this ITypeResolver resolver)
        {
            var registrations = SafeGetRegistrations(resolver);
            return new TypeResolver(new TypeResolver.CreateOptions(registrations)
            {
                IsImmutable = true,
                Predicate = resolver.Predicate,
                Fallback = resolver.Fallback
            });
        }

        /// <summary>
        /// Indicates if a typed service is registered in the resolver
        /// </summary>
        public static bool IsRegistered(this ITypeResolver typeResolver, Type type, string? name)
        {
            return typeResolver.IsRegistered(new RegistrationId(type) { Name = name });
        }

        /// <inheritdoc cref="IsRegistered"/>
        public static bool IsRegistered<T>(this ITypeResolver typeResolver, string? name = null)
        {
            return IsRegistered(typeResolver, typeof(T), name);
        }

        /// <summary>
        /// Indicates if a typed service is resolvable with some options
        /// </summary>
        /// <exception cref="IoCException"/>
        public static bool IsResolvable(this ITypeResolver typeResolver, Type type, string? name = null, ResolveOptions? resolveOptions = null)
        {
            return typeResolver.IsResolvable(new RegistrationId(type) { Name = name }, resolveOptions ?? ResolveOptions.Empty);
        }

        /// <inheritdoc cref="IsResolvable"/>
        public static bool IsResolvable<T>(this ITypeResolver typeResolver, string? name = null, ResolveOptions? resolveOptions = null)
        {
            return IsResolvable(typeResolver, typeof(T), name, resolveOptions ?? ResolveOptions.Empty);
        }

        /// <summary>
        /// Resolve a typed service
        /// </summary>
        /// <exception cref="IoCException"/>
        public static object? Resolve(this ITypeResolver typeResolver, Type type, string? name = null, ResolveOptions? resolveOptions = null)
        {
            return typeResolver.Resolve(new RegistrationId(type) { Name = name }, resolveOptions ?? ResolveOptions.Empty);
        }

        /// <inheritdoc cref="Resolve(ITypeResolver,Type,string?,ResolveOptions?)"/>
        public static T? Resolve<T>(this ITypeResolver typeResolver, string? name = null, ResolveOptions? resolveOptions = null) where T : class
        {
            return (T?)Resolve(typeResolver, typeof(T), name, resolveOptions);
        }

        /// <summary>
        /// Register a service resolver
        /// </summary>
        public static void RegisterType(this ITypeContainer typeContainer, Type type, Type? asType = null, string? name = null, bool lazy = false)
        {
            if (asType == null)
            {
                asType = type;
            }
            else if (!asType.IsAssignableFrom(type))
            {
                throw new IoCException($"'{asType}' is not assignable from '{type}'");
            }

            if (lazy)
            {
                object? instance = null;
                RegisterTypeImpl(typeContainer, type, asType, name, _ => type, () => instance != null, () => instance!);
            }
            else
            {
                RegisterTypeImpl(typeContainer, type, asType, name, _ => type, () => false, () => default!);
            }
        }

        /// <inheritdoc cref="RegisterType"/>
        public static void RegisterType<T>(this ITypeContainer typeContainer, string? name = null, bool lazy = false)
        {
            RegisterType(typeContainer, typeof(T), null, name, lazy);
        }

        /// <inheritdoc cref="RegisterType"/>
        public static void RegisterType<TBase, T>(this ITypeContainer typeContainer, string? name = null, bool lazy = false)
        {
            RegisterType(typeContainer, typeof(T), typeof(TBase), name, lazy);
        }

        /// <summary>
        /// Register a service resolver
        /// </summary>
        public static void RegisterProvider(this ITypeRegistrar typeRegistrar, Type type, ResolveDelegate resolver, string? name = null)
        {
            typeRegistrar.Register(new Registration(type, resolver) { Name = name });
        }

        /// <summary>
        /// Register a service instance
        /// </summary>
        public static void RegisterInstance(this ITypeRegistrar typeRegistrar, Type type, object value, string? name = null)
        {
            if (!type.IsInstanceOfType(value))
                throw new IoCException($"Value is not instance of type '{type}' (Current: {value})");

            RegisterProvider(typeRegistrar, type, (_, _) => value, name);
        }

        /// <inheritdoc cref="RegisterInstance"/>
        public static void RegisterInstance<T>(this ITypeRegistrar typeRegistrar, T value, string? name = null) where T : class
        {
            RegisterInstance(typeRegistrar, typeof(T), value, name);
        }

        /// <summary>
        /// Register a service resolver with instance storing
        /// </summary>
        public static void RegisterLazy(this ITypeRegistrar typeRegistrar, Type type, Func<object> provider, string? name = null)
        {
            var lazy = new Lazy<object>(provider);
            RegisterProvider(typeRegistrar, type, (_, _) => lazy.Value, name);
        }

        /// <inheritdoc cref="RegisterLazy"/>
        public static void RegisterLazy<T>(this ITypeRegistrar typeRegistrar, Func<T> provider, string? name = null) where T : class
        {
            var lazy = new Lazy<T>(provider);
            RegisterProvider(typeRegistrar, typeof(T), (_, _) => lazy.Value, name);
        }

        /// <summary>
        /// Register a service resolver with generic parameters
        /// </summary>
        public static void RegisterGenericType(this ITypeContainer typeContainer, Type type, Type? asType = null, string? name = null, bool lazy = false)
        {
            if (!type.IsGenericTypeDefinition)
                throw new IoCException($"Type '{type}' is not a generic definition");

            if (asType == null)
            {
                asType = type;
            }
            else
            {
                if (!asType.IsGenericTypeDefinition)
                    throw new IoCException($"Type '{asType}' is not a generic definition");

                if (!type.GetInterfaces().Any(x => x.GetGenericTypeDefinition() == asType))
                    throw new IoCException($"Type '{asType}' is not assignable from '{type}'");
            }

            if (lazy)
            {
                object? instance = null;
                RegisterTypeImpl(typeContainer, type, asType, name, MakeType, () => instance != null, () => instance!);
            }
            else
            {
                RegisterTypeImpl(typeContainer, type, asType, name, MakeType, () => false, () => default!);
            }

            Type MakeType(Type baseType) => type.MakeGenericType(baseType.GetGenericArguments());
        }

        /// <summary>
        /// Register a service resolver with generic parameters
        /// </summary>
        public static void RegisterGenericProvider(this ITypeRegistrar typeRegistrar, Type type, Func<Type[], object> provider, string? name = null)
        {
            if (!type.IsGenericTypeDefinition)
                throw new IoCException($"Type '{type}' is not a generic definition");

            RegisterProvider(typeRegistrar, type, (t, _) => provider(t.GetGenericArguments()), name);
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

        private static void RegisterTypeImpl(this ITypeContainer typeContainer, Type type, Type asType, string? name, Func<Type, Type> typeResolver, Func<bool> predicate, Func<object> resolver)
        {
            typeContainer.Register(new Registration(asType, ResolveDelegate, ResolvePredicate) { Name = name });

            bool ResolvePredicate(Type t, ResolveOptions options)
            {
                return predicate() || TypeResolver.TryCreateActivator(SafeGetRegistrations(typeContainer), typeResolver(t), options, out _);
            }

            object ResolveDelegate(Type t, ResolveOptions options)
            {
                if (predicate())
                    return resolver();

                return TypeResolver.TryCreateActivator(SafeGetRegistrations(typeContainer), typeResolver(t), options, out var activator)
                    ? activator!()
                    : throw new IoCException($"Cannot auto resolve '{type}'");
            }
        }
    }
}
