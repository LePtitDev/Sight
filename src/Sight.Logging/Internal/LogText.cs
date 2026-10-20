using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Sight.Logging.Internal;

/// <summary>
/// Helpers to write untrusted text in a line oriented output
/// </summary>
internal static class LogText
{
    /// <summary>
    /// Width of the icon and type columns written by <see cref="LoggerExt"/>, continuation lines are indented by this width
    /// </summary>
    public const int HeaderWidth = 14;

    private static readonly Regex StackTracePathRegex = new Regex(@"^.*[\\/](?<rest>[^\\/]*:line \d+)$", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Append a text to a line. Control characters (ESC, CSI, OSC, BEL, NUL, ...) and bidirectional overrides are replaced by a visible
    /// escape (<c>\x1B</c>, <c>\u202E</c>), tabs are kept and every line break is replaced by a line break followed by <paramref name="indent"/>,
    /// so a text can neither emit terminal sequences nor start a line that looks like a log header.
    /// </summary>
    public static void Append(StringBuilder builder, string text, string indent)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (!NeedsHandling(c))
                continue;

            builder.Append(text, start, i - start);
            start = i + 1;

            if (c == '\n' || c == '\r' || c == '\u2028' || c == '\u2029' || c == '\u0085')
            {
                // \r\n is a single line break
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    continue;

                builder.Append(Environment.NewLine).Append(indent);
            }
            else if (c < 0x100)
            {
                builder.Append("\\x").Append(((int)c).ToString("X2"));
            }
            else
            {
                builder.Append("\\u").Append(((int)c).ToString("X4"));
            }
        }

        if (start == 0)
            builder.Append(text);
        else
            builder.Append(text, start, text.Length - start);
    }

    /// <summary>
    /// Remove the directory of a stack trace location (<c>C:\build\Foo.cs:line 12</c> => <c>Foo.cs:line 12</c>)
    /// </summary>
    public static string TrimStackTracePath(string text)
    {
        var match = StackTracePathRegex.Match(text);
        return match.Success ? match.Groups["rest"].Value : text;
    }

    private static bool NeedsHandling(char c)
    {
        return (c < ' ' && c != '\t')
               || (c >= '\u007f' && c <= '\u009f')
               || c == '\u2028' || c == '\u2029'
               || (c >= '\u202a' && c <= '\u202e')
               || (c >= '\u2066' && c <= '\u2069');
    }
}
