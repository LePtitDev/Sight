using System.Diagnostics.CodeAnalysis;

namespace Sight.Linq
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class Enumerable
    {
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
    }
}
