# Sight.Linq

[![](https://img.shields.io/nuget/v/Sight.Linq.svg)](https://www.nuget.org/packages/Sight.Linq/)

This library implement extension methods for common classes.

Arguments are validated eagerly: a `null` source or delegate throws an `ArgumentNullException` at the call, even for lazy methods.

> **Upgrading from 0.1.x:** `ForEach` is now eager and returns `void` (use `Tap` for the previous lazy behavior), `Except` keeps duplicates, `WhereNotNull` only applies to nullable types, and `IndexOfLast`, `Insert`, `IsEmpty(predicate)` and `TryGet` are obsolete in favor of `LastIndexOf`, `InsertAt`, `None` and `TryGetFirst`.

## 📌 `IEnumerable<>` methods

### Append

> Append elements at the end of the collection

**Example:**
```csharp
var arr = new[] { 1, 2, 3 };
var result = arr.Append(4, 5); // = { 1, 2, 3, 4, 5 }
```

**Note:** on an `IEnumerable<object>`, appending an `object[]` appends each of its elements, not the array itself.

### Distinct

> Enumerate elements with unique provided key (on .NET 6+, it is `Enumerable.DistinctBy`)

**Example:**
```csharp
var arr = new[] { "one", "two", "three", "four" };
var result = arr.Distinct(x => x.Length); // = { "one", "three", "four" }
```

### Except

> Remove elements from the collection. Unlike `Enumerable.Except`, the remaining elements are not de-duplicated

**Examples:**
```csharp
var arr = new[] { 1, 1, 2, 3, 4, 5 };
var result = arr.Except(2, 4); // = { 1, 1, 3, 5 }
```

*or, with a comparer*
```csharp
var arr = new[] { "a", "B", "c" };
var result = arr.Except(StringComparer.OrdinalIgnoreCase, "b"); // = { "a", "c" }
```

### ForEach

> Invoke delegate for each element in source. The delegate is invoked immediately

**Examples:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var counter = 0;
arr.ForEach(x => counter += x); // counter = 15
```

*or, with the index*
```csharp
var arr = new[] { "a", "b" };
arr.ForEach((x, i) => Console.WriteLine($"{i}: {x}"));
```

**Note:** `List<T>` has its own instance `ForEach(Action<T>)`, which has the same behavior.

### IndexOf

> Find index of an item

**Examples:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.IndexOf(4); // = 3
```

*or*
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.IndexOf(x => x > 2); // = 2
```

### InsertAt

> Insert elements in the collection. The result is lazy: a negative index throws immediately and an index greater than the number of elements throws when the enumeration ends

**Example:**
```csharp
var arr = new[] { 1, 2, 5 };
var result = arr.InsertAt(2 /* :index */, 3, 4); // = { 1, 2, 3, 4, 5 }
```

*(`Insert` is obsolete: on a `List<T>`, it is shadowed by the instance `Insert` method that mutates the list.)*

### IsEmpty

> Indicates if the collection is empty

**Example:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.IsEmpty(); // = false
```

### IsNullOrEmpty

> Indicates if the collection is `null` or empty

**Example:**
```csharp
IEnumerable<int>? arr = null;
var result = arr.IsNullOrEmpty(); // = true
```

### LastIndexOf

> Find index of the last item

**Examples:**
```csharp
var arr = new[] { 1, 4, 3, 4, 5 };
var result = arr.LastIndexOf(4); // = 3
```

*or*
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.LastIndexOf(x => x < 4); // = 2
```

*(`IndexOfLast` is obsolete.)* On lists, the search runs backward from the end.

### None

> Indicates if no element of the collection pass the predicate

**Example:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.None(x => x > 5); // = true
```

*(`IsEmpty(predicate)` is obsolete.)*

### Pairwise

> Enumerate each pair of consecutive elements (or the result of a selector applied to them)

**Examples:**
```csharp
var arr = new[] { 1, 2, 3, 4 };
var result = arr.Pairwise(); // = { (1, 2), (2, 3), (3, 4) }
```

*or*
```csharp
var arr = new[] { 1, 4, 9, 16 };
var result = arr.Pairwise((a, b) => b - a); // = { 3, 5, 7 }
```

### Tap

> Lazily invoke delegate for each element while the result is enumerated

**Example:**
```csharp
var arr = new[] { 1, 2, 3 };
var counter = 0;
var result = arr.Tap(x => counter += x); // counter = 0
result.ToArray(); // counter = 6
```

### ToEnumerable

> Convert an element to enumerable of one item

**Example:**
```csharp
var val = 3;
var result = val.ToEnumerable(); // = { 3 }
```

### TryGetFirst

> Try to found the first element that match the predicate (like with `IDictionary<,>`)

**Example:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.TryGetFirst(x => x > 2, out var val); // = true (val: 3)
```

*(`TryGet` is obsolete.)*

### TryGetLast

> Try to found the last element that match the predicate. On lists, the search runs backward from the end

**Example:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.TryGetLast(x => x < 4, out var val); // = true (val: 3)
```

### WhereNot

> Filter elements from a predicate

**Example:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.WhereNot(x => x > 2); // = { 1, 2 }
```

### WhereNotNull

> Filter null elements. The result type is not nullable (`string?` becomes `string`, `int?` becomes `int`)

**Example:**
```csharp
var arr = new[] { "one", null, "three" };
IEnumerable<string> result = arr.WhereNotNull(); // = { "one", "three" }
```

## 📌 `ICollection<>` methods

All methods throw a `NotSupportedException` with a clear message on a read-only collection (an array for example).
When the items are the collection itself (`collection.AddRange(collection)`), they are copied first.

### AddRange

> Add items to collection

**Example:**
```csharp
var collection = new MyCollection { 1, 2, 3 };
collection.AddRange(4, 5); // = { 1, 2, 3, 4, 5 }
```

### InsertRange

> Insert items to collection at specified index

**Example:**
```csharp
var collection = new MyList { 1, 4, 5 };
collection.InsertRange(1 /* :index */, 2, 3); // = { 1, 2, 3, 4, 5 }
```

### RemoveAll

> Remove each duplicated items from collection

**Examples:**
```csharp
var collection = new MyCollection { 1, 1, 2, 3, 5, 5 };
collection.RemoveAll(1, 5); // = { 2, 3 }
```

*or*
```csharp
var collection = new MyCollection { 1, 2, 3, 4, 5 };
collection.RemoveAll(x => x > 3); // = { 1, 2, 3 }
```

### RemoveAt

> Remove item at index. The collection must be an `IList<>`, otherwise a `NotSupportedException` is thrown

**Example:**
```csharp
ICollection<int> collection = new MyList { 1, 2, 3, 4, 5 };
collection.RemoveAt(3); // = { 1, 2, 3, 5 }
```

### RemoveRange

> Remove items from collection (one occurrence of each)

**Example:**
```csharp
var collection = new MyCollection { 1, 2, 3, 4, 5 };
collection.RemoveRange(3, 5); // = { 1, 2, 4 }
```

## 📌 `IDictionary<,>` methods

### GetOrAdd

> Get the value of a key, or add it if the key is not found (the factory is invoked only when needed)

**Examples:**
```csharp
var dictionary = new Dictionary<string, int> { ["a"] = 1 };
var result = dictionary.GetOrAdd("b", 2); // = 2 (added)
```

*or*
```csharp
var result = dictionary.GetOrAdd("ccc", key => key.Length); // = 3 (added)
```

### GetValueOrDefault

> Get the value of a key, or a default value if the key is not found

**Example:**
```csharp
IDictionary<string, int> dictionary = new SortedDictionary<string, int> { ["a"] = 1 };
var result = dictionary.GetValueOrDefault("b", -1); // = -1
```

**Note:** on types that are both `IDictionary<,>` and `IReadOnlyDictionary<,>` other than `Dictionary<,>` (like `ConcurrentDictionary<,>`), the call is ambiguous with the BCL method on .NET Core: cast the dictionary or use the BCL one.

## 📌 Reflection methods

### GetAttribute

> Get attribute of member. If several attributes match, the first one is returned (unlike `GetCustomAttribute<T>` that throws an `AmbiguousMatchException`)

**Example:**
```csharp
[DisplayName("My Class")]
class MyClass { }

var result = typeof(MyClass).GetAttribute<DisplayNameAttribute>(); // = DisplayName("My Class")
```

### GetAttributes

> Get attributes of member

**Example:**
```csharp
[Item("One")]
[Item("Two")]
[Item("Three")]
class MyClass { }

var result = typeof(MyClass).GetAttributes<ItemAttribute>(); // = { Item("One"), Item("Two"), Item("Three") }
```

### HasAttribute

> Indicates if member has an attribute (without instantiating it). `inherit` is honored for properties and events too

**Example:**
```csharp
[DisplayName("My Class")]
class MyClass { }

var result = typeof(MyClass).HasAttribute<DisplayNameAttribute>(); // = true
```

### GetTypesOf

> Get types of assembly that implement base type. Types that cannot be loaded are ignored, and open generic base types (`typeof(IRepository<>)`) are supported.
> Not compatible with trimming.

**Example:**
```csharp
interface IMyInterface {}

class MyFirstClass : IMyInterface { }
class MySecondClass : IMyInterface { }

var result = typeof(IMyInterface).Assembly.GetTypesOf<IMyInterface>(); // = { MyFirstClass, MySecondClass }
```
