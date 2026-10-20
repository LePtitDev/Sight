using System.Collections.ObjectModel;
using Sight.Linq;

namespace Sight.Linq.Tests;

public class FastPathTests
{
    /// <summary>Only an IReadOnlyList: enumerating it through GetEnumerator is a test failure</summary>
    private sealed class ReadOnlyListOnly<T>(params T[] items) : IReadOnlyList<T>
    {
        public int Count => items.Length;

        public T this[int index] => items[index];

        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("Enumerator must not be used");

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Test]
    public void Test_searches_on_lists_match_the_enumerable_results()
    {
        var items = new[] { 1, 4, 3, 4, 5, 4 };
        var sources = new IEnumerable<int>[] { items, new List<int>(items), new Collection<int>(items), new ReadOnlyListOnly<int>(items) };

        foreach (var source in sources)
        {
            Assert.AreEqual(1, source.IndexOf(4), source.GetType().Name);
            Assert.AreEqual(-1, source.IndexOf(9), source.GetType().Name);
            Assert.AreEqual(2, source.IndexOf(x => x == 3), source.GetType().Name);
            Assert.AreEqual(4, source.IndexOf((x, i) => i == 4), source.GetType().Name);
            Assert.AreEqual(5, source.LastIndexOf(4), source.GetType().Name);
            Assert.AreEqual(-1, source.LastIndexOf(9), source.GetType().Name);
            Assert.AreEqual(5, source.LastIndexOf(x => x == 4), source.GetType().Name);
            Assert.AreEqual(3, source.LastIndexOf((x, i) => i == 3), source.GetType().Name);

            Assert.IsTrue(source.TryGetFirst(x => x > 3, out var first), source.GetType().Name);
            Assert.AreEqual(4, first);
            Assert.IsTrue(source.TryGetLast(x => x < 5, out var last), source.GetType().Name);
            Assert.AreEqual(4, last);
            Assert.IsFalse(source.TryGetFirst(x => x > 9, out _), source.GetType().Name);
            Assert.IsFalse(source.TryGetLast(x => x > 9, out _), source.GetType().Name);
        }
    }

    [Test]
    public void Test_index_of_with_comparer_on_list()
    {
        var list = new List<string> { "a", "B", "b" };

        Assert.AreEqual(1, list.IndexOf("b", StringComparer.OrdinalIgnoreCase));
        Assert.AreEqual(2, list.IndexOf("b"));
        Assert.AreEqual(2, list.LastIndexOf("b", StringComparer.OrdinalIgnoreCase));
    }

    [Test]
    public void Test_try_get_last_stops_at_first_match_from_the_end()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var visited = 0;

        list.TryGetLast(x =>
        {
            visited++;
            return x < 5;
        }, out var item);

        Assert.AreEqual(4, item);
        Assert.AreEqual(2, visited);
    }

    [Test]
    public void Test_set_fast_paths_keep_semantics()
    {
        ICollection<int> set = new HashSet<int> { 1, 2, 3, 4 };
        ICollection<int> sorted = new SortedSet<int> { 1, 2, 3, 4 };

        foreach (var collection in new[] { set, sorted })
        {
            collection.AddRange(4, 5, 6);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, collection);

            collection.RemoveRange(1, 6);
            CollectionAssert.AreEquivalent(new[] { 2, 3, 4, 5 }, collection);

            collection.RemoveAll(2, 9);
            CollectionAssert.AreEquivalent(new[] { 3, 4, 5 }, collection);

            collection.RemoveAll(x => x > 3);
            CollectionAssert.AreEquivalent(new[] { 3 }, collection);
        }
    }

    [Test]
    public void Test_remove_all_predicate_on_custom_list_keeps_order_and_removes_every_match()
    {
        ICollection<int> collection = new Collection<int> { 1, 2, 2, 3, 4, 4, 5 };

        collection.RemoveAll(x => x % 2 == 0);

        Assert.AreEqual(new[] { 1, 3, 5 }, collection);
    }

    [Test]
    public void Test_remove_all_items_on_list_removes_every_occurrence_including_null()
    {
        ICollection<string?> collection = new List<string?> { "a", null, "b", null, "a" };

        collection.RemoveAll(null, "a");

        Assert.AreEqual(new[] { "b" }, collection);
    }

    [Test]
    public void Test_insert_range_on_list_and_bounds()
    {
        var list = new List<int> { 1, 5 };

        list.InsertRange(1, new[] { 2, 3, 4 }.AsEnumerable());

        Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(6, 0));
        list.InsertRange(5, 6);
        Assert.AreEqual(6, list[^1]);
    }

    [Test]
    [CancelAfter(10_000)]
    public void Test_bulk_operations_on_large_lists_are_linear()
    {
        const int size = 200_000;
        ICollection<int> list = new List<int>(Enumerable.Range(0, size));
        var toRemove = Enumerable.Range(0, size).Where(x => x % 2 == 0).ToArray();

        list.RemoveAll(toRemove);

        Assert.AreEqual(size / 2, list.Count);

        var target = new List<int>();
        target.InsertRange(0, Enumerable.Range(0, size));
        target.InsertRange(0, Enumerable.Range(0, size));

        Assert.AreEqual(size * 2, target.Count);
    }

    [Test]
    public void Test_to_enumerable_is_a_collection()
    {
        var result = 3.ToEnumerable();

        Assert.IsInstanceOf<ICollection<int>>(result);
        Assert.AreEqual(new[] { 3 }, result);
    }
}
