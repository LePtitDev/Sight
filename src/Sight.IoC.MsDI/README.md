# Sight.IoC.MsDI

[![](https://img.shields.io/nuget/v/Sight.IoC.MsDI.svg)](https://www.nuget.org/packages/Sight.IoC.MsDI/)

This library implement some extension methods for Sight.IoC to support the Microsoft Dependency Injection.

## How to use?

*Thoses methods are extensions for `Sight.IoC` classes, see `Sight.IoC` documentation [here](../Sight.IoC/README.md).*

### Getting a `IServiceProvider` wrapped to a `ITypeResolver`

```csharp
container.RegisterType<MyService1>();

var provider = container.GetServiceProvider();

var service1 = provider.GetService<MyService1>(); // = MyService1

container.RegisterType<MyService2>();

var service1 = provider.GetService<MyService2>(); // = MyService2
```

### Build a `IServiceProvider` from a `ITypeResolver`

```csharp
container.RegisterType<MyService1>();

var provider = container.BuildServiceProvider();

var service1 = provider.GetService<MyService1>(); // = MyService1

container.RegisterType<MyService2>();

var service1 = provider.GetService<MyService2>(); // = null
```

You can also build only the `IServiceCollection`

```csharp
var services = container.BuildServicesCollection();
```

### Register services from `IServiceCollection`

```csharp
var services = new ServiceCollection();
services.AddLogger();

container.RegisterServicesCollection(services);

var provider = container.GetServiceProvider();
var logger = container.Resolve<ILogger>(resolveOptions: new object[] { provider }); // = ILogger
```
