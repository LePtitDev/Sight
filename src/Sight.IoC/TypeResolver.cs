﻿using System.Collections;
using System.Reflection;
using Sight.Linq;

namespace Sight.IoC
{
    /// <summary>
    /// Implementation of <see cref="ITypeResolver"/>
    /// </summary>
    public class TypeResolver : ITypeResolver
    {
        private static readonly Type[] ExcludedTypesForAutoResolve =
        {
            typeof(string)
        };

        private readonly Func<IEnumerable<Registration>> _provider;

        /// <summary>
        /// Initialize a new instance of <see cref="TypeResolver"/> class
        /// </summary>
        public TypeResolver(IEnumerable<Registration> registrations)
            : this(new CreateOptions(registrations))
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TypeResolver"/> class
        /// </summary>
        public TypeResolver(Func<IEnumerable<Registration>> registrationsFunc)
            : this(new CreateOptions(registrationsFunc))
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TypeResolver"/> class
        /// </summary>
        public TypeResolver(CreateOptions createOptions)
        {
            _provider = createOptions.Provider;
            Predicate = createOptions.Predicate;
            SyncRoot = createOptions.SyncRoot;
            Fallback = createOptions.Fallback;
        }

        /// <inheritdoc />
        public IEnumerable<Registration> Registrations => _provider();

        /// <inheritdoc />
        public object? SyncRoot { get; }

        /// <inheritdoc />
        public RegistrationPredicate? Predicate { get; }

        /// <inheritdoc />
        public ResolveFallback? Fallback { get; }

        /// <inheritdoc />
        public bool IsRegistered(RegistrationId identifier)
        {
            return EnsureSync(() => Registrations.Any(x => IsRegistrationFor(this, x, identifier)));
        }

        /// <inheritdoc />
        public bool TryResolveActivator(RegistrationId identifier, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            var type = identifier.Type;
            var dictionary = this.SafeGetRegistrations();
            if (dictionary.TryGet(x => IsRegistrationFor(this, x, identifier, resolveOptions), out var item))
            {
                activator = () => ResolveFromProvider(type, resolveOptions, item.Resolver);
                return true;
            }

            if (type.IsConstructedGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                var genericIdentifier = new RegistrationId(genericType) { Name = identifier.Name };
                if (dictionary.TryGet(x => IsRegistrationFor(this, x, genericIdentifier) && IsRegistrationResolvable(x, identifier, resolveOptions), out item))
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
                    var items = ResolveAll(this, new RegistrationId(elementType!), resolveOptions);
                    var arr = Array.CreateInstance(type.GetElementType()!, items.Count);
                    for (var i = 0; i < items.Count; i++)
                    {
                        arr.SetValue(items[i], i);
                    }

                    return arr;
                };
                return true;
            }

            if (type.IsConstructedGenericType)
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
                            var activators = ResolveAll(this, new RegistrationId(elementType), resolveOptions);
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

            if (Fallback != null && Fallback.Predicate(type, resolveOptions))
            {
                activator = () => Fallback.Resolver(type, resolveOptions);
                return true;
            }

            if (!resolveOptions.AutoResolve)
            {
                activator = null;
                return false;
            }

            return TryCreateActivator(this, type, resolveOptions, out activator);
        }

        /// <inheritdoc />
        public bool TryResolveInvoker(MethodInfo method, object? instance, ResolveOptions resolveOptions, out Func<object?>? invoker)
        {
            if (instance != null && (method.DeclaringType == null || !method.DeclaringType.IsInstanceOfType(instance)))
                throw new IoCException("Instance not assignable to declaring type of method");

            return TryCreateInvoker(this, method, resolveOptions, p => method.Invoke(instance, p), out invoker);
        }

        /// <summary>
        /// Ensure to lock collection if needed when calling action
        /// </summary>
        protected void EnsureSync(Action func)
        {
            _ = EnsureSync(() =>
            {
                func();
                return 0;
            });
        }

        /// <summary>
        /// Ensure to lock collection if needed when calling action
        /// </summary>
        protected T EnsureSync<T>(Func<T> func)
        {
            if (SyncRoot == null)
            {
                return func();
            }

            lock (SyncRoot)
            {
                return func();
            }
        }

        private static IReadOnlyList<object> ResolveAll(ITypeResolver typeResolver, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return typeResolver.SafeGetRegistrations().Where(x => IsRegistrationFor(typeResolver, x, identifier) && (!resolveOptions.IsOptional || IsRegistrationResolvable(x, identifier, resolveOptions))).Select(x => ResolveFromProvider(identifier.Type, resolveOptions, x.Resolver)).ToArray();
        }

