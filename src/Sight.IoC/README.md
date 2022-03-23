# Sight.IoC

[![](https://img.shields.io/nuget/v/Sight.IoC.svg)](https://www.nuget.org/packages/Sight.IoC/)

This library implement dependency injection (IoC) with named services, auto resolve, registration extensibility and more.

## Getting started

You can inject and resolve your services by using the `TypeContainer`.

**Example:**
```csharp
var container = new TypeContainer();
container.RegisterType<IService, MyService>();

var service = container.Resolve<IService>(); // = MyService
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
container.RegisterType<IService, MyService>();

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

You can also use `IsRegistered`, `IsResolvable` and `IsInvokable`.
