using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sight.Logging.Extensions;

/// <summary>
/// Extension methods to write Microsoft.Extensions.Logging messages to Sight loggers
/// </summary>
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Write the messages to a Sight logger. The logger is not disposed with the service provider.
    /// </summary>
    public static ILoggingBuilder AddSight(this ILoggingBuilder builder, Sight.Logging.ILogger logger)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (logger == null)
            throw new ArgumentNullException(nameof(logger));

        builder.Services.AddSingleton<ILoggerProvider>(new SightLoggerProvider(logger));
        return builder;
    }

    /// <summary>
    /// Write the messages to a Sight logger created by the service provider. The logger is disposed with the service provider
    /// if it is disposable.
    /// </summary>
    public static ILoggingBuilder AddSight(this ILoggingBuilder builder, Func<IServiceProvider, Sight.Logging.ILogger> factory)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        builder.Services.AddSingleton<ILoggerProvider>(provider => new SightLoggerProvider(factory(provider), disposeLogger: true));
        return builder;
    }
}
