# Sight.Serialization

[![](https://img.shields.io/nuget/v/Sight.Serialization.svg)](https://www.nuget.org/packages/Sight.Serialization/)

This library implements schema-driven serialization in a compact binary format (`.sbdl`) and an equivalent text format (`.sbdt`). Formats are specified in [docs/specs](../../docs/specs).

## Features

* Binary and text formats, lossless conversion between them
* Schemas declared with C# types, a schema language (`.sgs`) or an API
* Schemas embedded in files, referenced by name and hash, or omitted; several schemas per file (e.g. extension schemas with derived types)
* Typed (de)serialization or untyped documents (`DataDocument`)
* Compact encoding: variable-length integers, deduplicated short strings, presence bitmaps, optional Deflate compression
* Inheritance and polymorphism, internal references (identity and cycles), external references to other files
* Schema evolution: added, removed and renamed fields, widened numbers
* Dependencies resolved from memory, custom resolvers or search paths
* Streams, byte arrays and async APIs

## How to use?

```csharp
[SerializationType("game.Character")]
[SerializationDerivedType(typeof(Warrior))]
public abstract class Character
{
    public string Name { get; set; } = string.Empty;

    [DefaultValue(1)]
    public byte Level { get; set; } = 1;
}

[SerializationType("game.Warrior")]
public class Warrior : Character
{
    public ushort Rage { get; set; }
}

[SerializationType("game.Player")]
public class Player
{
    [SerializationReference]
    public List<Character> Party { get; set; } = new();

    [SerializationReference]
    public Character? Leader { get; set; }

    public ExternalRef<Texture>? Icon { get; set; }
}

// Binary
byte[] data = BinarySerializer.Write(player);
Player copy = BinarySerializer.Read<Player>(data);

// Text
string text = TextSerializer.Write(player);
copy = TextSerializer.Read<Player>(text);

// Untyped
DataDocument document = BinarySerializer.Read(data);
string name = document.Root["party"][0]["name"].AsString();

// Options
var options = new SerializationOptions
{
    Id = "save-01",
    SchemaMode = SchemaMode.Inline,
    Compression = Compression.Deflate,
    SearchPaths = { "assets/" },
};
BinarySerializer.Write(stream, player, options);
```

Schemas can also be written in the schema language:

```
@sgs 1
@id game
@version 3
@import assets 2

abstract struct Character {
  name:  string
  level: u8 = 1
}

struct Warrior : Character { rage: u16 }
```

```csharp
Schema schema = Schema.Parse(text);
schema.Save(stream); // compiled schema (.sbdl)
Schema same = Schema.FromType<Player>();
```

## Mapping of C# types

| C# | Schema |
|---|---|
| `bool`, `byte`, `sbyte`, `ushort`, `short`, `uint`, `int`, `ulong`, `long`, `float`, `double`, `string` | `bool`, `u8`, `s8`, `u16`, `s16`, `u32`, `s32`, `u64`, `s64`, `f32`, `f64`, `string` |
| `enum` | `enum` (same underlying type) |
| `T[]`, `List<T>`, `IList<T>`, `IReadOnlyList<T>`, `HashSet<T>`... | `list<T>` (`byte[]` is a fast `list<u8>`) |
| `Dictionary<K, V>`, `IDictionary<K, V>`, `IReadOnlyDictionary<K, V>` | `map<K, V>` |
| class or struct | `struct` (`ref<T>` with `[SerializationReference]` or `IsReference = true`) |
| `ExternalRef<T>` | `extref<T>` |
| `object` | `any` |
| `T?` (nullable value type or annotated reference type) | `T?` |

Public properties with a setter and public fields are serialized; field names are the member names in camelCase.
