using Sight.Linq;

namespace Sight.Linq.Tests;

public class RemoveAllPredicateTests
{
    [Test]
    public void Test_remove_all_predicate_on_list_typed_as_collection()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };

        collection.RemoveAll((Func<int, bool>)(x => x > 1));

        Assert.AreEqual(new[] { 1 }, collection);
    }

    [Test]
    public void Test_remove_all_predicate_on_non_list_collection()
    {
        ICollection<int> collection = new LinkedList<int>(new[] { 1, 2, 3, 4 });

        collection.RemoveAll(x => x % 2 == 0);

        Assert.AreEqual(new[] { 1, 3 }, collection);
    }
}
