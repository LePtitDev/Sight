using Sight.Linq;

namespace Sight.Linq.Tests;

public class CollectionExtTests
{
    [Test]
    public void Test_add_range_adds_items()
    {
        ICollection<int> collection = new HashSet<int> { 1 };

        collection.AddRange(2, 3);
        collection.AddRange(new[] { 4 }.AsEnumerable());

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, collection);
    }

    [Test]
    public void Test_insert_range_inserts_at_index()
    {
        var list = new List<int> { 1, 4, 5 };

        list.InsertRange(1, 2, 3);

        Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list);
    }

    [Test]
    public void Test_insert_range_works_on_custom_list()
    {
        IList<int> list = new System.Collections.ObjectModel.Collection<int> { 1, 4, 5 };

        list.InsertRange(1, 2, 3);

        Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list);
    }

    [Test]
    public void Test_remove_all_items_removes_every_occurrence()
    {
        ICollection<int> collection = new List<int> { 1, 1, 2, 3, 5, 5 };

        collection.RemoveAll(1, 5);

        Assert.AreEqual(new[] { 2, 3 }, collection);
    }

    [Test]
    public void Test_remove_range_removes_first_occurrence_of_each()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3, 3, 4, 5 };

        collection.RemoveRange(3, 5);

        Assert.AreEqual(new[] { 1, 2, 3, 4 }, collection);
    }
}
