using System.Collections;
using System.Reflection;
using Sight.Linq;

namespace Sight.IoC
{
    /// <summary>
    /// Helper methods for IoC
    /// </summary>
    public static class IoCHelpers
    {
        /// <summary>
        /// Try to resolve a delegate that can initialize a new instance of the registration
        /// </summary>
        /// <exception cref="IoCException"/>
        public static bool TryResolveActivator(ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions resolveOptions, out Func<object>? activator)
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

            if (type.IsGenericType)
            {
                var genericTypes = type.GetGenericArguments();
                if (genericTypes.Length == 1)
                {
                    var listType = typeof(List<>).MakeGenericType(genericTypes);
                    if (type.IsAssignableFrom(listType))
                    {
                        activator = () =>
                        {
                            var elementType = genericTypes[0];
                            var activators = ResolveAll(typeResolver, new RegistrationId(elementType!), resolveOptions);
                            var list = (IList)Activator.CreateInstance(listType)!;
                            foreach (var value in activators)
                            {
                                list.Add(value);
                            }

                            return list;
                        };

                        return true;
                    }
                }
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

        /// <inheritdoc cref="TryResolveActivator(ITypeResolver,RegistrationId,ResolveOptions,out Func{object}?)"/>
        public static bool TryResolveActivator(ITypeResolver typeResolver, Type type, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            return TryResolveActivator(typeResolver, new RegistrationId(type), resolveOptions, out activator);
        }

        /// <inheritdoc cref="TryResolveActivator(ITypeResolver,RegistrationId,ResolveOptions,out Func{object}?)"/>
        public static bool TryResolveActivator<T>(ITypeResolver typeResolver, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            return TryResolveActivator(typeResolver, new RegistrationId(typeof(T)), resolveOptions, out activator);
        }

        /// <summary>
        /// Try to initialize a new instance of the registration
        /// </summary>
        /// <exception cref="IoCException"/>
        public static bool TryResolveInstance(ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions resolveOptions, out object? instance)
        {
            if (TryResolveActivator(typeResolver, identifier, resolveOptions, out var activator))
            {
                instance = activator!();
                return true;
            }

            instance = null;
            return false;
        }

        /// <inheritdoc cref="TryResolveInstance(ITypeResolver,RegistrationId,ResolveOptions,out object?)"/>
        public static bool TryResolveInstance(ITypeResolver typeResolver, Type type, ResolveOptions resolveOptions, out object? instance)
        {
            return TryResolveInstance(typeResolver, new RegistrationId(type), resolveOptions, out instance);
        }

        /// <inheritdoc cref="TryResolveInstance(ITypeResolver,RegistrationId,ResolveOptions,out object?)"/>
        public static bool TryResolveInstance<T>(ITypeResolver typeResolver, ResolveOptions resolveOptions, out T? instance)
        {
            if (TryResolveInstance(typeResolver, typeof(T), resolveOptions, out var i))
            {
                instance = (T?)i;
                return true;
            }

            instance = default;
            return false;
        }

        /// <summary>
        /// Try to resolve a delegate that can invoke a method with dependency injection
        /// </summary>
        /// <exception cref="IoCException"/>
        public static bool TryResolveInvoker(ITypeResolver typeResolver, MethodInfo method, object? instance, ResolveOptions resolveOptions, out Func<object?>? invoke)
        {
            if (instance != null && (method.DeclaringType == null || !method.DeclaringType.IsInstanceOfType(instance)))
                throw new IoCException("Instance not assignable to declaring type of method");

            return TryCreateInvoker(typeResolver, method, resolveOptions, p => method.Invoke(instance, p), out invoke);
        }

        /// <inheritdoc cref="TryResolveInvoker(ITypeResolver,MethodInfo,object?,ResolveOptions,out Func{object?}?)"/>
        public static bool TryResolveInvoker(ITypeResolver typeResolver, MethodInfo method, ResolveOptions resolveOptions, out Func<object?>? invoke)
        {
            return TryResolveInvoker(typeResolver, method, null, resolveOptions, out invoke);
        }

        /// <summary>
        /// Initialize a new instance of the registration
        /// </summary>
        /// <exception cref="IoCException"/>
        public static object ResolveInstance(ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            if (!TryResolveInstance(typeResolver, identifier, resolveOptions, out var instance))
                throw new IoCException($"Cannot initialize a new instance of '{identifier.Name}'");

            return instance!;
        }

        /// <inheritdoc cref="ResolveInstance(ITypeResolver,RegistrationId,ResolveOptions)"/>
        public static object ResolveInstance(ITypeResolver typeResolver, Type type, ResolveOptions resolveOptions)
        {
            return ResolveInstance(typeResolver, new RegistrationId(type), resolveOptions);
        }

        /// <inheritdoc cref="ResolveInstance(ITypeResolver,RegistrationId,ResolveOptions)"/>
        public static T ResolveInstance<T>(ITypeResolver typeResolver, ResolveOptions resolveOptions)
        {
            return (T)ResolveInstance(typeResolver, typeof(T), resolveOptions);
        }

        /// <summary>
        /// Invoke a method with dependency injection
        /// </summary>
        /// <exception cref="IoCException"></exception>
        public static object? Invoke(ITypeResolver typeResolver, MethodInfo method, object? instance, ResolveOptions resolveOptions)
        {
            if (!TryResolveInvoker(typeResolver, method, instance, resolveOptions, out var invoker))
                throw new IoCException($"Cannot invoke '{method}' with current state");

            return invoker!();
        }

        /// <inheritdoc cref="Invoke(ITypeResolver,MethodInfo,object?,ResolveOptions)"/>
        public static object? Invoke(ITypeResolver typeResolver, MethodInfo method, ResolveOptions resolveOptions)
        {
            return Invoke(typeResolver, method, null, resolveOptions);
        }

        private static IReadOnlyList<object> ResolveAll(ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return typeResolver.SafeGetRegistrations().Where(x => IsRegistrationFor(typeResolver, x, identifier) && (!resolveOptions.IsOptional || IsRegistrationResolvable(x, resolveOptions))).Select(x => ResolveFromProvider(x.Type, resolveOptions, x.Resolver)).ToArray();
        }

        internal static bool IsRegistrationFor(ITypeResolver typeResolver, Registration registration, RegistrationId identifier)
        {
            return typeResolver.Predicate?.Invoke(registration, identifier) ?? registration.Type == identifier.Type && (identifier.Name == null || string.Equals(registration.Name, identifier.Name));
        }

        private static bool IsRegistrationFor(ITypeResolver typeResolver, Registration registration, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return IsRegistrationFor(typeResolver, registration, identifier) && IsRegistrationResolvable(registration, resolveOptions);
        }

        private static bool IsRegistrationResolvable(Registration registration, ResolveOptions resolveOptions)
        {
            return registration.Predicate == null || registration.Predicate(registration.Type, resolveOptions);
        }

        internal static bool TryCreateActivator(ITypeResolver typeResolver, Type type, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            if (!type.IsClass || type.IsAbstract)
            {
                activator = null;
                return false;
            }

            foreach (var constructor in type.GetConstructors())
            {
                if (TryCreateInvoker(typeResolver, constructor, resolveOptions, p => constructor.Invoke(p), out activator!))
                    return true;
            }

            activator = null;
            return false;
        }

        private static bool TryCreateInvoker(ITypeResolver typeResolver, MethodBase method, ResolveOptions resolveOptions, Func<object?[], object?> invoker, out Func<object?>? activator)
        {
            var parameters = new List<object?>();
            var parameterInfos = method.GetParameters();
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

                return invoker(parameters.ToArray());
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
