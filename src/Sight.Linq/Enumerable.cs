using System.Diagnostics.CodeAnalysis;

namespace Sight.Linq
{
    public static class Enumerable
    {
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
