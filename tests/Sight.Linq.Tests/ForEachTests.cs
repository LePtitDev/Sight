using Sight.Linq;

namespace Sight.Linq.Tests;

public class ForEachTests
{
    [Test]
    public void Test_for_each_is_eager()
    {
        IEnumerable<int> source = new[] { 1, 2, 3, 4, 5 };
        var counter = 0;

        source.ForEach(x => counter += x);

        Assert.AreEqual(15, counter);
    }

    [Test]
    public void Test_for_each_with_index_is_eager()
    {
        IEnumerable<string> source = new[] { "a", "b", "c" };
        var result = new List<string>();

        source.ForEach((x, i) => result.Add(x + i));

        Assert.AreEqual(new[] { "a0", "b1", "c2" }, result);
    }

    [Test]
    public void Test_tap_is_lazy_and_returns_items()
    {
        var counter = 0;

        var result = new[] { 1, 2, 3 }.Tap(x => counter += x);

        Assert.AreEqual(0, counter);
        Assert.AreEqual(new[] { 1, 2, 3 }, result.ToArray());
        Assert.AreEqual(6, counter);
    }

    [Test]
    public void Test_tap_with_index()
    {
        var result = new List<int>();

        new[] { 5, 6 }.Tap((x, i) => result.Add(x * 10 + i)).ToArray();

        Assert.AreEqual(new[] { 50, 61 }, result);
    }
}
