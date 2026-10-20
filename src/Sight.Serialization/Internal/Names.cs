using System.Collections.Generic;

namespace Sight.Serialization.Internal;

internal static class Names
{
    public static readonly HashSet<string> ReservedWords = new() { "null", "true", "false", "inf", "nan" };

    public static readonly HashSet<string> TypeKeywords = new()
    {
        "bool", "u8", "u16", "u32", "u64", "s8", "s16", "s32", "s64", "f32", "f64",
        "string", "list", "map", "ref", "extref", "any",
    };

    public static bool IsIdentifier(string? value)
    {
        if (string.IsNullOrEmpty(value) || !IsIdentifierStart(value![0]))
            return false;

        for (var i = 1; i < value.Length; i++)
        {
            if (!IsIdentifierPart(value[i]))
                return false;
        }

        return !ReservedWords.Contains(value);
    }

    public static bool IsQualifiedName(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var part in value!.Split('.'))
        {
            if (!IsIdentifier(part))
                return false;
        }

        return true;
    }

    public static bool IsIdentifierStart(char c) => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '_';

    public static bool IsIdentifierPart(char c) => IsIdentifierStart(c) || c is >= '0' and <= '9';
}
