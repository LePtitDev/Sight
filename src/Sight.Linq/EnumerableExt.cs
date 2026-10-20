using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Sight.Linq.Internal;

namespace Sight.Linq;

/// <summary>
/// Extension methods for <see cref="IEnumerable{T}"/>
/// </summary>
public static class EnumerableExt
{
    /// <summary>
    /// Append elements at the end of the collection.
    /// Beware that on an <c>IEnumerable&lt;object&gt;</c>, appending an <c>object[]</c> appends each of its elements, not the array itself
    /// </summary>
    public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] items)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(items, nameof(items));
        return source.Concat(items);
    }

    /// <summary>
    /// Enumerate elements with unique provided key
    /// </summary>
    public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keyFunc, IEqualityComparer<TKey>? keyComparer = null)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(keyFunc, nameof(keyFunc));
#if NET6_0_OR_GREATER
        return source.DistinctBy(keyFunc, keyComparer);
#else
        return DistinctIterator(source, keyFunc, keyComparer);
#endif
    }

#if !NET6_0_OR_GREATER
    private static IEnumerable<TSource> DistinctIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keyFunc, IEqualityComparer<TKey>? keyComparer)
    {
        var hashSet = new HashSet<TKey>(keyComparer ?? EqualityComparer<TKey>.Default);
        foreach (var item in source)
        {
            if (hashSet.Add(keyFunc(item)))
                yield return item;
        }
    }
