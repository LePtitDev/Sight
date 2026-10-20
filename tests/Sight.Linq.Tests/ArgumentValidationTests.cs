using Sight.Linq;

namespace Sight.Linq.Tests;

public class ArgumentValidationTests
{
    private static readonly IEnumerable<int> s_null = null!;
    private static readonly IEnumerable<int> s_source = new[] { 1, 2, 3 };

    [Test]
    public void Test_lazy_methods_validate_arguments_eagerly()
    {
        // Results are never enumerated: the exception must come from the call itself
        Assert.Throws<ArgumentNullException>(() => s_null.Distinct(x => x));
        Assert.Throws<ArgumentNullException>(() => s_source.Distinct<int, int>(null!));
        Assert.Throws<ArgumentNullException>(() => s_null.Tap(_ => { }));
        Assert.Throws<ArgumentNullException>(() => s_source.Tap((Action<int>)null!));
        Assert.Throws<ArgumentNullException>(() => s_source.Tap((Action<int, int>)null!));
        Assert.Throws<ArgumentNullException>(() => s_null.InsertAt(0, 1));
        Assert.Throws<ArgumentNullException>(() => s_source.InsertAt(0, null!));
        Assert.Throws<ArgumentNullException>(() => s_null.WhereNot(x => x > 1));
        Assert.Throws<ArgumentNullException>(() => s_source.WhereNot((Func<int, int, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int?>)null!).WhereNotNull());
        Assert.Throws<ArgumentNullException>(() => s_null.Append(1));
        Assert.Throws<ArgumentNullException>(() => s_null.Except(1));
    }

    [Test]
    public void Test_eager_methods_validate_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => s_null.ForEach(_ => { }));
        Assert.Throws<ArgumentNullException>(() => s_source.ForEach((Action<int>)null!));
        Assert.Throws<ArgumentNullException>(() => s_null.IndexOf(1));
        Assert.Throws<ArgumentNullException>(() => s_source.IndexOf((Func<int, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => s_null.LastIndexOf(1));
        Assert.Throws<ArgumentNullException>(() => s_source.LastIndexOf((Func<int, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => s_null.IsEmpty());
        Assert.Throws<ArgumentNullException>(() => s_source.None(null!));
        Assert.Throws<ArgumentNullException>(() => s_null.TryGetFirst(x => true, out _));
        Assert.Throws<ArgumentNullException>(() => s_source.TryGetLast(null!, out _));
    }

    [Test]
    public void Test_collection_methods_validate_arguments()
    {
        ICollection<int> collection = new List<int>();
        ICollection<int> nullCollection = null!;

        Assert.Throws<ArgumentNullException>(() => nullCollection.AddRange(1));
        Assert.Throws<ArgumentNullException>(() => collection.AddRange((IEnumerable<int>)null!));
        Assert.Throws<ArgumentNullException>(() => collection.RemoveAll((Func<int, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => collection.RemoveRange((IEnumerable<int>)null!));
        Assert.Throws<ArgumentNullException>(() => collection.RemoveAll((IEnumerable<int>)null!));
        Assert.Throws<ArgumentNullException>(() => ((IList<int>)new List<int>()).InsertRange(0, (IEnumerable<int>)null!));
    }

    [Test]
    public void Test_reflection_methods_validate_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => ((System.Reflection.ICustomAttributeProvider)null!).GetAttribute<ObsoleteAttribute>());
        Assert.Throws<ArgumentNullException>(() => typeof(int).HasAttribute(null!));
        Assert.Throws<ArgumentNullException>(() => ((System.Reflection.Assembly)null!).GetTypesOf<object>());
        Assert.Throws<ArgumentNullException>(() => typeof(int).Assembly.GetTypesOf(null!));
    }
}
