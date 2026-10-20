#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Sight.Logging.Internal;
using Sight.Logging.Logs;

namespace Sight.Logging;

/// <summary>
/// Describe the level of the messages built by a <see cref="LogInterpolatedStringHandler{TLevel}"/>
/// </summary>
public interface ILogLevelMarker
{
    /// <summary>
    /// Message level
    /// </summary>
    public LogLevels Level { get; }
}

/// <summary>
/// Marker of the <see cref="LogLevels.Trace"/> level
/// </summary>
public readonly struct TraceMarker : ILogLevelMarker
{
    /// <inheritdoc />
    public LogLevels Level => LogLevels.Trace;
}

/// <summary>
/// Marker of the <see cref="LogLevels.Debug"/> level
/// </summary>
public readonly struct DebugMarker : ILogLevelMarker
{
    /// <inheritdoc />
    public LogLevels Level => LogLevels.Debug;
}

/// <summary>
/// Marker of the <see cref="LogLevels.Information"/> level
/// </summary>
public readonly struct InformationMarker : ILogLevelMarker
{
    /// <inheritdoc />
    public LogLevels Level => LogLevels.Information;
}

/// <summary>
/// Marker of the <see cref="LogLevels.Warning"/> level
/// </summary>
public readonly struct WarningMarker : ILogLevelMarker
{
    /// <inheritdoc />
    public LogLevels Level => LogLevels.Warning;
}

/// <summary>
/// Marker of the <see cref="LogLevels.Error"/> level
/// </summary>
public readonly struct ErrorMarker : ILogLevelMarker
{
    /// <inheritdoc />
    public LogLevels Level => LogLevels.Error;
}

/// <summary>
/// Marker of the <see cref="LogLevels.Critical"/> level
/// </summary>
public readonly struct CriticalMarker : ILogLevelMarker
{
    /// <inheritdoc />
    public LogLevels Level => LogLevels.Critical;
}

/// <summary>
/// Interpolated string handler of the logging extensions: when the level is filtered out the interpolated
/// values are not evaluated nor formatted. Exceptions and time spans get the same rich rendering as <c>%e</c> and <c>%t</c>.
/// </summary>
/// <typeparam name="TLevel">Level of the message</typeparam>
[InterpolatedStringHandler]
public ref struct LogInterpolatedStringHandler<TLevel>
    where TLevel : struct, ILogLevelMarker
{
    private readonly List<object>? _parts;
    private readonly StringBuilder? _text;

    /// <summary>
    /// Initialize a new instance of the struct <see cref="LogInterpolatedStringHandler{TLevel}"/>
    /// </summary>
    public LogInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        isEnabled = logger.IsEnabled(default(TLevel).Level);
        IsEnabled = isEnabled;
        if (isEnabled)
        {
            _parts = new List<object>(formattedCount * 2 + 1);
            _text = new StringBuilder(literalLength + formattedCount * 8);
        }
        else
        {
            _parts = null;
            _text = null;
        }
    }

    internal bool IsEnabled { get; }

    /// <summary>
    /// Append a literal part of the message
    /// </summary>
    public void AppendLiteral(string value)
    {
        _text?.Append(value);
    }

    /// <summary>
    /// Append an interpolated value
    /// </summary>
    public void AppendFormatted<T>(T value)
    {
        AppendFormatted(value, null);
    }

    /// <summary>
    /// Append an interpolated value with a format
    /// </summary>
    public void AppendFormatted<T>(T value, string? format)
    {
        if (_parts == null)
            return;

        if (value is Exception || (value is TimeSpan && string.IsNullOrEmpty(format)))
        {
            FlushText();
            _parts.Add(MessageFormatter.ValuePart(value, format));
        }
        else
        {
            _text!.Append(MessageFormatter.FormatText(value, format));
        }
    }

    /// <summary>
    /// Append an interpolated value with an alignment and a format
    /// </summary>
    public void AppendFormatted<T>(T value, int alignment, string? format = null)
    {
        if (_parts == null)
            return;

        var text = MessageFormatter.FormatText(value, format);

        _text!.Append(alignment < 0 ? text.PadRight(-alignment) : text.PadLeft(alignment));
    }

    internal void Emit(ILogger logger)
    {
        if (_parts == null)
            return;

        FlushText();
        LogKind.Of(default(TLevel).Level).Emit(logger, _parts);
    }

    private void FlushText()
    {
        if (_text!.Length > 0)
        {
            _parts!.Add(Log.TextLog(_text.ToString()));
            _text.Clear();
        }
    }
}

/// <summary>
/// Extension methods of <see cref="ILogger"/> taking an interpolated string
/// </summary>
public static class LoggerInterpolationExt
{
    /// <summary>
    /// Log trace message
    /// </summary>
    public static void LogTrace(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LogInterpolatedStringHandler<TraceMarker> message)
    {
        message.Emit(logger);
    }

    /// <summary>
    /// Log debug message
    /// </summary>
    public static void LogDebug(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LogInterpolatedStringHandler<DebugMarker> message)
    {
        message.Emit(logger);
    }

    /// <summary>
    /// Log information message
    /// </summary>
    public static void LogInformation(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LogInterpolatedStringHandler<InformationMarker> message)
    {
        message.Emit(logger);
    }

    /// <summary>
    /// Log warning message
    /// </summary>
    public static void LogWarning(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LogInterpolatedStringHandler<WarningMarker> message)
    {
        message.Emit(logger);
    }

    /// <summary>
    /// Log error message
    /// </summary>
    public static void LogError(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LogInterpolatedStringHandler<ErrorMarker> message)
    {
        message.Emit(logger);
    }

    /// <summary>
    /// Log critical error message
    /// </summary>
    public static void LogCritical(this ILogger logger, [InterpolatedStringHandlerArgument("logger")] ref LogInterpolatedStringHandler<CriticalMarker> message)
    {
        message.Emit(logger);
    }
}
#endif
