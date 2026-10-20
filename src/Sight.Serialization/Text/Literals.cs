using System;
using System.Globalization;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Text;

/// <summary>
/// Parsed number literal
/// </summary>
internal readonly struct NumberLiteral
{
    private NumberLiteral(bool isFloat, bool isNegative, ulong magnitude, string floatText, TypeKind? suffix)
    {
        IsFloat = isFloat;
        IsNegative = isNegative;
        Magnitude = magnitude;
        FloatText = floatText;
        Suffix = suffix;
    }

    public bool IsFloat { get; }

    public bool IsNegative { get; }

    /// <summary>
    /// Absolute value of an integer literal
    /// </summary>
    public ulong Magnitude { get; }

    /// <summary>
    /// Text of a float literal, without suffix
    /// </summary>
    public string FloatText { get; }

    public TypeKind? Suffix { get; }

    /// <summary>
    /// Indicates whether the literal is a plain decimal integer (convertible to a float)
    /// </summary>
    public bool IsDecimalInteger => !IsFloat && FloatText.Length > 0;

    public static NumberLiteral Parse(Token token)
    {
        var text = token.Text;
        var isNegative = text.StartsWith("-", StringComparison.Ordinal);
        var body = isNegative ? text.Substring(1) : text;

        if (body.StartsWith("inf", StringComparison.Ordinal) || body.StartsWith("nan", StringComparison.Ordinal))
        {
            var suffix = ParseSuffix(body.Substring(3), token, allowFloat: true, allowInteger: false);
            return new NumberLiteral(true, isNegative, 0, text.Substring(0, text.Length - (body.Length - 3)), suffix);
        }

        if (body.StartsWith("0x", StringComparison.Ordinal) || body.StartsWith("0b", StringComparison.Ordinal))
        {
            var isHex = body[1] == 'x';
            var end = 2;
            while (end < body.Length && (body[end] == '_' || (isHex ? Uri.IsHexDigit(body[end]) : body[end] is '0' or '1')))
                end++;

            var suffix = ParseSuffix(body.Substring(end), token, allowFloat: false, allowInteger: true);
            var digits = body.Substring(2, end - 2).Replace("_", string.Empty);
            ulong magnitude = 0;
            foreach (var c in digits)
            {
                var digit = (ulong)(c <= '9' ? c - '0' : (char.ToLowerInvariant(c) - 'a') + 10);
                var radix = isHex ? 16UL : 2UL;
                if (magnitude > (ulong.MaxValue - digit) / radix)
                    throw Error(token, "Integer literal is too large");

                magnitude = (magnitude * radix) + digit;
            }

            return new NumberLiteral(false, isNegative, magnitude, string.Empty, suffix);
        }

        var index = 0;
        while (index < body.Length && (char.IsDigit(body[index]) || body[index] == '_'))
            index++;

        var isFloat = false;
        if (index < body.Length && body[index] == '.')
        {
            isFloat = true;
            index++;
            while (index < body.Length && (char.IsDigit(body[index]) || body[index] == '_'))
                index++;
        }

        if (index < body.Length && body[index] is 'e' or 'E')
        {
            isFloat = true;
            index++;
            if (index < body.Length && body[index] is '+' or '-')
                index++;

            while (index < body.Length && char.IsDigit(body[index]))
                index++;
        }

        var suffixText = body.Substring(index);
        var number = body.Substring(0, index).Replace("_", string.Empty);
        var kind = ParseSuffix(suffixText, token, allowFloat: true, allowInteger: !isFloat);
        if (isFloat || kind is TypeKind.F32 or TypeKind.F64)
            return new NumberLiteral(true, isNegative, 0, (isNegative ? "-" : string.Empty) + number, kind);

        if (!ulong.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            throw Error(token, "Integer literal is too large");

        return new NumberLiteral(false, isNegative, value, (isNegative ? "-" : string.Empty) + number, kind);
    }

    public double ToDouble()
    {
        if (!IsFloat)
            return IsNegative ? -(double)Magnitude : Magnitude;

        return FloatText switch
        {
            "inf" => double.PositiveInfinity,
            "-inf" => double.NegativeInfinity,
            "nan" or "-nan" => double.NaN,
            _ => double.Parse(FloatText, NumberStyles.Float, CultureInfo.InvariantCulture),
        };
    }

    public float ToSingle()
    {
        if (!IsFloat)
            return float.Parse(FloatText, NumberStyles.Float, CultureInfo.InvariantCulture);

        return FloatText switch
        {
            "inf" => float.PositiveInfinity,
            "-inf" => float.NegativeInfinity,
            "nan" or "-nan" => float.NaN,
            _ => float.Parse(FloatText, NumberStyles.Float, CultureInfo.InvariantCulture),
        };
    }

    private static TypeKind? ParseSuffix(string suffix, Token token, bool allowFloat, bool allowInteger)
    {
        if (suffix.Length == 0)
            return null;

        TypeKind? kind = suffix switch
        {
            "u8" => TypeKind.U8,
            "u16" => TypeKind.U16,
            "u32" => TypeKind.U32,
            "u64" => TypeKind.U64,
            "s8" => TypeKind.S8,
            "s16" => TypeKind.S16,
            "s32" => TypeKind.S32,
            "s64" => TypeKind.S64,
            "f32" => TypeKind.F32,
            "f64" => TypeKind.F64,
            _ => null,
        };

        if (kind == null || (kind.Value.IsFloat() && !allowFloat) || (kind.Value.IsInteger() && !allowInteger))
            throw Error(token, $"Invalid number suffix '{suffix}'");

        return kind;
    }

    private static SerializationException Error(Token token, string message) => new(message, token.Line, token.Column);
}

/// <summary>
/// Conversion of literal tokens to values of a given type
/// </summary>
internal static class Literals
{
    /// <summary>
    /// Convert a scalar literal (keyword, number, string, Base64 excluded) to a value of the expected scalar type
    /// </summary>
    public static DataValue ToScalar(Token token, TypeReference type)
    {
        var kind = type.Kind;
        if (token.Kind == TokenKind.Identifier)
        {
            switch (token.Text)
            {
                case "true" when kind == TypeKind.Bool:
                    return DataScalar.FromBoolean(true);
                case "false" when kind == TypeKind.Bool:
                    return DataScalar.FromBoolean(false);
                case "inf" or "nan" or "inff32" or "inff64" or "nanf32" or "nanf64" when kind.IsFloat():
                    var number = NumberLiteral.Parse(token);
                    CheckSuffix(token, number, kind);
                    return DataScalar.FromDouble(kind, kind == TypeKind.F32 ? number.ToSingle() : number.ToDouble());
            }
        }
        else if (token.Kind == TokenKind.String && kind == TypeKind.String)
        {
            return DataScalar.FromString(token.Value!);
        }
        else if (token.Kind == TokenKind.Number)
        {
            var number = NumberLiteral.Parse(token);
            CheckSuffix(token, number, kind);
            if (kind.IsInteger())
            {
                if (number.IsFloat)
                    throw new SerializationException($"A float literal is not valid for type {type}", token.Line, token.Column);

                return ToInteger(token, number, kind);
            }

            if (kind.IsFloat())
            {
                if (!number.IsFloat && !number.IsDecimalInteger)
                    throw new SerializationException($"A hexadecimal or binary literal is not valid for type {type}", token.Line, token.Column);

                return DataScalar.FromDouble(kind, kind == TypeKind.F32 ? number.ToSingle() : number.ToDouble());
            }
        }

        throw new SerializationException($"{token} is not a valid value for type {type}", token.Line, token.Column);
    }

    /// <summary>
    /// Value of a number literal in an <c>any</c> position
    /// </summary>
    public static DataValue ToAnyNumber(Token token)
    {
        var number = NumberLiteral.Parse(token);
        var kind = number.Suffix ?? (number.IsFloat ? TypeKind.F64 : TypeKind.S64);
        return kind.IsFloat()
            ? DataScalar.FromDouble(kind, kind == TypeKind.F32 ? number.ToSingle() : number.ToDouble())
            : ToInteger(token, number, kind);
    }

    public static DataScalar ToInteger(Token token, NumberLiteral number, TypeKind kind)
    {
        try
        {
            if (number.IsNegative)
            {
                if (number.Magnitude > 1UL << 63)
                    throw new OverflowException();

                return DataScalar.FromInt64(kind, unchecked(-(long)number.Magnitude));
            }

            return DataScalar.FromUInt64(kind, number.Magnitude);
        }
        catch (OverflowException)
        {
            throw new SerializationException($"Value {token.Text} is out of range for {kind.ToKeyword()}", token.Line, token.Column);
        }
    }

    private static void CheckSuffix(Token token, NumberLiteral number, TypeKind kind)
    {
        if (number.Suffix.HasValue && number.Suffix.Value != kind)
            throw new SerializationException($"Suffix {number.Suffix.Value.ToKeyword()} does not match type {kind.ToKeyword()}", token.Line, token.Column);
    }
}
