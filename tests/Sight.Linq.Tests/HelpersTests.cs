using System.Collections.ObjectModel;
using Sight.Linq;

namespace Sight.Linq.Tests;

public class HelpersTests
{
    private static IEnumerable<int> Generate(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return i;
        }
    }

    [Test]
    public void Test_is_null_or_empty()
    {
        IEnumerable<int>? nothing = null;

        Assert.IsTrue(nothing.IsNullOrEmpty());
        Assert.IsTrue(Array.Empty<int>().IsNullOrEmpty());
        Assert.IsTrue(new List<int>().IsNullOrEmpty());
        Assert.IsTrue(Generate(0).IsNullOrEmpty());
        Assert.IsFalse(new[] { 1 }.IsNullOrEmpty());
        Assert.IsFalse(new HashSet<int> { 1 }.IsNullOrEmpty());
        Assert.IsFalse(Generate(2).IsNullOrEmpty());
    }

    [Test]
    public void Test_pairwise_yields_consecutive_pairs()
    {
        var result = new[] { 1, 2, 3, 4 }.Pairwise().ToArray();

        Assert.AreEqual(new[] { (1, 2), (2, 3), (3, 4) }, result);
    }

    [Test]
    public void Test_pairwise_with_selector()
    {
        var result = new[] { 1, 4, 9, 16 }.Pairwise((a, b) => b - a);

        Assert.AreEqual(new[] { 3, 5, 7 }, result);
    }

    [Test]
    public void Test_pairwise_with_less_than_two_items_is_empty()
    {
        Assert.IsEmpty(Generate(0).Pairwise());
        Assert.IsEmpty(Generate(1).Pairwise());
    }

    [Test]
    public void Test_pairwise_validates_arguments_eagerly()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null!).Pairwise());
        Assert.Throws<ArgumentNullException>(() => new[] { 1 }.Pairwise((Func<int, int, int>)null!));
    }

    [Test]
    public void Test_get_value_or_default()
    {
        IDictionary<string, int> dictionary = new SortedDictionary<string, int> { ["a"] = 1 };

        Assert.AreEqual(1, dictionary.GetValueOrDefault("a"));
        Assert.AreEqual(0, dictionary.GetValueOrDefault("b"));
        Assert.AreEqual(9, dictionary.GetValueOrDefault("b", 9));
    }

    [Test]
    public void Test_get_value_or_default_on_dictionary_is_not_ambiguous()
    {
        var dictionary = new Dictionary<string, string> { ["a"] = "x" };

        Assert.AreEqual("x", dictionary.GetValueOrDefault("a"));
        Assert.IsNull(dictionary.GetValueOrDefault("b"));
        Assert.AreEqual("y", dictionary.GetValueOrDefault("b", "y"));
    }

    [Test]
    public void Test_get_or_add_with_value()
    {
        IDictionary<string, int> dictionary = new SortedDictionary<string, int> { ["a"] = 1 };

        Assert.AreEqual(1, dictionary.GetOrAdd("a", 5));
        Assert.AreEqual(5, dictionary.GetOrAdd("b", 5));
        Assert.AreEqual(5, dictionary["b"]);
    }

    [Test]
    public void Test_get_or_add_with_factory_invokes_it_only_when_missing()
    {
        var dictionary = new Dictionary<string, int> { ["a"] = 1 };
        var calls = 0;

        Assert.AreEqual(1, dictionary.GetOrAdd("a", k => { calls++; return 7; }));
        Assert.AreEqual(0, calls);
        Assert.AreEqual(1, dictionary.GetOrAdd("bb", k => { calls++; return k.Length; }) - 1);
        Assert.AreEqual(1, calls);
        Assert.AreEqual(2, dictionary["bb"]);
    }

    [Test]
    public void Test_get_or_add_on_read_only_dictionary()
    {
        IDictionary<string, int> dictionary = new ReadOnlyDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

        Assert.AreEqual(1, dictionary.GetOrAdd("a", 5));
        Assert.Throws<NotSupportedException>(() => dictionary.GetOrAdd("b", 5));
        Assert.Throws<NotSupportedException>(() => dictionary.GetOrAdd("b", _ => 5));
    }

    [Test]
    public void Test_dictionary_helpers_validate_arguments()
    {
        IDictionary<string, int> dictionary = null!;

        Assert.Throws<ArgumentNullException>(() => dictionary.GetValueOrDefault("a"));
        Assert.Throws<ArgumentNullException>(() => dictionary.GetOrAdd("a", 1));
        Assert.Throws<ArgumentNullException>(() => new Dictionary<string, int>().GetOrAdd("a", (Func<string, int>)null!));
    }
}
