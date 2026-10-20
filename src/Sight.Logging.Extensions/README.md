# Sight.Logging.Extensions

[![](https://img.shields.io/nuget/v/Sight.Logging.Extensions.svg)](https://www.nuget.org/packages/Sight.Logging.Extensions/)

This library is a bridge between `Microsoft.Extensions.Logging` and `Sight.Logging`: everything logged through
`ILogger<T>` / `ILoggerFactory` is written by a Sight logger (console, file, memory, ...).

## How to use?

```csharp
var services = new ServiceCollection();
services.AddLogging(builder => builder
    .SetMinimumLevel(LogLevel.Debug)
    .AddSight(new FileLogger("logs.txt", createThread: true)));
```

The logger can also be created (and disposed) by the service provider:

```csharp
services.AddLogging(builder => builder.AddSight(provider => new ConsoleLogger()));
```

- The category name of the `Microsoft.Extensions.Logging` logger becomes the Sight category (`[My.Namespace.MyService]` in the output).
- Levels are mapped one to one (`Trace` ... `Critical`), and `IsEnabled` follows the Sight logger, so a Sight logger created with
  `WithMinimumLevel(...)` filters the messages.
- The message is formatted by `Microsoft.Extensions.Logging` (`{Name}` holes included) and written as is; an exception is appended
  with the same rendering as `LogError(exception)`.
- Scopes and event ids are ignored.
- `AddSight(ILogger)` does not dispose the logger, `AddSight(Func<IServiceProvider, ILogger>)` does.
