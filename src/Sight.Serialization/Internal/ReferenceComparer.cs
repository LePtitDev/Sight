using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Sight.Serialization.Internal;

internal sealed class ReferenceComparer<T> : IEqualityComparer<T>
    where T : class
{
    public static readonly ReferenceComparer<T> Instance = new();

    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

    public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
}
