using Sight.Linq;

namespace Sight.Linq.Tests;

public class InsertAtTests
{
    [Test]
    public void Test_insert_at_middle()
    {
        Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, new[] { 1, 2, 5 }.InsertAt(2, 3, 4));
    }

    [Test]
    public void Test_insert_at_start_and_end()
    {
        var arr = new[] { 1, 2 };

        Assert.AreEqual(new[] { 0, 1, 2 }, arr.InsertAt(0, 0));
        Assert.AreEqual(new[] { 1, 2, 3 }, arr.InsertAt(2, 3));
    }

    [Test]
    public void Test_insert_at_negative_index_throws_immediately()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { 1, 2 }.InsertAt(-1, 0));
    }

    [Test]
    public void Test_insert_at_index_past_the_end_throws_when_enumerated()
    {
        var result = new[] { 1, 2 }.InsertAt(3, 9);

        Assert.Throws<ArgumentOutOfRangeException>(() => result.ToArray());
    }

    [Test]
    public void Test_insert_at_is_not_confused_with_list_insert()
    {
        var list = new List<int> { 1, 2 };

        var result = list.InsertAt(1, 8, 9).ToArray();

        Assert.AreEqual(new[] { 1, 8, 9, 2 }, result);
        Assert.AreEqual(new[] { 1, 2 }, list);
    }

#pragma warning disable CS0618
    [Test]
    public void Test_obsolete_insert_still_works_on_enumerable()
    {
        IEnumerable<int> source = new[] { 1, 4 };

        Assert.AreEqual(new[] { 1, 2, 3, 4 }, source.Insert(1, 2, 3));
    }
#pragma warning restore CS0618
}
