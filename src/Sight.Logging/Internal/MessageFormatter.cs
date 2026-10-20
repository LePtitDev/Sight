using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sight.Logging.Logs;

namespace Sight.Logging.Internal;

/// <summary>
/// Turn a message and its parameters into log parts
/// </summary>
internal static class MessageFormatter
{
    private static readonly Regex StackTraceRegex = new Regex(@"(?<PATH>(?>[a-zA-Z]:)?(?>[\/\\][^\/\\:]+)+):line (?<LINE>\d+)", RegexOptions.Compiled);

    /// <summary>
    /// Split a message into parts. Placeholders are <c>%e</c> / <c>%t</c> (next parameter as exception / time), <c>{0}</c> (parameter by index),
    /// <c>{name}</c> (next parameter), each hole accepting a format (<c>{0:N2}</c>). <c>%%</c>, <c>{{</c> and <c>}}</c> are literals;
    /// any malformed sequence or placeholder without parameter is kept as is.
    /// </summary>
    public static void Parse(List<object> parts, string message, object[] @params)
    {
        if (message.IndexOf('%') < 0 && message.IndexOf('{') < 0 && message.IndexOf('}') < 0)
        {
            parts.Add(Log.TextLog(message));
            return;
        }

        var next = 0;
        var bld = new StringBuilder();

        void Flush()
        {
            if (bld.Length > 0)
            {
                parts.Add(Log.TextLog(bld.ToString()));
                bld.Clear();
            }
        }

        for (var i = 0; i < message.Length; i++)
        {
            var c = message[i];
            var following = i + 1 < message.Length ? message[i + 1] : '\0';

            if (c == '%' && following == '%')
            {
                bld.Append('%');
                i++;
            }
            else if (c == '%' && (following == 'e' || following == 't') && next < @params.Length)
            {
                Flush();
                var param = @params[next++];
                parts.Add(following == 'e' ? ExceptionPart(param) : TimePart(param));
                i++;
            }
            else if (c == '{' && following == '{')
            {
                bld.Append('{');
                i++;
            }
            else if (c == '}' && following == '}')
            {
                bld.Append('}');
                i++;
            }
            else if (c == '{' && TryParseHole(message, i, out var end, out var index, out var format)
                     && TryResolveParam(@params, index, ref next, out var value))
            {
                Flush();
                parts.Add(ValuePart(value, format));
                i = end;
            }
            else
            {
                bld.Append(c);
            }
        }

        Flush();
    }

    public static object TimePart(object? param)
    {
        return param is TimeSpan time
            ? Log.ColoredLog(LogColors.Highlight, Log.TimedLog(time, Log.Format(time)))
            : Log.TextLog(param?.ToString() ?? string.Empty);
    }

    public static object ExceptionPart(object? param)
    {
        if (param is not Exception ex)
            return Log.TextLog(param?.ToString() ?? string.Empty);

        var exception = ex.ToString();
        var exceptionParts = new List<object>();
        var exceptionIndex = 0;
        foreach (var match in StackTraceRegex.Matches(exception).OfType<Match>())
        {
            if (exceptionIndex < match.Index)
                exceptionParts.Add(exception.Substring(exceptionIndex, match.Index - exceptionIndex));

            exceptionParts.Add(Log.HyperlinkLog($"{match.Groups["PATH"].Value}#L{match.Groups["LINE"].Value}", match.Value));
            exceptionIndex = match.Index + match.Length;
        }

        if (exceptionIndex < exception.Length)
            exceptionParts.Add(exception.Substring(exceptionIndex));

        return Log.ColoredLog(LogColors.LowOpacity, Log.RichLog(exceptionParts));
    }

    /// <summary>
    /// Render a value: exceptions and time spans get their rich rendering, other values are formatted with the invariant culture
    /// </summary>
    public static object ValuePart(object? value, string? format)
    {
        switch (value)
        {
            case Exception:
                return ExceptionPart(value);
            case TimeSpan when string.IsNullOrEmpty(format):
                return TimePart(value);
            default:
                return Log.TextLog(FormatText(value, format));
        }
    }

    /// <summary>
    /// Format a value with the invariant culture, an invalid format never throws (the value is written without format)
    /// </summary>
    public static string FormatText(object? value, string? format)
    {
        if (value == null)
            return "null";

        if (value is IFormattable formattable && !string.IsNullOrEmpty(format))
        {
            try
            {
                return formattable.ToString(format, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
            }
        }

        return value is IFormattable invariant ? invariant.ToString(null, CultureInfo.InvariantCulture) : value.ToString() ?? string.Empty;
    }

    private static bool TryResolveParam(object[] @params, int index, ref int next, out object? value)
    {
        // index < 0: named hole, takes the next parameter
        var position = index >= 0 ? index : next;
        if (position >= @params.Length)
        {
            value = null;
            return false;
        }

        if (index < 0)
            next++;

        value = @params[position];
        return true;
    }

    // Parse {name}, {0} or {name:format} starting at an opening brace. index: parameter index for a positional hole, -1 for a named one
    private static bool TryParseHole(string message, int start, out int end, out int index, out string? format)
    {
        end = message.IndexOf('}', start + 1);
        index = -1;
        format = null;
        if (end < 0)
            return false;

        var content = message.Substring(start + 1, end - start - 1);
        var colon = content.IndexOf(':');
        var name = colon < 0 ? content : content.Substring(0, colon);
        if (colon >= 0)
            format = content.Substring(colon + 1);

        if (name.Length == 0)
            return false;

        var digits = true;
        foreach (var c in name)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_' || c == '.'))
                return false;

            digits &= c >= '0' && c <= '9';
        }

        if (digits)
            return int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out index);

        return true;
    }
}
