using System.Linq;
using Sight.Linq.Internal;

namespace Sight.Linq;

/// <summary>
/// Extension methods for <see cref="ICollection{T}"/>
/// </summary>
public static class CollectionExt
{
    /// <summary>
    /// Add items to collection
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only</exception>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        Guard.NotNull(collection, nameof(collection));
        Guard.NotNull(items, nameof(items));
        Guard.NotReadOnly(collection);
        items = Snapshot(collection, items);

        if (collection is List<T> list)
        {
            list.AddRange(items);
            return;
        }

        if (collection is ISet<T> set)
        {
            set.UnionWith(items);
            return;
        }

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>
    /// Add items to collection
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only</exception>
    public static void AddRange<T>(this ICollection<T> collection, params T[] items)
    {
        AddRange(collection, (IEnumerable<T>)items);
    }

    /// <summary>
    /// Insert items to collection at specified index
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than the number of items</exception>
    public static void InsertRange<T>(this IList<T> collection, int index, IEnumerable<T> items)
    {
        Guard.NotNull(collection, nameof(collection));
        Guard.NotNull(items, nameof(items));
        Guard.NotReadOnly(collection);
        if (index < 0 || index > collection.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within the bounds of the collection");

        items = Snapshot(collection, items);

        if (collection is List<T> list)
        {
            list.InsertRange(index, items);
            return;
        }

        foreach (var item in items)
        {
            collection.Insert(index++, item);
        }
    }

    /// <summary>
    /// Insert items to collection at specified index
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than the number of items</exception>
    public static void InsertRange<T>(this IList<T> collection, int index, params T[] items)
    {
        InsertRange(collection, index, (IEnumerable<T>)items);
    }

    /// <summary>
    /// Remove each duplicated items from collection
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only</exception>
    public static void RemoveAll<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        Guard.NotNull(collection, nameof(collection));
        Guard.NotNull(items, nameof(items));
        Guard.NotReadOnly(collection);
        items = Snapshot(collection, items);

        if (collection is ISet<T> set)
        {
            set.ExceptWith(items);
            return;
        }

        if (collection is List<T> list)
        {
            var removed = new HashSet<T>(items);
            if (removed.Count > 0)
                list.RemoveAll(removed.Contains);

            return;
        }

        foreach (var item in items)
        {
            while (collection.Remove(item))
            {
            }
        }
    }

    /// <summary>
    /// Remove each duplicated items from collection
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only</exception>
    public static void RemoveAll<T>(this ICollection<T> collection, params T[] items)
    {
        RemoveAll(collection, (IEnumerable<T>)items);
    }

    /// <summary>
    /// Remove items from collection with predicate
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only</exception>
    public static void RemoveAll<T>(this ICollection<T> collection, Func<T, bool> predicate)
    {
        Guard.NotNull(collection, nameof(collection));
        Guard.NotNull(predicate, nameof(predicate));
        Guard.NotReadOnly(collection);

        if (collection is List<T> list)
        {
            list.RemoveAll(new Predicate<T>(predicate));
            return;
        }

        if (collection is HashSet<T> hashSet)
        {
            hashSet.RemoveWhere(new Predicate<T>(predicate));
            return;
        }

        if (collection is IList<T> items)
        {
            for (var i = items.Count - 1; i >= 0; i--)
            {
                if (predicate(items[i]))
                    items.RemoveAt(i);
            }

            return;
        }

        foreach (var item in collection.ToArray())
        {
            if (predicate(item))
                collection.Remove(item);
        }
    }

    /// <summary>
    /// Remove item at index. The collection must be an <see cref="IList{T}"/>, as an index is meaningless for other collections
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only or is not an <see cref="IList{T}"/></exception>
    public static void RemoveAt<T>(this ICollection<T> collection, int index)
    {
        Guard.NotNull(collection, nameof(collection));
        if (collection is not IList<T> list)
            throw new NotSupportedException($"Cannot remove an item at an index from a {collection.GetType().Name}: it is not an IList<T>");

        Guard.NotReadOnly(collection);
        list.RemoveAt(index);
    }

    /// <summary>
    /// Remove items from collection
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only</exception>
    public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        Guard.NotNull(collection, nameof(collection));
        Guard.NotNull(items, nameof(items));
        Guard.NotReadOnly(collection);
        items = Snapshot(collection, items);

        if (collection is ISet<T> set)
        {
            set.ExceptWith(items);
            return;
        }

        foreach (var item in items)
        {
            collection.Remove(item);
        }
    }

    /// <summary>
    /// Remove items from collection
    /// </summary>
    /// <exception cref="NotSupportedException"><paramref name="collection"/> is read-only</exception>
    public static void RemoveRange<T>(this ICollection<T> collection, params T[] items)
    {
        RemoveRange(collection, (IEnumerable<T>)items);
    }

    /// <summary>
    /// Copy <paramref name="items"/> when it is the collection being modified, as it would otherwise be changed during its enumeration
    /// </summary>
    private static IEnumerable<T> Snapshot<T>(ICollection<T> collection, IEnumerable<T> items)
    {
        return ReferenceEquals(collection, items) ? items.ToArray() : items;
    }
}
