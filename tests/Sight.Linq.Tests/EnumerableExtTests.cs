using Sight.Linq;

namespace Sight.Linq.Tests;

public class EnumerableExtTests
{
    [Test]
    public void Test_append_adds_items_at_the_end()
    {
        Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, new[] { 1, 2, 3 }.Append(4, 5));
    }

    [Test]
    public void Test_distinct_by_key_keeps_first_of_each_key()
    {
        var result = new[] { "one", "two", "three", "four" }.Distinct(x => x.Length);

        Assert.AreEqual(new[] { "one", "three", "four" }, result);
    }

    [Test]
    public void Test_distinct_by_key_uses_comparer()
    {
        var result = new[] { "a", "A", "b" }.Distinct(x => x, StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(new[] { "a", "b" }, result);
    }

    [Test]
    public void Test_except_removes_items()
    {
        Assert.AreEqual(new[] { 1, 3, 5 }, new[] { 1, 2, 3, 4, 5 }.Except(2, 4));
    }

    [Test]
    public void Test_index_of_item()
    {
        var arr = new[] { 1, 2, 3, 4, 3 };

        Assert.AreEqual(2, arr.IndexOf(3));
        Assert.AreEqual(-1, arr.IndexOf(9));
        Assert.AreEqual(1, new[] { "a", "B" }.IndexOf("b", StringComparer.OrdinalIgnoreCase));
    }

    [Test]
    public void Test_index_of_predicate()
    {
        var arr = new[] { 1, 2, 3, 4, 5 };

        Assert.AreEqual(2, arr.IndexOf(x => x > 2));
        Assert.AreEqual(-1, arr.IndexOf(x => x > 9));
        Assert.AreEqual(3, arr.IndexOf((x, i) => i == 3));
    }

    [Test]
    public void Test_is_empty()
    {
        Assert.IsTrue(Array.Empty<int>().IsEmpty());
        Assert.IsFalse(new[] { 1 }.IsEmpty());
    }

    [Test]
    public void Test_to_enumerable_wraps_item()
    {
        Assert.AreEqual(new[] { 3 }, 3.ToEnumerable());
    }

    [Test]
    public void Test_try_get_last_returns_last_match()
    {
        var found = new[] { 1, 2, 3, 4, 5 }.TryGetLast(x => x < 4, out var item);

        Assert.IsTrue(found);
        Assert.AreEqual(3, item);
        Assert.IsFalse(new[] { 1, 2 }.TryGetLast(x => x > 4, out _));
    }

    [Test]
    public void Test_where_not_filters_matching_items()
    {
        var arr = new[] { 1, 2, 3, 4, 5 };

        Assert.AreEqual(new[] { 1, 2 }, arr.WhereNot(x => x > 2));
        Assert.AreEqual(new[] { 1, 2, 3 }, arr.WhereNot((x, i) => i >= 3));
    }

    [Test]
    public void Test_where_not_null_filters_null_items()
    {
        Assert.AreEqual(new[] { "one", "three" }, new[] { "one", null, "three" }.WhereNotNull());
    }
}