#endif

    /// <summary>
    /// Remove elements from the collection. Unlike <see cref="Enumerable.Except{TSource}(IEnumerable{TSource},IEnumerable{TSource})"/>, duplicates of the remaining elements are kept
    /// </summary>
    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] items)
    {
        return Except(source, null, items);
    }

    /// <summary>
    /// Remove elements from the collection. Unlike <see cref="Enumerable.Except{TSource}(IEnumerable{TSource},IEnumerable{TSource})"/>, duplicates of the remaining elements are kept
    /// </summary>
    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer, params T[] items)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(items, nameof(items));
        var excluded = new HashSet<T>(items, comparer ?? EqualityComparer<T>.Default);
        return source.Where(x => !excluded.Contains(x));
    }

    /// <summary>
    /// Invoke delegate for each element in source (eagerly)
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(action, nameof(action));
        foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// Invoke delegate with collection index for each element in source (eagerly)
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(action, nameof(action));
        var index = 0;
        foreach (var item in source)
        {
            action(item, index++);
        }
    }

    /// <summary>
    /// Find index of item
    /// </summary>
    public static int IndexOf<T>(this IEnumerable<T> source, T item, IEqualityComparer<T>? comparer = null)
    {
        Guard.NotNull(source, nameof(source));
        if (source is IList<T> list)
        {
            if (comparer == null || ReferenceEquals(comparer, EqualityComparer<T>.Default))
                return list.IndexOf(item);

            for (var i = 0; i < list.Count; i++)
            {
                if (comparer.Equals(list[i], item))
                    return i;
            }

            return -1;
        }

        comparer ??= EqualityComparer<T>.Default;
        if (source is IReadOnlyList<T> readOnlyList)
        {
            for (var i = 0; i < readOnlyList.Count; i++)
            {
                if (comparer.Equals(readOnlyList[i], item))
                    return i;
            }

            return -1;
        }

        var index = 0;
        foreach (var e in source)
        {
            if (comparer.Equals(e, item))
                return index;

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Find index of an item that pass predicate
    /// </summary>
    public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));
        if (source is IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                    return i;
            }

            return -1;
        }

        if (source is IReadOnlyList<T> readOnlyList)
        {
            for (var i = 0; i < readOnlyList.Count; i++)
            {
                if (predicate(readOnlyList[i]))
                    return i;
            }

            return -1;
        }

        var index = 0;
        foreach (var item in source)
        {
            if (predicate(item))
                return index;

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Find index of an item that pass predicate
    /// </summary>
    public static int IndexOf<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));
        if (source is IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (predicate(list[i], i))
                    return i;
            }

            return -1;
        }

        if (source is IReadOnlyList<T> readOnlyList)
        {
            for (var i = 0; i < readOnlyList.Count; i++)
            {
                if (predicate(readOnlyList[i], i))
                    return i;
            }

            return -1;
        }

        var index = 0;
        foreach (var item in source)
        {
            if (predicate(item, index))
                return index;

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Find index of the last occurrence of an item
    /// </summary>
    public static int LastIndexOf<T>(this IEnumerable<T> source, T item, IEqualityComparer<T>? comparer = null)
    {
        Guard.NotNull(source, nameof(source));
        comparer ??= EqualityComparer<T>.Default;
        if (source is IList<T> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (comparer.Equals(list[i], item))
                    return i;
            }

            return -1;
        }

        if (source is IReadOnlyList<T> readOnlyList)
        {
            for (var i = readOnlyList.Count - 1; i >= 0; i--)
            {
                if (comparer.Equals(readOnlyList[i], item))
                    return i;
            }

            return -1;
        }

        var index = 0;
        var result = -1;
        foreach (var e in source)
        {
            if (comparer.Equals(e, item))
                result = index;

            index++;
        }

        return result;
    }

    /// <summary>
    /// Find index of the last item that pass predicate
    /// </summary>
    public static int LastIndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));
        if (source is IList<T> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                    return i;
            }

            return -1;
        }

        if (source is IReadOnlyList<T> readOnlyList)
        {
            for (var i = readOnlyList.Count - 1; i >= 0; i--)
            {
                if (predicate(readOnlyList[i]))
                    return i;
            }

            return -1;
        }

        var index = 0;
        var result = -1;
        foreach (var item in source)
        {
            if (predicate(item))
                result = index;

            index++;
        }

        return result;
    }

    /// <summary>
    /// Find index of the last item that pass predicate
    /// </summary>
    public static int LastIndexOf<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));
        if (source is IList<T> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i], i))
                    return i;
            }

            return -1;
        }

        if (source is IReadOnlyList<T> readOnlyList)
        {
            for (var i = readOnlyList.Count - 1; i >= 0; i--)
            {
                if (predicate(readOnlyList[i], i))
                    return i;
            }

            return -1;
        }

        var index = 0;
        var result = -1;
        foreach (var item in source)
        {
            if (predicate(item, index))
                result = index;

            index++;
        }

        return result;
    }

    /// <summary>
    /// Find index of the last occurrence of an item
    /// </summary>
    [Obsolete("Use LastIndexOf")]
    public static int IndexOfLast<T>(this IEnumerable<T> source, T item, IEqualityComparer<T>? comparer = null)
    {
        return LastIndexOf(source, item, comparer);
    }

    /// <summary>
    /// Find index of the last item that pass predicate
    /// </summary>
    [Obsolete("Use LastIndexOf")]
    public static int IndexOfLast<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return LastIndexOf(source, predicate);
    }

    /// <summary>
    /// Find index of the last item that pass predicate
    /// </summary>
    [Obsolete("Use LastIndexOf")]
    public static int IndexOfLast<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
    {
        return LastIndexOf(source, predicate);
    }

    /// <summary>
    /// Insert elements in the collection.
    /// The result is lazy: an <paramref name="index"/> greater than the number of elements throws an <see cref="ArgumentOutOfRangeException"/> at the end of the enumeration
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative</exception>
    public static IEnumerable<T> InsertAt<T>(this IEnumerable<T> source, int index, params T[] items)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(items, nameof(items));
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index must not be negative");

        return InsertAtIterator(source, index, items);
    }

    private static IEnumerable<T> InsertAtIterator<T>(IEnumerable<T> source, int index, T[] items)
    {
        var i = 0;
        foreach (var item in source)
        {
            if (index == i++)
            {
                foreach (var e in items)
                {
                    yield return e;
                }
            }

            yield return item;
        }

        if (index > i)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index is greater than the number of elements");

        if (index == i)
        {
            foreach (var e in items)
            {
                yield return e;
            }
        }
    }

    /// <summary>
    /// Insert elements in the collection
    /// </summary>
    [Obsolete("Use InsertAt: this extension is shadowed by IList<T>.Insert and silently differs from it.")]
    public static IEnumerable<T> Insert<T>(this IEnumerable<T> source, int index, params T[] items)
    {
        return InsertAt(source, index, items);
    }

    /// <summary>
    /// Indicates if the collection is empty
    /// </summary>
    public static bool IsEmpty<T>(this IEnumerable<T> source)
    {
        Guard.NotNull(source, nameof(source));
        return !source.Any();
    }

    /// <summary>
    /// Indicates if the collection is <c>null</c> or empty
    /// </summary>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? source)
    {
        return source switch
        {
            null => true,
            ICollection<T> collection => collection.Count == 0,
            IReadOnlyCollection<T> collection => collection.Count == 0,
            _ => !source.Any(),
        };
    }

    /// <summary>
    /// Indicates if no element of the collection pass the predicate
    /// </summary>
    [Obsolete("Use None")]
    public static bool IsEmpty<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        return None(source, predicate);
    }

    /// <summary>
    /// Indicates if no element of the collection pass the predicate
    /// </summary>
    public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));
        return !source.Any(predicate);
    }

    /// <summary>
    /// Lazily invoke delegate for each element while the result is enumerated
    /// </summary>
    public static IEnumerable<T> Tap<T>(this IEnumerable<T> source, Action<T> action)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(action, nameof(action));
        return TapIterator(source, action);
    }

    private static IEnumerable<T> TapIterator<T>(IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
            yield return item;
        }
    }

    /// <summary>
    /// Lazily invoke delegate with collection index for each element while the result is enumerated
    /// </summary>
    public static IEnumerable<T> Tap<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(action, nameof(action));
        return TapIterator(source, action);
    }

    private static IEnumerable<T> TapIterator<T>(IEnumerable<T> source, Action<T, int> action)
    {
        var index = 0;
        foreach (var item in source)
        {
            action(item, index++);
            yield return item;
        }
    }

    /// <summary>
    /// Enumerate each pair of consecutive elements. A source with less than two elements gives no pair
    /// </summary>
    public static IEnumerable<(T Previous, T Current)> Pairwise<T>(this IEnumerable<T> source)
    {
        Guard.NotNull(source, nameof(source));
        return PairwiseIterator(source, (previous, current) => (previous, current));
    }

    /// <summary>
    /// Enumerate the result of a selector applied to each pair of consecutive elements. A source with less than two elements gives no pair
    /// </summary>
    public static IEnumerable<TResult> Pairwise<T, TResult>(this IEnumerable<T> source, Func<T, T, TResult> selector)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(selector, nameof(selector));
        return PairwiseIterator(source, selector);
    }

    private static IEnumerable<TResult> PairwiseIterator<T, TResult>(IEnumerable<T> source, Func<T, T, TResult> selector)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            yield break;

        var previous = enumerator.Current;
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            yield return selector(previous, current);
            previous = current;
        }
    }

    /// <summary>
    /// Convert an element to enumerable of one item
    /// </summary>
    public static IEnumerable<T> ToEnumerable<T>(this T item)
    {
        return new[] { item };
    }

    /// <summary>
    /// Try to found the first element that match the predicate (like with <see cref="IDictionary{TKey,TValue}"/>)
    /// </summary>
    public static bool TryGetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, [MaybeNullWhen(false)] out T item)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));
        if (source is IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var elem = list[i];
                if (predicate(elem))
                {
                    item = elem;
                    return true;
                }
            }

            item = default;
            return false;
        }

        if (source is IReadOnlyList<T> readOnlyList)
        {
            for (var i = 0; i < readOnlyList.Count; i++)
            {
                var elem = readOnlyList[i];
                if (predicate(elem))
                {
                    item = elem;
                    return true;
                }
            }

            item = default;
            return false;
        }

        foreach (var elem in source)
        {
            if (predicate(elem))
            {
                item = elem;
                return true;
            }
        }

        item = default;
        return false;
    }

    /// <summary>
    /// Try to found the first element that match the predicate (like with <see cref="IDictionary{TKey,TValue}"/>)
    /// </summary>
    [Obsolete("Use TryGetFirst")]
    public static bool TryGet<T>(this IEnumerable<T> source, Func<T, bool> predicate, [MaybeNullWhen(false)] out T item)
    {
        return TryGetFirst(source, predicate, out item);
    }

    /// <summary>
    /// Try to found the last element that match the predicate (like with <see cref="IDictionary{TKey,TValue}"/>)
    /// </summary>
    public static bool TryGetLast<T>(this IEnumerable<T> source, Func<T, bool> predicate, [MaybeNullWhen(false)] out T item)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));
        if (source is IReadOnlyList<T> readOnlyList)
        {
            for (var i = readOnlyList.Count - 1; i >= 0; i--)
            {
                var elem = readOnlyList[i];
                if (predicate(elem))
                {
                    item = elem;
                    return true;
                }
            }

            item = default;
            return false;
        }

        if (source is IList<T> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var elem = list[i];
                if (predicate(elem))
                {
                    item = elem;
                    return true;
                }
            }

            item = default;
            return false;
        }

        item = default;
        var found = false;
        foreach (var elem in source)
        {
            if (predicate(elem))
            {
                item = elem;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// Filter elements from a predicate
    /// </summary>
    public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));
        return source.Where(x => !predicate(x));
    }

    /// <summary>
    /// Filter elements from a predicate
    /// </summary>
    public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
    {
        Guard.NotNull(source, nameof(source));
        Guard.NotNull(predicate, nameof(predicate));
        return WhereNotIterator(source, predicate);
    }

    private static IEnumerable<T> WhereNotIterator<T>(IEnumerable<T> source, Func<T, int, bool> predicate)
    {
        var index = 0;
        foreach (var item in source)
        {
            if (!predicate(item, index++))
                yield return item;
        }
    }

    /// <summary>
    /// Filter null elements
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        Guard.NotNull(source, nameof(source));
        return source.Where(x => x != null)!;
    }

    /// <summary>
    /// Filter null elements
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        Guard.NotNull(source, nameof(source));
        return WhereNotNullIterator(source);
    }

    private static IEnumerable<T> WhereNotNullIterator<T>(IEnumerable<T?> source) where T : struct
    {
        foreach (var item in source)
        {
            if (item.HasValue)
                yield return item.Value;
        }
    }
}
