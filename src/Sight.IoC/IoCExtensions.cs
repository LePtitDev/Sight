using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Sight.Linq;

namespace Sight.IoC
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
                SyncRoot = container.SyncRoot,
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
        public static T? Resolve<T>(this ITypeResolver typeResolver, string? name = null, ResolveOptions? resolveOptions = null) where T : notnull
        {
            return (T?)Resolve(typeResolver, typeof(T), name, resolveOptions);
        }

        /// <summary>
        /// Register a service resolver
        /// </summary>
        /// <exception cref="IoCException"/>
        public static void RegisterType(this ITypeContainer typeContainer, Type type, Type? asType = null, string? name = null, bool lazy = false)
        {
            RegisterType(typeContainer, type, (asType ?? type).ToEnumerable(), name, lazy);
        }

        /// <inheritdoc cref="RegisterType(ITypeContainer,Type,Type?,string?,bool)"/>
        public static void RegisterType(this ITypeContainer typeContainer, Type type, IEnumerable<Type> asTypes, string? name = null, bool lazy = false)
        {
            if (lazy)
            {
                object? instance = null;
                RegisterTypes(typeContainer, type, asTypes, name, () => instance != null, () => instance!, x => instance = x);
            }
            else
            {
                RegisterTypes(typeContainer, type, asTypes, name, () => false, () => default!, null);
            }

            static void RegisterTypes(ITypeContainer typeContainer, Type type, IEnumerable<Type> asTypes, string? name, Func<bool> predicate, Func<object> resolver, Action<object>? onResolved)
            {
                foreach (var asType in asTypes)
                {
                    if (!asType.IsAssignableFrom(type))
                    {
                        throw new IoCException($"'{asType}' is not assignable from '{type}'");
                    }

                    RegisterTypeImpl(typeContainer, type, asType, name, _ => type, predicate, resolver, onResolved);
                }
            }
        }

        /// <inheritdoc cref="RegisterType(ITypeContainer,Type,Type?,string?,bool)"/>
        public static void RegisterType<T>(this ITypeContainer typeContainer, string? name = null, bool lazy = false)
        {
            RegisterType(typeContainer, typeof(T), (Type?)null, name, lazy);
        }

        /// <inheritdoc cref="RegisterType(ITypeContainer,Type,Type?,string?,bool)"/>
        public static void RegisterType<TBase, T>(this ITypeContainer typeContainer, string? name = null, bool lazy = false)
        {
            RegisterType(typeContainer, typeof(T), typeof(TBase), name, lazy);
        }

        /// <summary>
        /// Register a service resolver
        /// </summary>
        public static void RegisterProvider(this ITypeRegistrar typeRegistrar, Type type, ResolveDelegate resolver, string? name = null, bool lazy = false)
        {
            if (lazy)
            {
                object? instance = null;
                typeRegistrar.Register(new Registration(type, (t, o) => instance ??= resolver(t, o)) { Name = name });
            }
            else
            {
                typeRegistrar.Register(new Registration(type, resolver) { Name = name });
            }
        }

        /// <inheritdoc cref="RegisterProvider"/>
        public static void RegisterProvider<T>(this ITypeRegistrar typeRegistrar, ResolveDelegate<T> resolver, string? name = null, bool lazy = false) where T : notnull
        {
            RegisterProvider(typeRegistrar, typeof(T), (t, o) => resolver(t, o), name, lazy);
        }

        /// <summary>
        /// Register a service instance
        /// </summary>
        /// <exception cref="IoCException"/>
        public static void RegisterInstance(this ITypeRegistrar typeRegistrar, Type type, object value, string? name = null)
        {
            if (!type.IsInstanceOfType(value))
                throw new IoCException($"Value is not instance of type '{type}' (Current: {value})");

            RegisterProvider(typeRegistrar, type, (_, _) => value, name);
        }

        /// <inheritdoc cref="RegisterInstance"/>
        public static void RegisterInstance<T>(this ITypeRegistrar typeRegistrar, T value, string? name = null) where T : notnull
        {
            RegisterInstance(typeRegistrar, typeof(T), value, name);
        }

        /// <summary>
        /// Register a service resolver with generic parameters
        /// </summary>
        /// <exception cref="IoCException"/>
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

                if (type.GetInterfaces().All(x => x.GetGenericTypeDefinition() != asType))
                    throw new IoCException($"Type '{asType}' is not assignable from '{type}'");
            }

            if (lazy)
            {
                object? instance = null;
                RegisterTypeImpl(typeContainer, type, asType, name, MakeType, () => instance != null, () => instance!, x => instance = x);
            }
            else
            {
                RegisterTypeImpl(typeContainer, type, asType, name, MakeType, () => false, () => default!, null);
            }

            Type MakeType(Type baseType) => type.MakeGenericType(baseType.GetGenericArguments());
        }

        /// <summary>
        /// Register a service resolver with generic parameters
        /// </summary>
        /// <exception cref="IoCException"/>
        public static void RegisterGenericProvider(this ITypeRegistrar typeRegistrar, Type type, Func<Type[], object> provider, string? name = null)
        {
            if (!type.IsGenericTypeDefinition)
                throw new IoCException($"Type '{type}' is not a generic definition");

            RegisterProvider(typeRegistrar, type, (t, _) => provider(t.GetGenericArguments()), name);
        }

        /// <summary>
        /// Get a copy of registrations by locking collection if required
        /// </summary>
        public static IReadOnlyList<Registration> SafeGetRegistrations(this ITypeResolver resolver)
        {
            IReadOnlyList<Registration> registrations;
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

            static IReadOnlyList<Registration> GetRegistrations(ITypeResolver resolver)
            {
                var registrations = resolver.Registrations;
                return registrations is Registration[] or ImmutableArray<Registration> or IImmutableList<Registration> 
                    ? (IReadOnlyList<Registration>)registrations
                    : registrations.ToArray();
            }
        }

        private static void RegisterTypeImpl(this ITypeContainer typeContainer, Type type, Type asType, string? name, Func<Type, Type> typeResolver, Func<bool> predicate, Func<object> resolver, Action<object>? onResolved)
        {
            typeContainer.Register(new Registration(asType, ResolveDelegate, ResolvePredicate) { Name = name });

            bool ResolvePredicate(Type t, ResolveOptions options)
            {
                return predicate() || TryCreateActivator(typeContainer, typeResolver(t), options, out _);
            }

            object ResolveDelegate(Type t, ResolveOptions options)
            {
                if (predicate())
                    return resolver();

                var instance = TryCreateActivator(typeContainer, typeResolver(t), options, out var activator)
                    ? activator!()
                    : throw new IoCException($"Cannot auto resolve '{type}'");
                onResolved?.Invoke(instance);
                return instance;
            }
        }

        /// <summary>
        /// Try to resolve a delegate that can initialize a new instance of the registration
        /// </summary>
        public static bool TryResolveActivator(this ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            var type = identifier.Type;
            var dictionary = typeResolver.SafeGetRegistrations();
            if (dictionary.TryGet(x => IsRegistrationFor(typeResolver, x, identifier, resolveOptions), out var item))
            {
                activator = () => ResolveFromProvider(type, resolveOptions, item.Resolver);
                return true;
            }

            if (type.IsConstructedGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                var genericIdentifier = new RegistrationId(genericType) { Name = identifier.Name };
                if (dictionary.TryGet(x => IsRegistrationFor(typeResolver, x, genericIdentifier, resolveOptions), out item))
                {
                    activator = () => ResolveFromProvider(type, resolveOptions, item.Resolver);
                    return true;
                }
            }

            if (type.IsArray && type.GetArrayRank() == 1)
            {
                activator = () =>
                {
                    var elementType = type.GetElementType();
                    var items = ResolveAll(typeResolver, new RegistrationId(elementType!), resolveOptions);
                    var arr = Array.CreateInstance(type.GetElementType()!, items.Count);
                    for (var i = 0; i < items.Count; i++)
                    {
                        arr.SetValue(items[i], i);
                    }

                    return arr;
                };
                return true;
            }

            if (type.IsGenericType && type.IsAssignableFrom(typeof(List<>)))
            {
                activator = () =>
                {
                    var elementType = type.GetGenericArguments()[0];
                    var activators = ResolveAll(typeResolver, new RegistrationId(elementType), resolveOptions);
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()))!;
                    foreach (var value in activators)
                    {
                        list.Add(value);
                    }

                    return list;
                };
                return true;
            }

            if (typeResolver.Fallback != null && typeResolver.Fallback.Predicate(type, resolveOptions))
            {
                activator = () => typeResolver.Fallback.Resolver(type, resolveOptions);
                return true;
            }

            if (!resolveOptions.AutoResolve)
            {
                activator = null;
                return false;
            }

            return TryCreateActivator(typeResolver, type, resolveOptions, out activator);
        }

        private static IReadOnlyList<object> ResolveAll(ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return typeResolver.SafeGetRegistrations().Where(x => IsRegistrationFor(typeResolver, x, identifier, resolveOptions)).Select(x => ResolveFromProvider(x.Type, resolveOptions, x.Resolver)).ToArray();
        }

        internal static bool IsRegistrationFor(this ITypeResolver typeResolver, Registration registration, RegistrationId identifier)
        {
            return typeResolver.Predicate?.Invoke(registration, identifier) ?? registration.Type == identifier.Type && (identifier.Name == null || string.Equals(registration.Name, identifier.Name));
        }

        private static bool IsRegistrationFor(this ITypeResolver typeResolver, Registration registration, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return IsRegistrationFor(typeResolver, registration, identifier) && (registration.Predicate == null || registration.Predicate(registration.Type, resolveOptions));
        }

        private static bool TryCreateActivator(ITypeResolver typeResolver, Type type, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            if (!type.IsClass || type.IsAbstract)
            {
                activator = null;
                return false;
            }

            foreach (var constructor in type.GetConstructors())
            {
                if (TryCreateActivator(typeResolver, constructor, resolveOptions, out activator))
                    return true;
            }

            activator = null;
            return false;
        }

        private static bool TryCreateActivator(ITypeResolver typeResolver, ConstructorInfo constructor, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            var parameters = new List<object?>();
            var parameterInfos = constructor.GetParameters();
            var parameterResolveOptions = new ResolveOptions
            {
                AutoResolve = resolveOptions.AutoWiring,
                AutoWiring = resolveOptions.AutoWiring
            };

            foreach (var parameter in parameterInfos)
            {
                if (resolveOptions.NamedParameters.TryGetValue(parameter.Name!, out var value))
                {
                    if (!parameter.ParameterType.IsInstanceOfType(value))
                    {
                        activator = null;
                        return false;
                    }

                    parameters.Add(value);
                    continue;
                }

                if (resolveOptions.TypedParameters.TryGetValue(parameter.ParameterType, out value))
                {
                    if (!parameter.ParameterType.IsInstanceOfType(value))
                    {
                        activator = null;
                        return false;
                    }

                    parameters.Add(value);
                    continue;
                }

                if (resolveOptions.AdditionalParameters.TryGet(x => parameter.ParameterType.IsInstanceOfType(x), out value))
                {
                    parameters.Add(value);
                    continue;
                }

                var registrationId = new RegistrationId(parameter.ParameterType);
                if (TryResolveActivator(typeResolver, registrationId, parameterResolveOptions, out var parameterActivator))
                {
                    parameters.Add(parameterActivator);
                }
                else if (parameter.DefaultValue != DBNull.Value)
                {
                    parameters.Add(parameter.DefaultValue);
                }
                else
                {
                    activator = null;
                    return false;
                }
            }

            activator = () =>
            {
                for (var i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i] is Func<object> provider)
                    {
                        parameters[i] = provider();
                    }
                }

                return constructor.Invoke(parameters.ToArray());
            };

            return true;
        }

        private static object ResolveFromProvider(Type type, ResolveOptions resolveOptions, ResolveDelegate provider)
        {
            var value = provider(type, resolveOptions);
            if (!type.IsInstanceOfType(value))
                throw new IoCException($"Cannot resolve '{type}' from resolve provider");

            return value;
        }
    }
}
