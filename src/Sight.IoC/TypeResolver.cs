﻿using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Sight.Linq;

namespace Sight.IoC
{
    /// <summary>
    /// Implementation of <see cref="ITypeResolver"/>
    /// </summary>
    public class TypeResolver : ITypeResolver
    {
        private readonly Func<IEnumerable<Registration>> _provider;
        private readonly bool _isImmutable;

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
            _isImmutable = createOptions.IsImmutable;
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
            return EnsureSync(() => Registrations.Any(x => IsRegistrationFor(x, identifier)));
        }

        /// <inheritdoc />
        public bool IsResolvable(RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return TryResolveActivator(identifier, resolveOptions, out _);
        }

        /// <inheritdoc />
        public object? Resolve(RegistrationId identifier, ResolveOptions resolveOptions)
        {
            if (TryResolveActivator(identifier, resolveOptions, out var activator))
                return activator!();

            if (resolveOptions.IsOptional)
                return null;

            throw new IoCException($"Cannot resolve {identifier}");
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

        private IReadOnlyCollection<Registration> GetImmutableRegistrations()
        {
            var registrations = Registrations;
            if (_isImmutable && registrations is IReadOnlyCollection<Registration> collection)
                return collection;

            return registrations switch
            {
                Registration[] array => array,
                ImmutableArray<Registration> immutableArray => immutableArray,
                IImmutableList<Registration> immutableList => immutableList,
                _ => registrations.ToArray()
            };
        }

        private bool TryResolveActivator(RegistrationId identifier, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            var type = identifier.Type;
            var dictionary = EnsureSync(GetImmutableRegistrations);
            if (dictionary.TryGet(x => IsRegistrationFor(x, identifier, resolveOptions), out var item))
            {
                activator = () => ResolveFromProvider(type, resolveOptions, item.Resolver);
                return true;
            }

            if (type.IsConstructedGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                var genericIdentifier = new RegistrationId(genericType) { Name = identifier.Name };
                if (dictionary.TryGet(x => IsRegistrationFor(x, genericIdentifier, resolveOptions), out item))
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
                    var items = ResolveAll(new RegistrationId(elementType!), resolveOptions);
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
                    var activators = ResolveAll(new RegistrationId(elementType), resolveOptions);
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()))!;
                    foreach (var value in activators)
                    {
                        list.Add(value);
                    }

                    return list;
                };
                return true;
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

            return TryCreateActivator(dictionary, type, resolveOptions, out activator);
        }

        private IReadOnlyList<object> ResolveAll(RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return EnsureSync(() => Registrations.Where(x => IsRegistrationFor(x, identifier, resolveOptions)).Select(x => ResolveFromProvider(x.Type, resolveOptions, x.Resolver)).ToArray());
        }

        private bool IsRegistrationFor(Registration registration, RegistrationId identifier)
        {
            return Predicate?.Invoke(registration, identifier) ?? registration.Type == identifier.Type && (identifier.Name == null || string.Equals(registration.Name, identifier.Name));
        }

        private bool IsRegistrationFor(Registration registration, RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return IsRegistrationFor(registration, identifier) && (registration.Predicate == null || registration.Predicate(registration.Type, resolveOptions));
        }

        internal static bool TryCreateActivator(IReadOnlyCollection<Registration> dictionary, Type type, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            if (!type.IsClass || type.IsAbstract)
            {
                activator = null;
                return false;
            }

            foreach (var constructor in type.GetConstructors())
            {
                if (TryCreateActivator(dictionary, constructor, resolveOptions, out activator))
                    return true;
            }

            activator = null;
            return false;
        }

        private static bool TryCreateActivator(IReadOnlyCollection<Registration> dictionary, ConstructorInfo constructor, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            var parameters = new List<object?>();
            var parameterInfos = constructor.GetParameters();
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

                if (resolveOptions.AdditionalParameters.TryGet(x => parameter.ParameterType.IsInstanceOfType(x), out value))
                {
                    parameters.Add(value);
                    continue;
                }

                if (dictionary.TryGet(x => parameter.ParameterType == x.Type, out var item))
                {
                    parameters.Add(item.Resolver);
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
                    if (parameters[i] is ResolveDelegate provider)
                    {
                        parameters[i] = ResolveFromProvider(parameterInfos[i].ParameterType, resolveOptions, provider);
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
            /// Indicates if registrations are immutable
            /// </summary>
            public bool IsImmutable { get; set; }

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
