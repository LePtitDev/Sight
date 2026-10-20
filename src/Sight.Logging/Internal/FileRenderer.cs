using System.Text;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Internal;

/// <summary>
/// Render log messages as plain text with emoji icons
/// </summary>
internal sealed class FileRenderer : LogRenderer
{
    private static readonly string DebugIcon = char.ConvertFromUtf32(0x1F4AC);

    public FileRenderer(TextLoggerOptions options)
        : base(options)
    {
    }

    protected override void AppendIcon(StringBuilder builder, LogIcon icon, string indent)
    {
        if (!icon.IsDefined)
        {
            LogText.Append(builder, icon.Value.ToString() ?? string.Empty, indent);
            return;
        }

        switch (icon.DefinedIcon)
        {
            case LogIcons.Debug:
                builder.Append(DebugIcon);
                break;
            case LogIcons.Information:
                builder.Append('ℹ');
                break;
            case LogIcons.Warning:
                builder.Append('⚠');
                break;
            case LogIcons.Error:
                builder.Append('❌');
                break;
            case LogIcons.Start:
                builder.Append('⏱');
                break;
            case LogIcons.Stop:
                builder.Append('⏹');
                break;
            case LogIcons.Hidden:
                builder.Append(' ');
                break;
            default:
                builder.Append('�');
                break;
        }
    }
}
