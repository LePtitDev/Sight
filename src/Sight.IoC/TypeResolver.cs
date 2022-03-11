using System.Collections;
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
        private readonly Func<IEnumerable<Registration>> _registrationsFunc;
        private readonly bool _isImmutable;
        private readonly RegistrationPredicate _registrationPredicate;

        /// <summary>
        /// Initialize a new instance of <see cref="TypeResolver"/> class
        /// </summary>
        public TypeResolver(IEnumerable<Registration> registrations, object? syncRoot = null, bool isImmutable = false, RegistrationPredicate? registrationPredicate = null)
            : this(() => registrations, syncRoot, isImmutable, registrationPredicate)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TypeResolver"/> class
        /// </summary>
        public TypeResolver(Func<IEnumerable<Registration>> registrationsFunc, object? syncRoot = null, bool isImmutable = false, RegistrationPredicate? registrationPredicate = null)
        {
            _registrationsFunc = registrationsFunc;
            _isImmutable = isImmutable;
            _registrationPredicate = registrationPredicate ?? IsRegistrationForType;
            SyncRoot = syncRoot;
        }

        /// <inheritdoc />
        public IEnumerable<Registration> Registrations => _registrationsFunc();

        /// <inheritdoc />
        public object? SyncRoot { get; }

        /// <inheritdoc />
        public bool IsRegistered(Type type)
        {
            return EnsureSync(() => Registrations.Any(x => x.Type == type));
        }

        /// <inheritdoc />
        public bool IsResolvable(Type type, ResolveOptions resolveOptions)
        {
            return TryResolveActivator(type, resolveOptions, out _);
        }

        /// <inheritdoc />
        public object? Resolve(Type type, ResolveOptions resolveOptions)
        {
            if (TryResolveActivator(type, resolveOptions, out var activator))
                return activator!();

            if (resolveOptions.IsOptional)
                return null;

            throw new IoCException($"Cannot resolve type '{type}'");
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

        private bool TryResolveActivator(Type type, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            var dictionary = EnsureSync(GetImmutableRegistrations);
            if (dictionary.TryGet(x => _registrationPredicate(x, type, resolveOptions), out var item))
            {
                activator = () => ResolveFromProvider(type, resolveOptions, item.Resolver);
                return true;
            }

            if (type.IsConstructedGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (dictionary.TryGet(x => x.Type == genericType, out item))
                {
                    activator = () => ResolveFromProvider(type, resolveOptions, item.Resolver);
                    return true;
                }
            }

            if (!resolveOptions.AutoResolve)
            {
                activator = null;
                return false;
            }

            if (type.IsArray && type.GetArrayRank() == 1)
            {
                activator = () =>
                {
                    var items = ResolveAll(type, resolveOptions);
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
                    var activators = ResolveAll(type, resolveOptions);
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()))!;
                    foreach (var value in activators)
                    {
                        list.Add(value);
                    }

                    return list;
                };
                return true;
            }

            return TryCreateActivator(type, resolveOptions, out activator);
        }

        private IReadOnlyList<object> ResolveAll(Type type, ResolveOptions resolveOptions)
        {
            return EnsureSync(() => Registrations.Where(x => x.Type == type).Select(x => ResolveFromProvider(x.Type, resolveOptions, x.Resolver)).ToArray());
        }

        private bool TryCreateActivator(Type type, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            if (!type.IsClass || type.IsAbstract)
            {
                activator = null;
                return false;
            }

            foreach (var constructor in type.GetConstructors())
            {
                if (TryCreateActivator(constructor, resolveOptions, out activator))
                    return true;
            }

            activator = null;
            return false;
        }

        private bool TryCreateActivator(ConstructorInfo constructor, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            var parameters = new List<object?>();
            var dictionary = EnsureSync(GetImmutableRegistrations);

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

        private static bool IsRegistrationForType(Registration registration, Type type, ResolveOptions resolveOptions)
        {
            return registration.Type == type;
        }
    }
}
