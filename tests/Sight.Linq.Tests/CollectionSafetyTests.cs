using System.Collections.ObjectModel;
using Sight.Linq;

namespace Sight.Linq.Tests;

public class CollectionSafetyTests
{
    /// <summary>A collection without version checks, that would enumerate forever while being appended to</summary>
    private sealed class UncheckedCollection<T> : ICollection<T>
    {
        private readonly List<T> _items = [];

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public void Add(T item) => _items.Add(item);

        public void Clear() => _items.Clear();

        public bool Contains(T item) => _items.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public bool Remove(T item) => _items.Remove(item);

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _items.Count; i++)
            {
                yield return _items[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Test]
    public void Test_add_range_of_itself_duplicates_items()
    {
        ICollection<int> list = new List<int> { 1, 2 };
        ICollection<int> custom = new UncheckedCollection<int> { 1, 2 };

        list.AddRange((IEnumerable<int>)list);
        custom.AddRange((IEnumerable<int>)custom);

        Assert.AreEqual(new[] { 1, 2, 1, 2 }, list);
        Assert.AreEqual(new[] { 1, 2, 1, 2 }, custom);
    }

    [Test]
    public void Test_remove_range_of_itself_empties_the_collection()
    {
        ICollection<int> collection = new Collection<int> { 1, 2, 3 };

        collection.RemoveRange((IEnumerable<int>)collection);

        Assert.IsEmpty(collection);
    }

    [Test]
    public void Test_remove_all_of_itself_empties_the_collection()
    {
        ICollection<int> collection = new Collection<int> { 1, 1, 2 };

        collection.RemoveAll((IEnumerable<int>)collection);

        Assert.IsEmpty(collection);
    }

    [Test]
    public void Test_insert_range_of_itself_duplicates_items()
    {
        IList<int> list = new Collection<int> { 1, 2 };

        list.InsertRange(1, (IEnumerable<int>)list);

        Assert.AreEqual(new[] { 1, 1, 2, 2 }, list);
    }

    [Test]
    public void Test_read_only_collections_throw_a_clear_exception()
    {
        ICollection<int> array = new[] { 1, 2, 3 };
        ICollection<int> readOnly = new ReadOnlyCollection<int>([1, 2, 3]);

        foreach (var collection in new[] { array, readOnly })
        {
            Assert.Throws<NotSupportedException>(() => collection.AddRange(4));
            Assert.Throws<NotSupportedException>(() => collection.RemoveRange(1));
            Assert.Throws<NotSupportedException>(() => collection.RemoveAll(1));
            Assert.Throws<NotSupportedException>(() => collection.RemoveAll(x => x > 1));
            Assert.Throws<NotSupportedException>(() => collection.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => ((IList<int>)collection).InsertRange(0, 9));
        }

        StringAssert.Contains("read-only", Assert.Throws<NotSupportedException>(() => array.AddRange(4))!.Message);
    }
}
