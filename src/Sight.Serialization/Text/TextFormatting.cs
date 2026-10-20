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
        return AppendScalar(new StringBuilder(), value, withSuffix).ToString();
    }

    /// <summary>
    /// Append the canonical form of a scalar
    /// </summary>
    public static StringBuilder AppendScalar(StringBuilder builder, DataValue value, bool withSuffix)
    {
        switch (value.Kind)
        {
            case TypeKind.Bool:
                return builder.Append(value.AsBoolean() ? "true" : "false");
            case TypeKind.String:
                return AppendString(builder, value.AsString());
            case TypeKind.F32:
            case TypeKind.F64:
                AppendFloat(builder, value.AsDouble(), value.Kind == TypeKind.F32);
                return withSuffix && value.Kind == TypeKind.F32 ? builder.Append("f32") : builder;
            default:
                if (value.Kind.IsSignedInteger())
                    AppendInteger(builder, value.AsInt64());
                else
                    AppendInteger(builder, value.AsUInt64());

                return withSuffix && value.Kind != TypeKind.S64 ? builder.Append(value.Kind.ToKeyword()) : builder;
        }
    }

    /// <summary>
    /// Shortest decimal representation that parses back to the same value
    /// </summary>
    public static string FormatFloat(double value, bool isSingle)
    {
        return AppendFloat(new StringBuilder(), value, isSingle).ToString();
    }

    /// <summary>
    /// Append the shortest decimal representation that parses back to the same value
    /// </summary>
    public static StringBuilder AppendFloat(StringBuilder builder, double value, bool isSingle)
    {
        if (double.IsNaN(value))
            return builder.Append("nan");

        if (double.IsPositiveInfinity(value))
            return builder.Append("inf");

        if (double.IsNegativeInfinity(value))
            return builder.Append("-inf");

        Span<char> buffer = stackalloc char[64];
        var raw = FormatRoundTrip(value, isSingle, buffer);

        // Normalize to digits + exponent, then render with the canonical rules
        var negative = raw.Length > 0 && raw[0] == '-';
        if (negative)
            raw = raw.Slice(1);

        var exponent = 0;
        var e = raw.IndexOfAny('E', 'e');
        if (e >= 0)
        {
            exponent = ParseExponent(raw.Slice(e + 1));
            raw = raw.Slice(0, e);
        }

        // Digits of the integer part and of the fraction, without the dot
        Span<char> digits = stackalloc char[raw.Length];
        var count = 0;
        var integerLength = raw.Length;
        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] == '.')
                integerLength = i;
            else
                digits[count++] = raw[i];
        }

        var leadingZeros = 0;
        while (leadingZeros < count && digits[leadingZeros] == '0')
            leadingZeros++;

        var pointPosition = integerLength + exponent - leadingZeros;
        var length = count - leadingZeros;
        while (length > 0 && digits[leadingZeros + length - 1] == '0')
            length--;

        if (length == 0)
            return builder.Append(negative ? "-0.0" : "0.0");

        var significant = digits.Slice(leadingZeros, length);

        // value = 0.digits × 10^pointPosition ; decimal exponent of the first digit:
        var decimalExponent = pointPosition - 1;
        if (negative)
            builder.Append('-');

        if (decimalExponent < -6 || decimalExponent > 20)
        {
            builder.Append(significant[0]);
            if (length > 1)
                Append(builder.Append('.'), significant.Slice(1));

            builder.Append('e');
            AppendInteger(builder, decimalExponent);
        }
        else if (pointPosition <= 0)
        {
            Append(builder.Append("0.").Append('0', -pointPosition), significant);
        }
        else if (pointPosition >= length)
        {
            Append(builder, significant).Append('0', pointPosition - length).Append(".0");
        }
        else
        {
            Append(Append(builder, significant.Slice(0, pointPosition)).Append('.'), significant.Slice(pointPosition));
        }

        return builder;
    }

    public static string FormatString(string value)
    {
        return AppendString(new StringBuilder(value.Length + 2), value).ToString();
    }

    /// <summary>
    /// Append a quoted and escaped string
    /// </summary>
    public static StringBuilder AppendString(StringBuilder builder, string value)
    {
        builder.Append('"');
        var start = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c >= 0x20 && c != '"' && c != '\\' && c != 0x7F)
                continue;

            builder.Append(value, start, i - start);
            start = i + 1;
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
                    builder.Append("\\u{").Append(((int)c).ToString("X", CultureInfo.InvariantCulture)).Append('}');
                    break;
            }
        }

        return builder.Append(value, start, value.Length - start).Append('"');
    }

    /// <summary>
    /// Identifier or quoted string, depending on the content
    /// </summary>
    public static string FormatName(string value) => Internal.Names.IsIdentifier(value) ? value : FormatString(value);

    private static ReadOnlySpan<char> FormatRoundTrip(double value, bool isSingle, Span<char> buffer)
    {
#if NETCOREAPP
        // "R" gives the shortest round-trippable representation since .NET Core 3.0
        var formatted = isSingle
            ? ((float)value).TryFormat(buffer, out var count, "R", CultureInfo.InvariantCulture)
            : value.TryFormat(buffer, out count, "R", CultureInfo.InvariantCulture);

        if (formatted)
            return buffer.Slice(0, count);
#endif
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

        return raw.AsSpan();
    }

    private static int ParseExponent(ReadOnlySpan<char> text)
    {
        var negative = text.Length > 0 && text[0] == '-';
        var start = text.Length > 0 && (text[0] == '-' || text[0] == '+') ? 1 : 0;
        var value = 0;
        for (var i = start; i < text.Length; i++)
            value = (value * 10) + (text[i] - '0');

        return negative ? -value : value;
    }

    private static StringBuilder Append(StringBuilder builder, ReadOnlySpan<char> chars)
    {
#if NETCOREAPP
        return builder.Append(chars);
#else
        foreach (var c in chars)
            builder.Append(c);

        return builder;
#endif
    }

    private static void AppendInteger(StringBuilder builder, long value)
    {
#if NETCOREAPP
        Span<char> buffer = stackalloc char[24];
        if (value.TryFormat(buffer, out var count, default, CultureInfo.InvariantCulture))
        {
            builder.Append(buffer.Slice(0, count));
            return;
        }
#endif
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendInteger(StringBuilder builder, ulong value)
    {
#if NETCOREAPP
        Span<char> buffer = stackalloc char[24];
        if (value.TryFormat(buffer, out var count, default, CultureInfo.InvariantCulture))
        {
            builder.Append(buffer.Slice(0, count));
            return;
        }
#endif
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
    }
}
