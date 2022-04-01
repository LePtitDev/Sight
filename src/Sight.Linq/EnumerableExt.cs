using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Sight.Linq
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class EnumerableExt
    {
        /// <summary>
        /// Append elements at the end of the collection
        /// </summary>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] items)
        {
            return source.Concat(items);
        }

        /// <summary>
        /// Enumerate elements with unique provided key
        /// </summary>
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keyFunc, IEqualityComparer<TKey>? keyComparer = null)
        {
            var hashSet = new HashSet<TKey>(keyComparer ?? EqualityComparer<TKey>.Default);
            foreach (var item in source)
            {
                if (hashSet.Add(keyFunc(item)))
                    yield return item;
            }
        }

        /// <summary>
        /// Remove elements from the collection
        /// </summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] items)
        {
            return source.Except((IEnumerable<T>)items);
        }

        /// <summary>
        /// Invoke delegate for each element in source
        /// </summary>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> func)
        {
            foreach (var item in source)
            {
                func(item);
                yield return item;
            }
        }

        /// <summary>
        /// Invoke delegate with collection index for each element in source
        /// </summary>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T, int> func)
        {
            var index = 0;
            foreach (var item in source)
            {
                func(item, index++);
                yield return item;
            }
        }

        /// <summary>
        /// Find index of item
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> source, T item, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;
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
        /// Insert elements in the collection
        /// </summary>
        public static IEnumerable<T> Insert<T>(this IEnumerable<T> source, int index, params T[] items)
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

            if (index >= i)
            {
                foreach (var e in items)
                {
                    yield return e;
                }
            }
        }

        /// <summary>
        /// Indicates if the collection is empty
        /// </summary>
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return !source.Any();
        }

        /// <summary>
        /// Indicates if the collection is empty
        /// </summary>
        public static bool IsEmpty<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return !source.Any(predicate);
        }

        /// <summary>
        /// Convert an element to enumerable of one item
        /// </summary>
        public static IEnumerable<T> ToEnumerable<T>(this T item)
        {
            yield return item;
        }

        /// <summary>
        /// Try to found an element that match the predicate (like with <see cref="IDictionary{TKey,TValue}"/>)
        /// </summary>
        public static bool TryGet<T>(this IEnumerable<T> source, Func<T, bool> predicate, [NotNullWhen(true)] out T? item) where T : notnull
        {
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
        /// Try to found the last element that match the predicate (like with <see cref="IDictionary{TKey,TValue}"/>)
        /// </summary>
        public static bool TryGetLast<T>(this IEnumerable<T> source, Func<T, bool> predicate, [NotNullWhen(true)] out T? item) where T : notnull
        {
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
            return source.Where(x => !predicate(x));
        }

        /// <summary>
        /// Filter elements from a predicate
        /// </summary>
        public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
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
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source)
        {
            return source.Where(x => x != null);
        }
    }
}