        private static bool IsRegistrationFor(ITypeResolver typeResolver, Registration registration, RegistrationId identifier)
        {
            return typeResolver.Predicate?.Invoke(registration, identifier) ?? registration.Types.Contains(identifier.Type) && (identifier.Name == null || string.Equals(registration.Name, identifier.Name));
        }

        private static bool IsRegistrationFor(ITypeResolver typeResolver, Registration registration, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return IsRegistrationFor(typeResolver, registration, identifier) && IsRegistrationResolvable(registration, identifier, resolveOptions);
        }

        private static bool IsRegistrationResolvable(Registration registration, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return registration.Predicate == null || registration.Predicate(identifier.Type, resolveOptions);
        }

        private static object ResolveFromProvider(Type type, ResolveOptions resolveOptions, ResolveDelegate provider)
        {
            var value = provider(type, resolveOptions);
            if (!type.IsInstanceOfType(value))
                throw new IoCException($"Cannot resolve '{type}' from resolve provider");

            return value;
        }

        internal static bool TryCreateActivator(ITypeResolver typeResolver, Type type, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition || type.IsValueType || ExcludedTypesForAutoResolve.Contains(type))
            {
                activator = null;
                return false;
            }

            foreach (var constructor in OrderConstructors(type.GetConstructors(), resolveOptions))
            {
                if (TryCreateInvoker(typeResolver, constructor, resolveOptions, p => constructor.Invoke(p), out activator!))
                    return true;
            }

            activator = null;
            return false;

            // Order constructors by complexity, so we try to inject maximum of services
            static IEnumerable<ConstructorInfo> OrderConstructors(IReadOnlyCollection<ConstructorInfo> constructors, ResolveOptions resolveOptions)
            {
                if (constructors.Count < 2)
                    return constructors;

                return constructors.OrderByDescending(x =>
                {
                    var parameters = x.GetParameters();
                    if (parameters.Length == 0)
                        return 0;

                    // Compute a score constructor complexity
                    double notDefaultFactor = parameters.Length + 1;
                    var abstractFactor = notDefaultFactor * notDefaultFactor;
                    var score = parameters.Length +
                           parameters.Count(p => !p.HasDefaultValue) / notDefaultFactor +
                           parameters.Count(p => p.ParameterType.IsAbstract) / abstractFactor;

                    // Increase score with corresponding named parameters (high)
                    foreach (var parameter in resolveOptions.NamedParameters)
                    {
                        score += 100 * parameters.Count(p => p.Name == parameter.Key);
                    }

                    // Increase score with corresponding typed parameters (normal)
                    foreach (var parameter in resolveOptions.TypedParameters)
                    {
                        score += 10 * parameters.Count(p => p.ParameterType == parameter.Key);
                    }

                    // Increase score with assignable additional parameters (low)
                    foreach (var parameter in resolveOptions.AdditionalParameters)
                    {
                        score += parameters.Count(p => p.ParameterType.IsInstanceOfType(parameter));
                    }

                    return score;
                });
            }
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
                if (typeResolver.TryResolveActivator(registrationId, parameterResolveOptions, out var parameterActivator))
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

        /// <summary>
        /// Describes initialization options of <see cref="TypeResolver"/>
        /// </summary>
        public class CreateOptions
        {
            /// <summary>
            /// Initialize a new instance of <see cref="CreateOptions"/> class with a registrations collection
            /// </summary>
            public CreateOptions(IEnumerable<Registration> registrations)
                : this(() => registrations)
            {
            }

            /// <summary>
            /// Initialize a new instance of <see cref="CreateOptions"/> class with a registrations provider
            /// </summary>
            public CreateOptions(Func<IEnumerable<Registration>> provider)
            {
                Provider = provider;
            }

            /// <summary>
            /// Provider of registrations
            /// </summary>
            public Func<IEnumerable<Registration>> Provider { get; }

            /// <summary>
            /// Object that can be used to synchronize registration access
            /// </summary>
            public object? SyncRoot { get; set; }

            /// <summary>
            /// Registration predicate for service search
            /// </summary>
            public RegistrationPredicate? Predicate { get; set; }

            /// <summary>
            /// Resolution fallback when no registration found
            /// </summary>
            public ResolveFallback? Fallback { get; set; }
        }
    }
}
