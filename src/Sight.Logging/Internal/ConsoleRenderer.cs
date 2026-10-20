using System;
using System.Globalization;
using System.Text;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Internal;

/// <summary>
/// Render log messages with ANSI colors (optional) and ASCII icons
/// </summary>
internal sealed class ConsoleRenderer : LogRenderer
{
    private const string Reset = "\u001b[0m";

    private readonly bool _ansi;
    private readonly bool _hyperlinks;

    public ConsoleRenderer(TextLoggerOptions options, bool ansi, bool hyperlinks)
        : base(options)
    {
        _ansi = ansi;
        _hyperlinks = ansi && hyperlinks;
    }

    protected override string DefaultStyle => _ansi ? Reset : string.Empty;

    protected override void AppendIcon(StringBuilder builder, LogIcon icon, string indent)
    {
        if (!icon.IsDefined)
        {
            LogText.Append(builder, icon.Value.ToString() ?? string.Empty, indent);
            return;
        }

        builder.Append(icon.DefinedIcon switch
        {
            LogIcons.Debug => '…',
            LogIcons.Information => ' ',
            LogIcons.Warning => '#',
            LogIcons.Error => 'X',
            LogIcons.Start => '>',
            LogIcons.Stop => '%',
            LogIcons.Hidden => ' ',
            _ => '?'
        });
    }

    protected override string? OpenColor(StringBuilder builder, LogColor color)
    {
        if (!_ansi)
            return null;

        var style = color.IsDefined
            ? color.DefinedColor switch
            {
                LogColors.LowOpacity => "\u001b[90m", // dark gray
                LogColors.Highlight => "\u001b[36m", // dark cyan
                LogColors.Success => "\u001b[32m", // dark green
                LogColors.Warning => "\u001b[33m", // dark yellow
                LogColors.Error => "\u001b[31m", // dark red
                _ => Reset // default
            }
            : CustomColor(color.Value);

        if (style == null)
            return null;

        builder.Append(style);
        return style;
    }

    protected override string? OpenHyperlink(StringBuilder builder, string href, bool pathHidden)
    {
        if (!_ansi)
            return null;

        builder.Append("\u001b[4m");
        if (_hyperlinks && !pathHidden && TryCreateUri(href, out var uri))
        {
            builder.Append("\u001b]8;;").Append(uri).Append("\u001b\\");
            return "\u001b]8;;\u001b\\\u001b[24m";
        }

        return "\u001b[24m";
    }

    /// <summary>
    /// Convert a hyperlink reference (absolute URI or absolute path, optionally followed by <c>#fragment</c>) to an URI
    /// that is safe to write in an OSC 8 sequence
    /// </summary>
    private static bool TryCreateUri(string href, out string uri)
    {
        uri = string.Empty;
        if (string.IsNullOrEmpty(href))
            return false;

        var fragmentIndex = href.IndexOf('#');
        var location = fragmentIndex < 0 ? href : href.Substring(0, fragmentIndex);
        var fragment = fragmentIndex < 0 ? string.Empty : href.Substring(fragmentIndex);

        string absolute;
        try
        {
            if (!Uri.TryCreate(location, UriKind.Absolute, out var parsed))
                return false;

            absolute = parsed.AbsoluteUri;
        }
        catch (Exception ex) when (ex is UriFormatException or InvalidOperationException)
        {
            return false;
        }

        uri = fragment.Length > 1 ? absolute + "#" + Uri.EscapeDataString(fragment.Substring(1)) : absolute;

        // Only printable ASCII is allowed, a control character would end the sequence early
        foreach (var c in uri)
        {
            if (c < ' ' || c > '~')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Get the ANSI sequence of a custom color: a <see cref="ConsoleColor"/>, a <c>(byte, byte, byte)</c> RGB tuple or a <c>#RRGGBB</c> string
    /// </summary>
    private static string? CustomColor(object value)
    {
        switch (value)
        {
            case ConsoleColor consoleColor:
                return "\u001b[" + AnsiCode(consoleColor).ToString(CultureInfo.InvariantCulture) + "m";
            case ValueTuple<byte, byte, byte> rgb:
                return Rgb(rgb.Item1, rgb.Item2, rgb.Item3);
            case string { Length: 7 } hex when hex[0] == '#' && int.TryParse(hex.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number):
                return Rgb((byte)(number >> 16), (byte)(number >> 8), (byte)number);
            default:
                return null;
        }
    }

    private static string Rgb(byte r, byte g, byte b)
    {
        return string.Format(CultureInfo.InvariantCulture, "\u001b[38;2;{0};{1};{2}m", r, g, b);
    }

    private static int AnsiCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => 30,
            ConsoleColor.DarkRed => 31,
            ConsoleColor.DarkGreen => 32,
            ConsoleColor.DarkYellow => 33,
            ConsoleColor.DarkBlue => 34,
            ConsoleColor.DarkMagenta => 35,
            ConsoleColor.DarkCyan => 36,
            ConsoleColor.Gray => 37,
            ConsoleColor.DarkGray => 90,
            ConsoleColor.Red => 91,
            ConsoleColor.Green => 92,
            ConsoleColor.Yellow => 93,
            ConsoleColor.Blue => 94,
            ConsoleColor.Magenta => 95,
            ConsoleColor.Cyan => 96,
            _ => 97
        };
    }
}
