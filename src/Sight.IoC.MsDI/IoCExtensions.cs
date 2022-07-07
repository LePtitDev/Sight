using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Sight.IoC.MsDI
{
    /// <summary>
    /// Extension methods to support Microsoft Dependency Injection
    /// </summary>
    public static class IoCExtensions
    {
        /// <summary>
        /// Register services from <see cref="IServiceCollection"/>. A <see cref="IServiceProvider"/> can be required to resolve some services
        /// </summary>
        public static void RegisterServicesCollection(this ITypeContainer typeContainer, IServiceCollection serviceCollection)
        {
            foreach (var descriptor in serviceCollection)
            {
                if (descriptor.ImplementationFactory != null)
                {
                    typeContainer.Register(new Registration(descriptor.ServiceType, 
                        resolver: (_, o) => descriptor.ImplementationFactory(ResolveServiceProvider(typeContainer, o)),
                        predicate: (_, o) => IsServiceProviderResolvable(typeContainer, o)));
                }
                else if (descriptor.ImplementationType != null)
                {
                    typeContainer.RegisterType(descriptor.ImplementationType, descriptor.ServiceType);
                }
                else if (descriptor.ImplementationInstance != null)
                {
                    typeContainer.RegisterInstance(descriptor.ServiceType, descriptor.ImplementationInstance);
                }
            }

            static IServiceProvider ResolveServiceProvider(ITypeResolver typeContainer, ResolveOptions resolveOptions)
            {
                return resolveOptions.AdditionalParameters.OfType<IServiceProvider>().FirstOrDefault() ??
                       resolveOptions.TypedParameters.Select(x => x.Value).OfType<IServiceProvider>().FirstOrDefault() ??
                       resolveOptions.NamedParameters.Select(x => x.Value).OfType<IServiceProvider>().FirstOrDefault() ??
                       typeContainer.Resolve<IServiceProvider>()!;
            }

            static bool IsServiceProviderResolvable(ITypeResolver typeContainer, ResolveOptions resolveOptions)
            {
                return resolveOptions.AdditionalParameters.OfType<IServiceProvider>().Any() ||
                       resolveOptions.TypedParameters.Select(x => x.Value).OfType<IServiceProvider>().Any() ||
                       resolveOptions.NamedParameters.Select(x => x.Value).OfType<IServiceProvider>().Any() ||
                       typeContainer.IsResolvable<IServiceProvider>();
            }
        }

        /// <summary>
        /// Get a <see cref="IServiceProvider"/> wrapped to a <see cref="ITypeResolver"/>
        /// </summary>
        public static IServiceProvider GetServiceProvider(this ITypeResolver typeResolver)
        {
            return new ServiceProvider(typeResolver);
        }

        /// <summary>
        /// Build a <see cref="IServiceCollection"/> from services registered in a <see cref="ITypeResolver"/>
        /// </summary>
        public static IServiceCollection BuildServicesCollection(this ITypeResolver typeResolver)
        {
            var serviceCollection = new ServiceCollection();
            foreach (var registration in typeResolver.SafeGetRegistrations())
            {
                foreach (var type in registration.Types)
                {
                    serviceCollection.AddTransient(type, _ => registration.Resolver(type, ResolveOptions.Default));
                }
            }

            return serviceCollection;
        }

        /// <summary>
        /// Build a <see cref="IServiceProvider"/> from services registered in a <see cref="ITypeResolver"/>
        /// </summary>
        public static IServiceProvider BuildServiceProvider(this ITypeResolver typeResolver)
        {
            return BuildServicesCollection(typeResolver).BuildServiceProvider();
        }

        private class ServiceProvider : IServiceProvider
        {
            private readonly ITypeResolver _typeResolver;

            public ServiceProvider(ITypeResolver typeResolver)
            {
                _typeResolver = typeResolver;
            }

            public object? GetService(Type serviceType)
            {
                return _typeResolver.Resolve(serviceType, resolveOptions: new ResolveOptions { IsOptional = true });
            }
        }
    }
}
