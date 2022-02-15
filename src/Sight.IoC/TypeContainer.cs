using System.Collections;
using System.Reflection;
using Sight.Linq;

namespace Sight.IoC
{
    public class TypeContainer : ITypeContainer
    {
        private readonly List<(Type type, ResolveDelegate provider)> _dictionary = new List<(Type, ResolveDelegate)>();
        private readonly object _lock = new object();

        public bool IsRegistered(Type type)
        {
            lock (_lock)
            {
                return _dictionary.Any(x => x.type == type);
            }
        }

        public bool IsResolvable(Type type, ResolveOptions resolveOptions)
        {
            return TryResolveActivator(type, resolveOptions, out _);
        }

        public object? Resolve(Type type, ResolveOptions resolveOptions)
        {
            if (TryResolveActivator(type, resolveOptions, out var activator))
                return activator!();

            if (resolveOptions.IsOptional)
                return null;

            throw new IoCException($"Cannot resolve type '{type}'");
        }

        public void Register(Type type, ResolveDelegate provider)
        {
            lock (_lock)
            {
                _dictionary.Add((type, provider));
            }
        }

        private bool TryResolveActivator(Type type, ResolveOptions resolveOptions, out Func<object>? activator)
        {
            (Type type, ResolveDelegate provider)[] dictionary;
            lock (_lock)
            {
                dictionary = _dictionary.ToArray();
            }

            if (dictionary.TryGet(x => x.type == type, out var item))
            {
                activator = () => ResolveFromProvider(type, resolveOptions, item.provider);
                return true;
            }

            if (type.IsConstructedGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (dictionary.TryGet(x => x.type == genericType, out item))
                {
                    activator = () => ResolveFromProvider(type, resolveOptions, item.provider);
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
            lock (_lock)
            {
                return _dictionary.Where(x => x.type == type).Select(x => ResolveFromProvider(x.type, resolveOptions, x.provider)).ToArray();
            }
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
            (Type type, ResolveDelegate provider)[] dictionary;
            lock (_lock)
            {
                dictionary = _dictionary.ToArray();
            }

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

                if (dictionary.TryGet(x => parameter.ParameterType == x.type, out var item))
                {
                    parameters.Add(item.provider);
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
    }
}
