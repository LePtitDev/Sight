﻿using System.Linq;

namespace Sight.Linq
{
    /// <summary>
    /// Extension methods for <see cref="ICollection{T}"/>
    /// </summary>
    public static class CollectionExt
    {
        /// <summary>
        /// Add items to collection
        /// </summary>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            if (collection is List<T> list)
            {
                list.AddRange(items);
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
        public static void AddRange<T>(this ICollection<T> collection, params T[] items)
        {
            AddRange(collection, (IEnumerable<T>)items);
        }

        /// <summary>
        /// Insert items to collection at specified index
        /// </summary>
        public static void InsertRange<T>(this IList<T> collection, int index, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Insert(index++, item);
            }
        }

        /// <summary>
        /// Insert items to collection at specified index
        /// </summary>
        public static void InsertRange<T>(this IList<T> collection, int index, params T[] items)
        {
            InsertRange(collection, index, (IEnumerable<T>)items);
        }

        /// <summary>
        /// Remove each duplicated items from collection
        /// </summary>
        public static void RemoveAll<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
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
        public static void RemoveAll<T>(this ICollection<T> collection, params T[] items)
        {
            RemoveAll(collection, (IEnumerable<T>)items);
        }

        /// <summary>
        /// Remove items from collection with predicate
        /// </summary>
        public static void RemoveAll<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            if (collection is List<T> list)
            {
                list.RemoveAll(predicate);
                return;
            }

            foreach (var item in collection.ToArray())
            {
                if (predicate(item))
                    collection.Remove(item);
            }
        }

        /// <summary>
        /// Remove item at index
        /// </summary>
        public static void RemoveAt<T>(this ICollection<T> collection, int index)
        {
            if (collection is IList<T> list)
            {
                list.RemoveAt(index);
                return;
            }

            var item = collection.ElementAt(index);
            if (item != null)
                collection.Remove(item);
        }

        /// <summary>
        /// Remove items from collection
        /// </summary>
        public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Remove(item);
            }
        }

        /// <summary>
        /// Remove items from collection
        /// </summary>
        public static void RemoveRange<T>(this ICollection<T> collection, params T[] items)
        {
            RemoveRange(collection, (IEnumerable<T>)items);
        }
    }
}
