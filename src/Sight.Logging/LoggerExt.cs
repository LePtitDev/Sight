using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sight.Logging.Internal;
using Sight.Logging.Logs;

namespace Sight.Logging;

/// <summary>
/// Extension methods for <see cref="ILogger"/>
/// </summary>
/// <remarks>
/// Messages accept parameters: <c>%e</c> / <c>%t</c> display the next parameter as an exception / a time span, <c>{0}</c> displays the
/// parameter at an index and <c>{name}</c> the next parameter (both accept a format, <c>{0:N2}</c>). <c>%%</c>, <c>{{</c> and <c>}}</c> are
/// literal characters. A message without parameter is written as is.
/// </remarks>
public static class LoggerExt
{
    /// <summary>
    /// Log trace message (most detailed level)
    /// </summary>
    public static void LogTrace(this ILogger logger, string message, params object[] @params)
    {
        LogImpl(logger, LogKind.Trace, message, @params);
    }

    /// <summary>
    /// Log debug message
    /// </summary>
    public static void LogDebug(this ILogger logger, string message, params object[] @params)
    {
        LogImpl(logger, LogKind.Debug, message, @params);
    }

    /// <summary>
    /// Log information message
    /// </summary>
    public static void LogInformation(this ILogger logger, string message, params object[] @params)
    {
        LogImpl(logger, LogKind.Information, message, @params);
    }

    /// <summary>
    /// Log warning message
    /// </summary>
    public static void LogWarning(this ILogger logger, string message, params object[] @params)
    {
        LogImpl(logger, LogKind.Warning, message, @params);
    }

    /// <summary>
    /// Log error message
    /// </summary>
    public static void LogError(this ILogger logger, string message, params object[] @params)
    {
        LogImpl(logger, LogKind.Error, message, @params);
    }

    /// <summary>
    /// Log exception message
    /// </summary>
    public static void LogError(this ILogger logger, Exception exception, string? message = null, params object[] @params)
    {
        LogException(logger, LogKind.Error, exception, message, @params);
    }

    /// <summary>
    /// Log critical error message
    /// </summary>
    public static void LogCritical(this ILogger logger, string message, params object[] @params)
    {
        LogImpl(logger, LogKind.Critical, message, @params);
    }

    /// <summary>
    /// Log critical exception message
    /// </summary>
    public static void LogCritical(this ILogger logger, Exception exception, string? message = null, params object[] @params)
    {
        LogException(logger, LogKind.Critical, exception, message, @params);
    }

    /// <summary>
    /// Log message that indicates a record start
    /// </summary>
    public static Stopwatch LogTimeStart(this ILogger logger, string message, params object[] @params)
    {
        LogImpl(logger, LogKind.TimeStart, message, @params);
        return Stopwatch.StartNew();
    }

    /// <summary>
    /// Log message that indicates a record stop
    /// </summary>
    public static void LogTimeStop(this ILogger logger, Stopwatch stopwatch, string message, params object[] @params)
    {
        if (logger.IsEnabled(LogKind.TimeStop.Level))
            LogTimeStopImpl(logger, stopwatch, message, @params);
    }

    private static void LogTimeStopImpl(ILogger logger, Stopwatch stopwatch, string message, object[] @params)
    {
        LogImpl(logger, LogKind.TimeStop, message, @params, suffixFactory: () => new[] { Log.TextLog(": "), MessageFormatter.TimePart(stopwatch.Elapsed) });
    }

    private static void LogException(ILogger logger, LogKind kind, Exception exception, string? message, object[] @params)
    {
        if (logger.IsEnabled(kind.Level))
            LogExceptionImpl(logger, kind, exception, message, @params);
    }

    private static void LogExceptionImpl(ILogger logger, LogKind kind, Exception exception, string? message, object[] @params)
    {
        LogImpl(logger, kind, message ?? string.Empty, @params, suffixFactory: () => string.IsNullOrEmpty(message)
            ? new[] { MessageFormatter.ExceptionPart(exception) }
            : new[] { Log.TextLog(": "), MessageFormatter.ExceptionPart(exception) });
    }

    private static void LogImpl(ILogger logger, LogKind kind, string message, object[] @params, Func<object[]>? suffixFactory = null)
    {
        // Filtered messages are not even built
        if (!logger.IsEnabled(kind.Level))
            return;

        if (@params.Length == 0 && suffixFactory == null)
        {
            kind.Emit(logger, new object[] { message });
            return;
        }

        var parts = new List<object>(1 + @params.Length * 2 + 2);
        if (@params.Length > 0)
            MessageFormatter.Parse(parts, message, @params);
        else
            parts.Add(Log.TextLog(message));

        if (suffixFactory != null)
            parts.AddRange(suffixFactory());

        kind.Emit(logger, parts);
    }
}
