using System;
using System.Globalization;
using System.Text;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Internal;

/// <summary>
/// Render a log message tree to a text line. Text parts are always sanitized, only the sequences
/// written by the renderer itself (<see cref="OpenColor"/>, <see cref="OpenHyperlink"/>) can reach the output.
/// </summary>
internal abstract class LogRenderer
{
    private const int MaxCachedCapacity = 8192;

    private static readonly string[] Indents = new string[128];

    [ThreadStatic]
    private static StringBuilder? _cachedBuilder;

    private readonly bool _hideStackTracePaths;
    private readonly string? _timestampFormat;
    private readonly bool _utc;

    protected LogRenderer(TextLoggerOptions options)
    {
        _hideStackTracePaths = options.HideStackTracePaths;
        _timestampFormat = options.TimestampFormat;
        _utc = options.UtcTimestamp;
    }

    /// <summary>
    /// Style that restores the default rendering
    /// </summary>
    protected virtual string DefaultStyle => string.Empty;

    /// <summary>
    /// Render a message, without trailing line break
    /// </summary>
    public string Render(object message)
    {
        // Reuse a builder per thread, rendering is not re-entrant
        var builder = _cachedBuilder ?? new StringBuilder(256);
        _cachedBuilder = null;
        builder.Clear();

        var prefixWidth = AppendPrefix(builder, message);
        Render(builder, message, GetIndent(prefixWidth + LogText.HeaderWidth), DefaultStyle);
        var text = builder.ToString();

        if (builder.Capacity <= MaxCachedCapacity)
            _cachedBuilder = builder;

        return text;
    }

    private static string GetIndent(int width)
    {
        if (width >= Indents.Length)
            return new string(' ', width);

        return Indents[width] ??= new string(' ', width);
    }

    /// <summary>
    /// Write the timestamp and category of the message
    /// </summary>
    /// <returns>Width of the written prefix</returns>
    private int AppendPrefix(StringBuilder builder, object message)
    {
        LogMessage.Inspect(message, out var timestamp, out var category);
        if (_timestampFormat == null && category == null)
            return 0;

        var style = OpenColor(builder, LogColors.LowOpacity);
        var textStart = builder.Length;

        if (_timestampFormat != null)
        {
            // Messages not created by the extensions have no timestamp: use the writing time
            // Formatted as a DateTime so that the "K" specifier gives Z (UTC) or the local offset
            var time = timestamp ?? DateTimeOffset.UtcNow;
            var date = _utc ? time.UtcDateTime : time.LocalDateTime;

            builder.Append(date.ToString(_timestampFormat, CultureInfo.InvariantCulture)).Append(' ');
        }

        if (category != null)
        {
            builder.Append('[');
            LogText.Append(builder, category, string.Empty);
            builder.Append("] ");
        }

        var width = builder.Length - textStart;
        if (style != null)
            builder.Append(DefaultStyle);

        return width;
    }

    protected abstract void AppendIcon(StringBuilder builder, LogIcon icon, string indent);

    /// <summary>
    /// Write the sequence that starts a color
    /// </summary>
    /// <returns>The written style (to restore it after nested parts), null if nothing was written</returns>
    protected virtual string? OpenColor(StringBuilder builder, LogColor color) => null;

    /// <summary>
    /// Write the sequence that starts a hyperlink
    /// </summary>
    /// <param name="builder">Output</param>
    /// <param name="href">Hyperlink reference</param>
    /// <param name="pathHidden">Indicates that the directory of the displayed path was hidden, so the reference must not leak it</param>
    /// <returns>The sequence that ends the hyperlink, null if nothing was written</returns>
    protected virtual string? OpenHyperlink(StringBuilder builder, string href, bool pathHidden) => null;

    private void Render(StringBuilder builder, object message, string indent, string style)
    {
        switch (message)
        {
            case LogIcon icon:
                AppendIcon(builder, icon, indent);
                break;
            case IColoredLog coloredLog:
                var colorStyle = OpenColor(builder, coloredLog.Color);
                Render(builder, coloredLog.Content, indent, colorStyle ?? style);
                if (colorStyle != null)
                    builder.Append(style);

                break;
            case IHyperlinkLog hyperlinkLog:
                var content = hyperlinkLog.Content;
                var pathHidden = false;
                if (_hideStackTracePaths && content is string path)
                {
                    var trimmed = LogText.TrimStackTracePath(path);
                    pathHidden = trimmed != path;
                    content = trimmed;
                }

                var closing = OpenHyperlink(builder, hyperlinkLog.Href, pathHidden);
                Render(builder, content, indent, style);
                if (closing != null)
                    builder.Append(closing);

                break;
            case IRichLog richLog:
                foreach (var part in richLog)
                {
                    Render(builder, part, indent, style);
                }

                break;
            case IContentLog contentLog:
                Render(builder, contentLog.Content, indent, style);
                break;
            default:
                LogText.Append(builder, message.ToString() ?? string.Empty, indent);
                break;
        }
    }
}
