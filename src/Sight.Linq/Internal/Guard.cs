namespace Sight.Linq.Internal;

internal static class Guard
{
    public static void NotNull(object? value, string paramName)
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
    }

    public static void NotReadOnly<T>(ICollection<T> collection)
    {
        if (collection.IsReadOnly)
            throw new NotSupportedException($"The {collection.GetType().Name} is read-only and cannot be modified");
    }
}
