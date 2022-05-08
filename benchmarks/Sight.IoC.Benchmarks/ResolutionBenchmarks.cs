using Autofac;
using Autofac.Core.Activators.Reflection;
using Autofac.Features.ResolveAnything;
using DryIoc;
using Microsoft.Extensions.DependencyInjection;

namespace Sight.IoC.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    public class ResolutionBenchmarks
    {
        [Benchmark]
        public void Sight()
        {
            var container = new TypeContainer();
            container.RegisterType<Dependency01, IDependency01>();

            var dependency = container.Resolve<Dependency03>(resolveOptions: new ResolveOptions
            {
                AutoResolve = true,
                AutoWiring = true
            });

            if (dependency == null)
                throw new BenchmarkException("Dependency not resolved");
        }

        [Benchmark]
        public void DryIoc()
        {
            var container = new Container();
            container.Register<IDependency01, Dependency01>();
            container.Register<Dependency02>();
            container.Register<Dependency03>();

            var dependency = container.Resolve<Dependency03>();

            if (dependency == null)
                throw new BenchmarkException("Dependency not resolved");
        }

        [Benchmark]
        public void Autofac()
        {
            var builder = new ContainerBuilder();
            builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
            builder.RegisterType<Dependency01>().As<IDependency01>();
            var container = builder.Build();

            var dependency = container.Resolve<Dependency03>(new AutowiringParameter());

            if (dependency == null)
                throw new BenchmarkException("Dependency not resolved");
        }

        [Benchmark]
        public void MsDI()
        {
            var collection = new ServiceCollection();
            collection.AddTransient<IDependency01, Dependency01>();
            collection.AddTransient<Dependency02>();
            collection.AddTransient<Dependency03>();
            var container = collection.BuildServiceProvider();

            var dependency = container.GetRequiredService<Dependency03>();

            if (dependency == null)
                throw new BenchmarkException("Dependency not resolved");
        }

        private interface IDependency01
        {
        }

        private class Dependency01 : IDependency01
        {
        }

        private class Dependency02
        {
            public Dependency02(IDependency01 dependency)
            {
            }
        }

        private class Dependency03
        {
            public Dependency03(Dependency02 dependency)
            {
            }
        }
    }
}
