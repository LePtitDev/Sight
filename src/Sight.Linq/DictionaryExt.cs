using System.Diagnostics.CodeAnalysis;
using Sight.Linq.Internal;

namespace Sight.Linq;

/// <summary>
/// Extension methods for <see cref="IDictionary{TKey,TValue}"/>
/// </summary>
public static class DictionaryExt
{
    /// <summary>
    /// Get the value of a key, or the value of <paramref name="defaultValue"/> if the key is not found
    /// </summary>
    public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)
    {
        Guard.NotNull(dictionary, nameof(dictionary));
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Get the value of a key, or the value of <paramref name="defaultValue"/> if the key is not found.
    /// This overload avoids the ambiguity with <c>CollectionExtensions.GetValueOrDefault</c> on types that are both <see cref="IDictionary{TKey,TValue}"/> and <see cref="IReadOnlyDictionary{TKey,TValue}"/>
    /// </summary>
    public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)
        where TKey : notnull
    {
        return GetValueOrDefault((IDictionary<TKey, TValue>)dictionary, key, defaultValue);
    }

    /// <summary>
    /// Get the value of a key, or add and return <paramref name="value"/> if the key is not found
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="dictionary"/> is read-only and the key is not found</exception>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        Guard.NotNull(dictionary, nameof(dictionary));
        if (dictionary.TryGetValue(key, out var existing))
            return existing;

        Guard.NotReadOnly(dictionary);
        dictionary.Add(key, value);
        return value;
    }

    /// <summary>
    /// Get the value of a key, or add and return the value created by <paramref name="valueFactory"/> if the key is not found.
    /// The factory is invoked only if the key is not found
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="dictionary"/> is read-only and the key is not found</exception>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
    {
        Guard.NotNull(dictionary, nameof(dictionary));
        Guard.NotNull(valueFactory, nameof(valueFactory));
        if (dictionary.TryGetValue(key, out var existing))
            return existing;

        Guard.NotReadOnly(dictionary);
        var value = valueFactory(key);
        dictionary.Add(key, value);
        return value;
    }
}
