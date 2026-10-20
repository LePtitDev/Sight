using System;
using System.Globalization;
using System.Text;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Text;

/// <summary>
/// Canonical formatting of scalar literals (text specification §8)
/// </summary>
internal static class TextFormatting
{
    public static string FormatScalar(DataValue value, bool withSuffix)
    {
        switch (value.Kind)
        {
            case TypeKind.Bool:
                return value.AsBoolean() ? "true" : "false";
            case TypeKind.String:
                return FormatString(value.AsString());
            case TypeKind.F32:
            case TypeKind.F64:
                var text = FormatFloat(value.AsDouble(), value.Kind == TypeKind.F32);
                return withSuffix && value.Kind == TypeKind.F32 ? text + "f32" : text;
            default:
                var integer = value.Kind.IsSignedInteger()
                    ? value.AsInt64().ToString(CultureInfo.InvariantCulture)
                    : value.AsUInt64().ToString(CultureInfo.InvariantCulture);
                return withSuffix && value.Kind != TypeKind.S64 ? integer + value.Kind.ToKeyword() : integer;
        }
    }

    /// <summary>
    /// Shortest decimal representation that parses back to the same value
    /// </summary>
    public static string FormatFloat(double value, bool isSingle)
    {
        if (double.IsNaN(value))
            return "nan";

        if (double.IsPositiveInfinity(value))
            return "inf";

        if (double.IsNegativeInfinity(value))
            return "-inf";

        string raw;
        if (isSingle)
        {
            var single = (float)value;
            raw = single.ToString("R", CultureInfo.InvariantCulture);
            if (float.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture) != single)
                raw = single.ToString("G9", CultureInfo.InvariantCulture);
        }
        else
        {
            raw = value.ToString("R", CultureInfo.InvariantCulture);
            if (double.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture) != value)
                raw = value.ToString("G17", CultureInfo.InvariantCulture);
        }

        // Normalize to digits + exponent, then render with the canonical rules
        var negative = raw.StartsWith("-", StringComparison.Ordinal);
        if (negative)
            raw = raw.Substring(1);

        var exponent = 0;
        var e = raw.IndexOfAny(new[] { 'E', 'e' });
        if (e >= 0)
        {
            exponent = int.Parse(raw.Substring(e + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            raw = raw.Substring(0, e);
        }

        var dot = raw.IndexOf('.');
        var integerPart = dot < 0 ? raw : raw.Substring(0, dot);
        var fraction = dot < 0 ? string.Empty : raw.Substring(dot + 1);
        var digits = (integerPart + fraction).TrimStart('0');
        var pointPosition = integerPart.Length + exponent - ((integerPart + fraction).Length - (integerPart + fraction).TrimStart('0').Length);
        digits = digits.TrimEnd('0');
        if (digits.Length == 0)
            return negative ? "-0.0" : "0.0";

        // value = 0.digits × 10^pointPosition ; decimal exponent of the first digit:
        var decimalExponent = pointPosition - 1;
        var builder = new StringBuilder();
        if (negative)
            builder.Append('-');

        if (decimalExponent < -6 || decimalExponent > 20)
        {
            builder.Append(digits[0]);
            if (digits.Length > 1)
                builder.Append('.').Append(digits, 1, digits.Length - 1);

            builder.Append('e').Append(decimalExponent.ToString(CultureInfo.InvariantCulture));
        }
        else if (pointPosition <= 0)
        {
            builder.Append("0.").Append('0', -pointPosition).Append(digits);
        }
        else if (pointPosition >= digits.Length)
        {
            builder.Append(digits).Append('0', pointPosition - digits.Length).Append(".0");
        }
        else
        {
            builder.Append(digits, 0, pointPosition).Append('.').Append(digits, pointPosition, digits.Length - pointPosition);
        }

        return builder.ToString();
    }

    public static string FormatString(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\0':
                    builder.Append("\\0");
                    break;
                default:
                    if (c < 0x20 || c == 0x7F)
                        builder.Append("\\u{").Append(((int)c).ToString("X", CultureInfo.InvariantCulture)).Append('}');
                    else
                        builder.Append(c);

                    break;
            }
        }

        return builder.Append('"').ToString();
    }

    /// <summary>
    /// Identifier or quoted string, depending on the content
    /// </summary>
    public static string FormatName(string value) => Internal.Names.IsIdentifier(value) ? value : FormatString(value);
}
