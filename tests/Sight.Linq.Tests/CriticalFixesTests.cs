using System.Collections.ObjectModel;
using Sight.Linq;

namespace Sight.Linq.Tests;

public class CriticalFixesTests
{
    [Test]
    public void Test_remove_at_removes_null_item()
    {
        ICollection<string?> collection = new Collection<string?> { "a", null };

        collection.RemoveAt(1);

        Assert.AreEqual(new[] { "a" }, collection);
    }

    [Test]
    public void Test_remove_at_removes_the_item_at_index_not_first_duplicate()
    {
        ICollection<int> collection = new Collection<int> { 7, 1, 7 };

        collection.RemoveAt(2);

        Assert.AreEqual(new[] { 7, 1 }, collection);
    }

    [Test]
    public void Test_remove_at_on_non_list_throws()
    {
        ICollection<int> collection = new HashSet<int> { 1, 2 };

        Assert.Throws<NotSupportedException>(() => collection.RemoveAt(0));
    }

    [Test]
    public void Test_where_not_null_strips_reference_nullability()
    {
        IEnumerable<string> result = new string?[] { "one", null, "three" }.WhereNotNull();

        Assert.AreEqual(new[] { "one", "three" }, result);
    }

    [Test]
    public void Test_where_not_null_unwraps_nullable_values()
    {
        IEnumerable<int> result = new int?[] { 1, null, 3 }.WhereNotNull();

        Assert.AreEqual(new[] { 1, 3 }, result);
    }

    [Test]
    public void Test_except_keeps_duplicates_of_remaining_items()
    {
        Assert.AreEqual(new[] { 1, 1, 2 }, new[] { 1, 1, 2, 3 }.Except(3));
    }

    [Test]
    public void Test_except_uses_comparer()
    {
        var result = new[] { "a", "B", "b", "c" }.Except(StringComparer.OrdinalIgnoreCase, "b");

        Assert.AreEqual(new[] { "a", "c" }, result);
    }

    [Test]
    public void Test_except_with_no_items_returns_everything()
    {
        Assert.AreEqual(new[] { 1, 1 }, new[] { 1, 1 }.Except());
    }
}
