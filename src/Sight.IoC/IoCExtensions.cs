namespace Sight.IoC
{
    public static class IoCExtensions
    {
        public static ITypeResolver AsImmutable(this ITypeResolver resolver)
        {
            return new TypeResolver(resolver.Registrations.ToArray(), syncRoot: null, isImmutable: true);
        }

        public static bool IsRegistered<T>(this ITypeResolver typeResolver)
        {
            return typeResolver.IsRegistered(typeof(T));
        }

        public static bool IsResolvable<T>(this ITypeResolver typeResolver, ResolveOptions resolveOptions)
        {
            return typeResolver.IsResolvable(typeof(T), resolveOptions);
        }

        public static T? Resolve<T>(this ITypeResolver typeResolver, ResolveOptions resolveOptions) where T : class
        {
            return (T?)typeResolver.Resolve(typeof(T), resolveOptions);
        }

        public static T Resolve<T>(this ITypeResolver typeResolver, params object[] additionalParameters)
        {
            return (T)Resolve(typeResolver, typeof(T), additionalParameters);
        }

        public static object Resolve(this ITypeResolver typeResolver, Type type, params object[] additionalParameters)
        {
            var resolveOptions = new ResolveOptions();
            resolveOptions.AdditionalParameters.AddRange(additionalParameters);
            return typeResolver.Resolve(type, resolveOptions)!;
        }

        public static void Register(this ITypeRegistrar typeRegistrar, Type type, object value)
        {
            if (!type.IsInstanceOfType(value))
                throw new IoCException($"Value is not instance of type '{type}' (Current: {value})");

            typeRegistrar.Register(type, (t, o) => value);
        }

        public static void Register<T>(this ITypeRegistrar typeRegistrar, T value) where T : class
        {
            Register(typeRegistrar, typeof(T), value);
        }

        public static void RegisterLazy(this ITypeRegistrar typeRegistrar, Type type, Func<object> provider)
        {
            var lazy = new Lazy<object>(provider);
            typeRegistrar.Register(type, (t, o) => lazy.Value);
        }

        public static void RegisterLazy<T>(this ITypeRegistrar typeRegistrar, Func<T> provider) where T : class
        {
            var lazy = new Lazy<T>(provider);
            typeRegistrar.Register(typeof(T), (t, o) => lazy.Value);
        }

        public static void RegisterGeneric(this ITypeRegistrar typeRegistrar, Type type, Func<Type[], object> provider)
        {
            if (!type.IsGenericTypeDefinition)
                throw new IoCException($"Type '{type}' is not a generic definition");

            typeRegistrar.Register(type, (t, _) => provider(t.GetGenericArguments()));
        }
    }
}
