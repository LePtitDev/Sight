using System.ComponentModel;

[assembly: Sight.Serialization.SerializationSchema("game", 3)]

namespace Sight.Serialization.Tests.Models;

[SerializationType("game.Class")]
public enum CharacterClass : byte
{
    Warrior,
    Mage,
}

[SerializationType("game.Character")]
[SerializationDerivedType(typeof(Warrior))]
[SerializationDerivedType(typeof(Mage))]
[SerializationDerivedType(typeof(Dragon))]
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

[SerializationType("game.Mage")]
public class Mage : Character
{
    public uint Mana { get; set; }
}

// Derived type declared in an extension schema
[SerializationType("mods.Dragon")]
public class Dragon : Character
{
    public byte Wings { get; set; }
}

[SerializationType("game.Item")]
public class Item
{
    public string Name { get; set; } = string.Empty;

    [DefaultValue(1)]
    public ushort Count { get; set; } = 1;
}

[SerializationType("game.Guild", IsReference = true)]
public class Guild
{
    public string Name { get; set; } = string.Empty;

    [SerializationReference]
    public List<Player> Members { get; set; } = new();
}

[SerializationType("game.Texture")]
public class Texture
{
    public string Path { get; set; } = string.Empty;
}

[SerializationType("game.Point")]
public struct Point
{
    public int X { get; set; }

    public int Y;
}

[SerializationType("game.Player")]
public class Player
{
    public string Name { get; set; } = string.Empty;

    public CharacterClass Class { get; set; }

    [SerializationReference]
    public List<Character> Party { get; set; } = new();

    public Item[] Inventory { get; set; } = Array.Empty<Item>();

    public Dictionary<string, float> Stats { get; set; } = new();

    public Guild? Guild { get; set; }

    [SerializationReference]
    public Character? Leader { get; set; }

    public ExternalRef<Texture>? Icon { get; set; }

    [SerializationFixed]
    public ulong Hash { get; set; }

    [SerializationField("xp")]
    public int Experience { get; set; }

    public int? Bonus { get; set; }

    public Point Position { get; set; }

    public byte[] Data { get; set; } = Array.Empty<byte>();

    public HashSet<string> Tags { get; set; } = new();

    public IReadOnlyList<bool> Flags { get; set; } = new List<bool>();

    public object? Extra { get; set; }

    [SerializationIgnore]
    public int Cache { get; set; }
}

// Schema evolution: two versions of the same type identity
[SerializationType("evo.Monster")]
public class MonsterV1
{
    public string Name { get; set; } = string.Empty;

    public byte Hp { get; set; }

    public string Removed { get; set; } = string.Empty;

    public Guild? Friend { get; set; }

    public int Xp { get; set; }
}

[SerializationType("evo.Monster")]
public class MonsterV2
{
    public string Name { get; set; } = string.Empty;

    public int Hp { get; set; }

    public string? Added { get; set; }

    [DefaultValue(5)]
    public int WithDefault { get; set; }

    [SerializationAlias("xp")]
    public int Experience { get; set; }
}

[SerializationType("evo.Monster")]
public class MonsterNarrow
{
    public string Name { get; set; } = string.Empty;

    public sbyte Hp { get; set; }
}

[SerializationType("evo.Monster")]
public class MonsterRequired
{
    public string Name { get; set; } = string.Empty;

    public int Required { get; set; }
}

[SerializationType("lib.Library")]
public class Library
{
    public List<Texture> Textures { get; set; } = new();
}

[SerializationType("lib.Sprite")]
public class Sprite
{
    public ExternalRef<Texture> Texture { get; set; } = null!;

    public ExternalRef<Library>? Library { get; set; }
}
