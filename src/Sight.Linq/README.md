# Sight.Linq

[![](https://img.shields.io/nuget/v/Sight.Linq.svg)](https://www.nuget.org/packages/Sight.Linq/)

This library implement extension methods for common classes.

## 📌 `IEnumerable<>` methods

### Append

> Append elements at the end of the collection

**Example:**
```csharp
var arr = new[] { 1, 2, 3 };
var result = arr.Append(4, 5); // = { 1, 2, 3, 4, 5 }
```

### Distinct

> Enumerate elements with unique provided key

**Example:**
```csharp
var arr = new[] { "one", "two", "three", "four" };
var result = arr.Distinct(x => x.Length); // = { "one", "three" }
```

### Except

> Remove elements from the collection

**Example:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.Except(2, 4); // = { 1, 3, 5 }
```

### ForEach

> Invoke delegate with collection index for each element in source

**Example:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var counter = 0;
arr.ForEach(x => counter += x); // counter = 15
```

### IndexOf

> Find index of an item

**Examples:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.ForEach(4); // = 3
```

*or*
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.ForEach(x => x > 2); // = 2
```

### Insert

> Insert elements in the collection

**Example:**
```csharp
var arr = new[] { 1, 2, 5 };
var result = arr.Insert(2 /* :index */, 3, 4); // = { 1, 2, 3, 4, 5 }
```

### IsEmpty

> Indicates if the collection is empty

**Examples:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.IsEmpty(); // = false
```

*or*
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.IsEmpty(x => x > 5); // = true
```

### ToEnumerable

> Convert an element to enumerable of one item

**Example:**
```csharp
var val = 3;
var result = val.ToEnumerable(); // = { 3 }
```

### TryGet

> Try to found an element that match the predicate (like with `IDictionary<,>`)

**Example:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.TryGet(x => x > 2, out var val); // = true (val: 3)
```

### WhereNot

> Filter elements from a predicate

**Example:**
```csharp
var arr = new[] { 1, 2, 3, 4, 5 };
var result = arr.WhereNot(x => x > 2); // = { 1, 2 }
```

### WhereNotNull

> Filter null elements

**Example:**
```csharp
var arr = new[] { "one", null, "three" };
var result = arr.WhereNotNull(); // = { "one", "three" }
```

## 📌 `ICollection<>` methods

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
var collection = new MyCollection { 1, 4, 5 };
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

> Remove item at index

**Example:**
```csharp
var collection = new MyCollection { 1, 2, 3, 4, 5 };
collection.RemoveAt(3); // = { 1, 2, 3, 5 }
```

### RemoveRange

> Remove items from collection

**Example:**
```csharp
var collection = new MyCollection { 1, 2, 3, 4, 5 };
collection.RemoveRange(3, 5); // = { 1, 2, 4 }
```

## 📌 Reflection methods

### GetAttribute

> Get attribute of member

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

> Indicates if member has an attribute

**Example:**
```csharp
[DisplayName("My Class")]
class MyClass { }

var result = typeof(MyClass).HasAttribute<DisplayNameAttribute>(); // = true
```

### GetTypesOf

> Get types of assembly that implement base type

**Example:**
```csharp
interface IMyInterface {}

class MyFirstClass : IMyInterface { }
class MySecondClass : IMyInterface { }

var result = typeof(IMyInterface).Assembly.GetTypesOf<IMyInterface>(); // = { MyFirstClass, MySecondClass }
```
