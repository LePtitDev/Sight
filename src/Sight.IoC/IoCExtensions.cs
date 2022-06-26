using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

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
        /// Indicates if a service is resolvable
        /// </summary>
        /// <exception cref="IoCException"/>
        public static bool IsResolvable(this ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions? resolveOptions = null)
        {
            return typeResolver.TryResolveActivator(identifier, resolveOptions ?? ResolveOptions.Default, out _);
        }

        /// <inheritdoc cref="IsResolvable(ITypeResolver,RegistrationId,ResolveOptions?)"/>
        public static bool IsResolvable(this ITypeResolver typeResolver, Type type, string? name = null, ResolveOptions? resolveOptions = null)
        {
            return IsResolvable(typeResolver, new RegistrationId(type) { Name = name }, resolveOptions ?? ResolveOptions.Default);
        }

        /// <inheritdoc cref="IsResolvable(ITypeResolver,RegistrationId,ResolveOptions)"/>
        public static bool IsResolvable<T>(this ITypeResolver typeResolver, string? name = null, ResolveOptions? resolveOptions = null)
        {
            return IsResolvable(typeResolver, typeof(T), name, resolveOptions ?? ResolveOptions.Default);
        }

        /// <summary>
        /// Indicates if a method is invokable with dependency injection
        /// </summary>
        /// <exception cref="IoCException"/>
        public static bool IsInvokable(this ITypeResolver typeResolver, MethodInfo method, object? instance, ResolveOptions? resolveOptions = null)
        {
            return typeResolver.TryResolveInvoker(method, instance, resolveOptions ?? ResolveOptions.Default, out _);
        }

        /// <inheritdoc cref="IsInvokable(ITypeResolver,MethodInfo,object?,ResolveOptions?)"/>
        public static bool IsInvokable(this ITypeResolver typeResolver, MethodInfo method, ResolveOptions? resolveOptions = null)
        {
            return IsInvokable(typeResolver, method, null, resolveOptions);
        }

        /// <inheritdoc cref="ITypeResolver.TryResolveActivator"/>
        public static bool TryResolveActivator(this ITypeResolver typeResolver, Type type, ResolveOptions resolveOptions, [NotNullWhen(true)] out Func<object>? activator)
        {
            return typeResolver.TryResolveActivator(new RegistrationId(type), resolveOptions, out activator);
        }

        /// <inheritdoc cref="ITypeResolver.TryResolveActivator"/>
        public static bool TryResolveActivator<T>(this ITypeResolver typeResolver, ResolveOptions resolveOptions, [NotNullWhen(true)] out Func<object>? activator)
        {
            return TryResolveActivator(typeResolver, typeof(T), resolveOptions, out activator);
        }

        /// <inheritdoc cref="ITypeResolver.TryResolveInvoker"/>
        public static bool TryResolveInvoker(this ITypeResolver typeResolver, MethodInfo method, ResolveOptions resolveOptions, [NotNullWhen(true)] out Func<object?>? invoke)
        {
            return typeResolver.TryResolveInvoker(method, null, resolveOptions, out invoke);
        }

        /// <inheritdoc cref="ITypeResolver.TryResolveInvoker"/>
        public static bool TryResolveInvoker(this ITypeResolver typeResolver, Delegate method, ResolveOptions resolveOptions, [NotNullWhen(true)] out Func<object?>? invoke)
        {
            return typeResolver.TryResolveInvoker(method.Method, method.Target, resolveOptions, out invoke);
        }

        /// <summary>
        /// Try to initialize a new instance of the registration
        /// </summary>
        /// <exception cref="IoCException"/>
        public static bool TryResolve(this ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions resolveOptions, [NotNullWhen(true)] out object? instance)
        {
            if (typeResolver.TryResolveActivator(identifier, resolveOptions, out var activator))
            {
                instance = activator();
                return true;
            }

            instance = null;
            return false;
        }

        /// <inheritdoc cref="TryResolve(ITypeResolver,RegistrationId,ResolveOptions,out object?)"/>
        public static bool TryResolve(this ITypeResolver typeResolver, Type type, ResolveOptions resolveOptions, [NotNullWhen(true)] out object? instance)
        {
            return TryResolve(typeResolver, new RegistrationId(type), resolveOptions, out instance);
        }

        /// <inheritdoc cref="TryResolve(ITypeResolver,RegistrationId,ResolveOptions,out object?)"/>
        public static bool TryResolve<T>(this ITypeResolver typeResolver, ResolveOptions resolveOptions, [NotNullWhen(true)] out T? instance)
        {
            if (TryResolve(typeResolver, typeof(T), resolveOptions, out var i))
            {
                instance = (T)i;
                return true;
            }

            instance = default;
            return false;
        }

        /// <summary>
        /// Try to invoke a method with dependency injection
        /// </summary>
        /// <exception cref="IoCException"/>
        public static bool TryInvoke(this ITypeResolver typeResolver, MethodInfo method, object? instance, ResolveOptions resolveOptions, out object? returnValue)
        {
            if (typeResolver.TryResolveInvoker(method, instance, resolveOptions, out var invoker))
            {
                returnValue = invoker();
                return true;
            }

            returnValue = null;
            return false;
        }

        /// <inheritdoc cref="TryInvoke(ITypeResolver,MethodInfo,object?,ResolveOptions,out object?)"/>
        public static bool TryInvoke(this ITypeResolver typeResolver, MethodInfo method, ResolveOptions resolveOptions, out object? returnValue)
        {
            return TryInvoke(typeResolver, method, null, resolveOptions, out returnValue);
        }

        /// <inheritdoc cref="TryInvoke(ITypeResolver,MethodInfo,object?,ResolveOptions,out object?)"/>
        public static bool TryInvoke(this ITypeResolver typeResolver, Delegate method, ResolveOptions resolveOptions, out object? returnValue)
        {
            return TryInvoke(typeResolver, method.Method, method.Target, resolveOptions, out returnValue);
        }

        /// <summary>
        /// Resolve a typed service
        /// </summary>
        /// <exception cref="IoCException"/>
        public static object? Resolve(this ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions? resolveOptions = null)
        {
            resolveOptions ??= ResolveOptions.Default;
            if (typeResolver.TryResolveActivator(identifier, resolveOptions, out var activator))
                return activator();

            if (resolveOptions.IsOptional)
                return null;

            throw new IoCException($"Cannot resolve {identifier}");
        }

        /// <inheritdoc cref="Resolve(ITypeResolver,RegistrationId,ResolveOptions?)"/>
        public static object? Resolve(this ITypeResolver typeResolver, Type type, string? name = null, ResolveOptions? resolveOptions = null)
        {
            return typeResolver.Resolve(new RegistrationId(type) { Name = name }, resolveOptions ?? ResolveOptions.Default);
        }

        /// <inheritdoc cref="Resolve(ITypeResolver,RegistrationId,ResolveOptions?)"/>
        public static T? Resolve<T>(this ITypeResolver typeResolver, string? name = null, ResolveOptions? resolveOptions = null) where T : notnull
        {
            return (T?)Resolve(typeResolver, typeof(T), name, resolveOptions);
        }

        /// <summary>
        /// Invoke a method with dependency injection
        /// </summary>
        /// <exception cref="IoCException"></exception>
        public static object? Invoke(this ITypeResolver typeResolver, MethodInfo method, object? instance, ResolveOptions? resolveOptions = null)
        {
            if (!typeResolver.TryResolveInvoker(method, instance, resolveOptions ?? ResolveOptions.Default, out var invoker))
                throw new IoCException($"Cannot invoke '{method}' with current state");

            return invoker();
        }

        /// <inheritdoc cref="Invoke(ITypeResolver,MethodInfo,object?,ResolveOptions?)"/>
        public static object? Invoke(this ITypeResolver typeResolver, MethodInfo method, ResolveOptions? resolveOptions = null)
        {
            return Invoke(typeResolver, method, null, resolveOptions);
        }

        /// <inheritdoc cref="Invoke(ITypeResolver,MethodInfo,object?,ResolveOptions?)"/>
        public static object? Invoke(this ITypeResolver typeResolver, Delegate method, ResolveOptions? resolveOptions = null)
        {
            return Invoke(typeResolver, method.Method, method.Target, resolveOptions);
        }

        /// <summary>
        /// Invoke an async method with dependency injection
        /// </summary>
        /// <exception cref="IoCException"></exception>
        public static Task<object?> InvokeAsync(this ITypeResolver typeResolver, MethodInfo method, object? instance, ResolveOptions? resolveOptions = null)
        {
            if (!typeResolver.TryResolveInvoker(method, instance, resolveOptions ?? ResolveOptions.Default, out var invoker))
                throw new IoCException($"Cannot invoke '{method}' with current state");

            return (Task<object?>)invoker()!;
        }

        /// <inheritdoc cref="InvokeAsync(ITypeResolver,MethodInfo,object?,ResolveOptions?)"/>
        public static Task<object?> InvokeAsync(this ITypeResolver typeResolver, MethodInfo method, ResolveOptions? resolveOptions = null)
        {
            return InvokeAsync(typeResolver, method, null, resolveOptions);
        }

        /// <inheritdoc cref="InvokeAsync(ITypeResolver,MethodInfo,object?,ResolveOptions?)"/>
        public static Task<object?> InvokeAsync(this ITypeResolver typeResolver, Delegate method, ResolveOptions? resolveOptions = null)
        {
            return InvokeAsync(typeResolver, method.Method, method.Target, resolveOptions);
        }

        /// <summary>
        /// Register a service resolver
        /// </summary>
        /// <exception cref="IoCException"/>
        public static void RegisterType(this ITypeContainer typeContainer, Type type, Type? asType = null, string? name = null, bool lazy = false)
        {
            RegisterType(typeContainer, type, new[] { asType ?? type }, name, lazy);
        }

        /// <inheritdoc cref="RegisterType(ITypeContainer,Type,Type?,string?,bool)"/>
        public static void RegisterType(this ITypeContainer typeContainer, Type type, Type[] asTypes, string? name = null, bool lazy = false)
        {
            if (type.IsAbstract)
                throw new IoCException($"Cannot register abstract type '{type}'");

            foreach (var asType in asTypes)
            {
                if (type == asType)
                    continue;

                if (asType.IsGenericTypeDefinition)
                {
                    if (!type.IsGenericTypeDefinition)
                        throw new IoCException($"Type '{type}' cannot be registered as generic type '{asType}'");

                    if ((type.BaseType == null || !type.BaseType.IsGenericType || type.BaseType.GetGenericTypeDefinition() != asType) && type.GetInterfaces().Where(x => x.IsGenericType).All(x => x.GetGenericTypeDefinition() != asType))
                        throw new IoCException($"Type '{asType}' is not assignable from '{type}'");
                }
                else
                {
                    if (type.IsGenericTypeDefinition)
                        throw new IoCException($"Generic type '{type}' cannot be registered as type '{asType}'");

                    if (!asType.IsAssignableFrom(type))
                        throw new IoCException($"'{asType}' is not assignable from '{type}'");
                }
            }

            if (lazy)
            {
                object? instance = null;
                RegisterTypeImpl(typeContainer, type, asTypes, name, type.IsGenericTypeDefinition ? baseType => type.MakeGenericType(baseType.GetGenericArguments()) : _ => type, t => GetLazyInstance(type, t, instance) != null, t => GetLazyInstance(type, t, instance)!, (t, x) => AddOrUpdateInstance(type, t, ref instance, x));
            }
            else
            {
                RegisterTypeImpl(typeContainer, type, asTypes, name, type.IsGenericTypeDefinition ? baseType => type.MakeGenericType(baseType.GetGenericArguments()) : _ => type, _ => false, _ => default!, null);
            }

            static void RegisterTypeImpl(ITypeContainer typeContainer, Type type, Type[] asTypes, string? name, Func<Type, Type> typeResolver, Func<Type, bool> predicate, Func<Type, object> resolver, Action<Type, object>? onResolved)
            {
                typeContainer.Register(new Registration(asTypes, ResolveDelegate, name, ResolvePredicate));

                bool ResolvePredicate(Type t, ResolveOptions options)
                {
                    var resolvedType = typeResolver(t);
                    return predicate(resolvedType) || TypeResolver.TryCreateActivator(typeContainer, resolvedType, options, false, out _);
                }

                object ResolveDelegate(Type t, ResolveOptions options)
                {
                    var resolvedType = typeResolver(t);
                    if (predicate(resolvedType))
                        return resolver(resolvedType);

                    var instance = TypeResolver.TryCreateActivator(typeContainer, resolvedType, options, false, out var activator)
                        ? activator()
                        : throw new IoCException($"Cannot auto resolve '{type}'");
                    onResolved?.Invoke(resolvedType, instance);
                    return instance;
                }
            }

            static object? GetLazyInstance(Type type, Type resolvedType, object? instance)
            {
                if (instance == null)
                    return null;

                if (!type.IsGenericTypeDefinition)
                    return instance;

                return ((ConcurrentDictionary<string, object>)instance).TryGetValue(GetLazyKey(resolvedType), out var value) ? value : null;
            }

            static void AddOrUpdateInstance(Type type, Type resolvedType, ref object? instance, object value)
            {
                if (!type.IsGenericTypeDefinition)
                {
                    instance = value;
                }
                else
                {
                    var dict = instance ??= new ConcurrentDictionary<string, object>();
                    ((ConcurrentDictionary<string, object>)dict).AddOrUpdate(GetLazyKey(resolvedType), value, (_, _) => value);
                }
            }

            static string GetLazyKey(Type resolvedType)
            {
                return string.Join(";", resolvedType.GetGenericArguments().Select(x => x.FullName));
            }
        }

        /// <inheritdoc cref="RegisterType(ITypeContainer,Type,Type?,string?,bool)"/>
        public static void RegisterType<T>(this ITypeContainer typeContainer, string? name = null, bool lazy = false)
        {
            RegisterType(typeContainer, typeof(T), (Type?)null, name, lazy);
        }

        /// <inheritdoc cref="RegisterType(ITypeContainer,Type,Type?,string?,bool)"/>
        public static void RegisterType<T, TBase>(this ITypeContainer typeContainer, string? name = null, bool lazy = false) where T : TBase
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
                typeRegistrar.Register(new Registration(type, (t, o) => instance ??= resolver(t, o), name));
            }
            else
            {
                typeRegistrar.Register(new Registration(type, resolver, name));
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
    }
}
