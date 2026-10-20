using System.Globalization;
using System.Linq;
using System.Text;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Text;

/// <summary>
/// Canonical text form of a schema
/// </summary>
internal static class SchemaPrinter
{
    public static string Print(Schema schema, string indent = "  ")
    {
        var builder = new StringBuilder();
        Append(builder, schema, indent, string.Empty, isFile: true);
        return builder.ToString();
    }

    /// <summary>
    /// Append a schema source: a whole <c>.sgs</c> file, or the content of an inline block (without <c>@sgs</c>)
    /// </summary>
    public static void Append(StringBuilder builder, Schema schema, string indent, string linePrefix, bool isFile)
    {
        if (isFile)
            builder.Append(linePrefix).Append("@sgs ").Append(SchemaParser.FormatVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');

        builder.Append(linePrefix).Append("@id ").Append(schema.Name).Append('\n');
        if (schema.Version != 0)
            builder.Append(linePrefix).Append("@version ").Append(schema.Version.ToString(CultureInfo.InvariantCulture)).Append('\n');

        foreach (var entry in schema.Metadata)
            builder.Append(linePrefix).Append("@meta ").Append(TextFormatting.FormatName(entry.Key)).Append(" = ").Append(TextFormatting.FormatString(entry.Value)).Append('\n');

        foreach (var import in schema.Imports)
            builder.Append(linePrefix).Append("@import ").Append(import.Name).Append(' ').Append(import.Version.ToString(CultureInfo.InvariantCulture)).Append('\n');

        foreach (var type in schema.Types)
        {
            builder.Append('\n');
            switch (type)
            {
                case EnumDefinition definition:
                    builder.Append(linePrefix).Append("enum ").Append(definition.Name).Append(" : ").Append(definition.Underlying.ToKeyword()).Append(" {\n");
                    foreach (var value in definition.Values)
                    {
                        var number = definition.Underlying == TypeKind.U64 ? ((ulong)value.Value).ToString(CultureInfo.InvariantCulture) : value.Value.ToString(CultureInfo.InvariantCulture);
                        builder.Append(linePrefix).Append(indent).Append(value.Name).Append(" = ").Append(number).Append('\n');
                    }

                    builder.Append(linePrefix).Append("}\n");
                    break;
                case StructDefinition definition:
                    builder.Append(linePrefix);
                    if (definition.IsAbstract)
                        builder.Append("abstract ");

                    builder.Append("struct ").Append(definition.Name);
                    if (definition.Base != null)
                        builder.Append(" : ").Append(definition.Base.GetName(schema));

                    if (definition.IsPositional)
                        builder.Append(" @positional");

                    if (definition.Fields.Count == 0)
                    {
                        builder.Append(" {}\n");
                        break;
                    }

                    builder.Append(" {\n");
                    foreach (var field in definition.Fields)
                    {
                        builder.Append(linePrefix).Append(indent).Append(field.Name).Append(": ");
                        field.Type.AsNonFixed().Append(builder, schema);
                        if (field.DefaultValue != null)
                            builder.Append(" = ").Append(FormatDefault(field.DefaultValue));

                        if (field.Type.IsFixed)
                            builder.Append(" @fixed");

                        if (field.Aliases.Count > 0)
                            builder.Append(" @alias(").Append(string.Join(", ", field.Aliases.ToArray())).Append(')');

                        builder.Append('\n');
                    }

                    builder.Append(linePrefix).Append("}\n");
                    break;
                case AliasDefinition definition:
                    builder.Append(linePrefix).Append("type ").Append(definition.Name).Append(" = ");
                    definition.Target.Append(builder, schema);
                    builder.Append('\n');
                    break;
            }
        }
    }

    private static string FormatDefault(DataValue value)
    {
        return value is DataEnum data ? data.Name : TextFormatting.FormatScalar(value, withSuffix: false);
    }
}
