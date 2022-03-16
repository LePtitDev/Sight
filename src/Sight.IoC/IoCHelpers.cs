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

        private static IReadOnlyList<object> ResolveAll(ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return typeResolver.SafeGetRegistrations().Where(x => IsRegistrationFor(typeResolver, x, identifier, resolveOptions)).Select(x => ResolveFromProvider(x.Type, resolveOptions, x.Resolver)).ToArray();
        }

        internal static bool IsRegistrationFor(ITypeResolver typeResolver, Registration registration, RegistrationId identifier)
        {
            return typeResolver.Predicate?.Invoke(registration, identifier) ?? registration.Type == identifier.Type && (identifier.Name == null || string.Equals(registration.Name, identifier.Name));
        }

        private static bool IsRegistrationFor(ITypeResolver typeResolver, Registration registration, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return IsRegistrationFor(typeResolver, registration, identifier) && (registration.Predicate == null || registration.Predicate(registration.Type, resolveOptions));
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
