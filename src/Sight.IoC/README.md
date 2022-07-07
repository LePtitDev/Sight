# Sight.IoC

[![](https://img.shields.io/nuget/v/Sight.IoC.svg)](https://www.nuget.org/packages/Sight.IoC/)

This library implement dependency injection (IoC) with named services, auto resolve, registration extensibility and more.

## Why use it?

* Easy to use
* Efficient
* Extensible
* Some cool features:
  * Registration of delegates, types, generics
  * Keyed or non-keyed registrations
  * Thread safe
  * Multiple service implementations support
  * Implementation overridable
  * Injection for types and methods
  * Asynchronous resolution support
  * Unregistered type resolution support
  * ...and more

## Getting started

You can inject and resolve your services by using the `TypeContainer`.

**Example:**
```csharp
var container = new TypeContainer();
container.RegisterType<MyService, IService>();

var service = container.Resolve<IService>(); // = MyService
```

## Benchmarks

A simple senario with 3 dependencies was made to compare `Sight.IoC 0.1.3`, `DryIoc 5.0.1`, `Autofac 6.3.0` and `Microsoft.Extensions.DependencyInjection 6.0.0`.

```md
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1645 (21H2)
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.202
  [Host]   : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT
  .NET 6.0 : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT

Job=.NET 6.0  Runtime=.NET 6.0

|  Method |      Mean |     Error |    StdDev |    Median |
|-------- |----------:|----------:|----------:|----------:|
|   Sight |  2.659 us | 0.0254 us | 0.0212 us |  2.655 us |
|  DryIoc |  2.287 us | 0.0456 us | 0.1202 us |  2.258 us |
| Autofac | 44.154 us | 0.7919 us | 1.8975 us | 43.286 us |
|    MsDI |  6.367 us | 0.5149 us | 1.5182 us |  6.159 us |
```

## Registration

You can register service with a provider that will return you service. It allow you to initialize your instance with custom code.

**Example:**
```csharp
container.RegisterProvider<IService>((type, options) => new MyService());
```

A provider is not required when you register concrete types, the resolver can auto initialize by injecting dependencies in the constructor.

**Example:**
```csharp
class MyService : IService
{
    public MyService(IDependency dependency)
    {
        // Make something...
    }
}

// In this example IDependency is already injected in container
container.RegisterType<MyService, IService>();

// The container will initialize the service with the registered dependency
var service = container.Resolve<IService>();
```

If you want to register an already initialized service, use:

```csharp
container.RegisterInstance<IService>(new MyService());
```

Generic registrations are supported with same methods.

**Example:**
```csharp
class MyService<T> : IService<T>
{
}

container.RegisterProvider(typeof(IService<>), (t, o) =>
{
    var concreteType = typeof(MyService<>).MakeGenericType(t.GetGenericArguments());
    return Activator.CreateInstance(concreteType);
);

// -- or --

container.RegisterType(typeof(MyService<>), typeof(IService<>));

// Resolve the generic service
var service = container.Resolve<IService<string>>();
```

## Resolution

You can resolve a service by calling `Resolve` method...

```csharp
container.Resolve<IService>();
```

... and invoke a method with dependency injection.

```csharp
public class MyClass
{
    public void MyMethod(IService service)
    {
        // Do something
    }
}

var instance = new MyClass();
container.Invoke(typeof(MyClass).GetMethod("MyMethod"), instance);
```

If you want to check if a service is resolvable before you can use `TryResolve` and `TryInvoke`.

**Example:**
```csharp
if (container.TryResolve<IService>(ResolveOptions.Default, out var service))
{
    // Do something
}
```

All those methods are also available for asynchronous resolution.

**Example:**
```csharp
public class MyClass2
{
    public MyClass2(MyClass1 myClass1)
    {
        // Do something
    }
}

container.RegisterProvider(async (t, o) => await MyClass1.CreateAsync());
container.RegisterType<MyClass2>();

var myClass2 = await container.ResolveAsync<MyClass2>();
```

You can also use `IsRegistered`, `IsResolvable` and `IsInvokable`.
