using System.IO;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;
using Sight.Serialization.Tests.Models;

namespace Sight.Serialization.Tests;

public class TypedTests
{
    private static Player CreatePlayer()
    {
        var conan = new Warrior { Name = "Conan", Rage = 12 };
        var merlin = new Mage { Name = "Merlin", Level = 7, Mana = 300 };
        var player = new Player
        {
            Name = "Arthur",
            Class = CharacterClass.Mage,
            Party = { conan, merlin },
            Inventory = new[] { new Item { Name = "Potion", Count = 3 }, new Item { Name = "Torch" } },
            Stats = { ["str"] = 12.5f, ["dex"] = 8 },
            Leader = merlin,
            Icon = new ExternalRef<Texture>("assets.textures", "hero", 2),
            Hash = 0x9F3A22C100000001,
            Experience = -12,
            Bonus = 4,
            Position = new Point { X = 3, Y = -4 },
            Data = new byte[] { 1, 2, 3 },
            Tags = { "a", "b" },
            Flags = new List<bool> { true, false, true },
            Extra = new Dictionary<string, object?> { ["n"] = 42, ["list"] = new List<object?> { "x", 1.5 } },
        };

        var guild = new Guild { Name = "Round Table" };
        guild.Members.Add(player);
        player.Guild = guild;
        return player;
    }

    private static void AssertPlayer(Player player)
    {
        Assert.AreEqual("Arthur", player.Name);
        Assert.AreEqual(CharacterClass.Mage, player.Class);
        Assert.IsInstanceOf<Warrior>(player.Party[0]);
        Assert.AreEqual(12, ((Warrior)player.Party[0]).Rage);
        Assert.AreEqual(1, player.Party[0].Level);
        Assert.IsInstanceOf<Mage>(player.Party[1]);
        Assert.AreEqual(7, player.Party[1].Level);
        Assert.AreSame(player.Party[1], player.Leader);
        Assert.AreEqual(2, player.Inventory.Length);
        Assert.AreEqual(1, player.Inventory[1].Count);
        Assert.AreEqual(12.5f, player.Stats["str"]);
        Assert.AreEqual("assets.textures", player.Icon!.DependencyId);
        Assert.AreEqual("hero", player.Icon.Key);
        Assert.AreEqual(2u, player.Icon.Version);
        Assert.AreEqual(0x9F3A22C100000001, player.Hash);
        Assert.AreEqual(-12, player.Experience);
        Assert.AreEqual(4, player.Bonus);
        Assert.AreEqual(3, player.Position.X);
        Assert.AreEqual(-4, player.Position.Y);
        Assert.AreEqual(new byte[] { 1, 2, 3 }, player.Data);
        Assert.IsTrue(player.Tags.SetEquals(new[] { "a", "b" }));
        Assert.AreEqual(new[] { true, false, true }, player.Flags.ToArray());
        var extra = (IDictionary<object, object?>)player.Extra!;
        Assert.AreEqual(42, extra["n"]);
        Assert.AreEqual(1.5, ((List<object?>)extra["list"]!)[1]);
        Assert.AreEqual("Arthur", player.Guild!.Members[0].Name);
        Assert.AreSame(player.Guild, player.Guild.Members[0].Guild);
        Assert.AreEqual(0, player.Cache);
    }

    [Test]
    public void BinaryRoundTrip()
    {
        var bytes = BinarySerializer.Write(CreatePlayer());
        AssertPlayer(BinarySerializer.Read<Player>(bytes));
    }

    [Test]
    public void TextRoundTrip()
    {
        var text = TextSerializer.Write(CreatePlayer());
        AssertPlayer(TextSerializer.Read<Player>(text));
        StringAssert.Contains("xp: -12", text);
        StringAssert.Contains("Warrior { name: \"Conan\", rage: 12 }", text);
    }

    [Test]
    public void StreamsAndAsync()
    {
        using var stream = new MemoryStream();
        BinarySerializer.WriteAsync(stream, CreatePlayer()).GetAwaiter().GetResult();
        stream.Position = 0;
        AssertPlayer(BinarySerializer.ReadAsync<Player>(stream).GetAwaiter().GetResult());

        using var text = new MemoryStream();
        TextSerializer.WriteAsync(text, CreatePlayer()).GetAwaiter().GetResult();
        text.Position = 0;
        AssertPlayer(TextSerializer.ReadAsync<Player>(text).GetAwaiter().GetResult());
    }

    [Test]
    public void SchemaFromType()
    {
        var schema = Schema.FromType<Player>();
        Assert.AreEqual("game", schema.Name);
        Assert.AreEqual(3u, schema.Version);
        var text = schema.ToText();
        StringAssert.Contains("abstract struct Character", text);
        StringAssert.Contains("struct Warrior : Character", text);
        StringAssert.Contains("level: u8 = 1", text);
        StringAssert.Contains("leader: ref<Character>?", text);
        StringAssert.Contains("party: list<ref<Character>>", text);
        StringAssert.Contains("guild: ref<Guild>?", text);
        StringAssert.Contains("hash: u64 @fixed", text);
        StringAssert.Contains("bonus: s32?", text);
        StringAssert.Contains("data: list<u8>", text);
        StringAssert.Contains("extra: any", text);
        StringAssert.Contains("icon: extref<Texture>?", text);
        StringAssert.DoesNotContain("cache", text);
        Assert.AreEqual(text, Schema.Parse(text).ToText());
    }

    [Test]
    public void UntypedReadOfTypedData()
    {
        var document = BinarySerializer.Read(BinarySerializer.Write(CreatePlayer()));
        Assert.AreEqual("game.Player", document.Root.TypeId);
        Assert.AreEqual("Conan", document.Root["party"][0]["name"].AsString());
        Assert.AreEqual(new DependencyReference("assets.textures", 2), document.Dependencies.Single());
    }

    [Test]
    public void PolymorphicRoot()
    {
        var bytes = BinarySerializer.Write<Character>(new Mage { Name = "M", Mana = 5 });
        var read = BinarySerializer.Read<Character>(bytes);
        Assert.IsInstanceOf<Mage>(read);
        Assert.AreEqual(5u, ((Mage)read).Mana);
        Assert.Throws<SerializationException>(() => BinarySerializer.Read<Warrior>(bytes));
    }

    [Test]
    public void DefaultValuesAreOmitted()
    {
        var withDefault = BinarySerializer.Write(new Item { Name = "a", Count = 1 });
        var withValue = BinarySerializer.Write(new Item { Name = "a", Count = 2 });
        Assert.AreEqual(withDefault.Length + 1, withValue.Length);
        Assert.AreEqual(1, BinarySerializer.Read<Item>(withDefault).Count);
    }

    [Test]
    public void NullIsRejectedForNonNullableMembers()
    {
        Assert.Throws<SerializationException>(() => BinarySerializer.Write(new Item { Name = null! }));
    }

    [Test]
    public void SchemaEvolution()
    {
        var guild = new Guild { Name = "g" };
        var bytes = BinarySerializer.Write(new MonsterV1 { Name = "orc", Hp = 200, Removed = "x", Friend = guild, Xp = 77 });

        var v2 = BinarySerializer.Read<MonsterV2>(bytes);
        Assert.AreEqual("orc", v2.Name);
        Assert.AreEqual(200, v2.Hp);
        Assert.IsNull(v2.Added);
        Assert.AreEqual(5, v2.WithDefault);
        Assert.AreEqual(77, v2.Experience);

        // u8 → s8 is not a widening conversion
        Assert.Throws<SerializationException>(() => BinarySerializer.Read<MonsterNarrow>(bytes));

        // Required field missing in the data
        Assert.Throws<SerializationException>(() => BinarySerializer.Read<MonsterRequired>(bytes));
    }

    [Test]
    public void ExternalReferencesFromMemory()
    {
        var options = new SerializationOptions();
        options.Dependencies.Add("assets.hero", 1, new Texture { Path = "hero.png" });

        var library = new Library { Textures = { new Texture { Path = "a.png" }, new Texture { Path = "b.png" } } };
        var libraryOptions = new SerializationOptions { Id = "assets.library", Version = 4 };
        libraryOptions.Exports["second"] = library.Textures[1];
        var libraryBytes = BinarySerializer.Write(library, libraryOptions);
        options.Dependencies.Add("assets.library", 4, () => new MemoryStream(libraryBytes));

        var sprite = new Sprite
        {
            Texture = new ExternalRef<Texture>("assets.library", "second"),
            Library = new ExternalRef<Library>("assets.library"),
        };

        var bytes = BinarySerializer.Write(sprite, options);
        var read = BinarySerializer.Read<Sprite>(bytes, options);
        Assert.AreEqual("b.png", read.Texture.Resolve().Path);
        Assert.AreEqual(2, read.Library!.Resolve().Textures.Count);
        Assert.IsTrue(read.Texture.IsResolved);

        var direct = new ExternalRef<Texture>("assets.hero", version: 1);
        Assert.AreEqual("hero.png", direct.Resolve(options).Path);
    }

    [Test]
    public void ExternalReferencesFromSearchPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), "sight-serialization-" + Guid.NewGuid().ToString("N"));
        var mods = Path.Combine(root, "mods");
        var assets = Path.Combine(root, "assets");
        Directory.CreateDirectory(mods);
        Directory.CreateDirectory(Path.Combine(assets, "nested"));
        try
        {
            File.WriteAllBytes(Path.Combine(assets, "nested", "hero.sbdl"), BinarySerializer.Write(new Texture { Path = "base.png" }, new SerializationOptions { Id = "tex.hero", Version = 1 }));
            File.WriteAllText(Path.Combine(mods, "hero.sbdt"), TextSerializer.Write(new Texture { Path = "mod.png" }, new SerializationOptions { Id = "tex.hero", Version = 1 }));
            File.WriteAllBytes(Path.Combine(assets, "hero2.sbdl"), BinarySerializer.Write(new Texture { Path = "v2.png" }, new SerializationOptions { Id = "tex.hero", Version = 2 }));

            var diagnostics = new List<string>();
            var options = new SerializationOptions { OnDiagnostic = diagnostics.Add };
            options.SearchPaths.Add(mods);
            options.SearchPaths.Add(assets);

            Assert.AreEqual("mod.png", new ExternalRef<Texture>("tex.hero", version: 1).Resolve(options).Path);
            Assert.AreEqual("v2.png", new ExternalRef<Texture>("tex.hero", version: 2).Resolve(options).Path);
            Assert.IsTrue(diagnostics.Any(x => x.Contains("shadowed")));
            Assert.Throws<SerializationException>(() => new ExternalRef<Texture>("tex.hero", version: 3).Resolve(options));

            var newer = new SerializationOptions { AllowNewerDependencies = true };
            newer.SearchPaths.Add(assets);
            Assert.AreEqual("v2.png", new ExternalRef<Texture>("tex.hero", version: 0).Resolve(newer).Path);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ReferencedSchemaFromSearchPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "sight-serialization-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var schema = Schema.FromType<Item>();
            using (var file = File.Create(Path.Combine(root, "game.sbdl")))
                schema.Save(file);

            var bytes = BinarySerializer.Write(new Item { Name = "x", Count = 4 }, new SerializationOptions { SchemaMode = SchemaMode.Reference });
            var options = new SerializationOptions();
            options.SearchPaths.Add(root);
            Assert.AreEqual(4, BinarySerializer.Read<Item>(bytes, options).Count);
            Assert.AreEqual(4, BinarySerializer.Read(bytes, options).Root["count"].AsInt32());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void MetadataAndCompression()
    {
        var options = new SerializationOptions { Id = "save-1", Version = 9, Compression = Compression.Deflate };
        options.Metadata["author"] = "Arthur";
        var bytes = BinarySerializer.Write(CreatePlayer(), options);
        AssertPlayer(BinarySerializer.Read<Player>(bytes, options));
        var document = BinarySerializer.Read(bytes, options);
        Assert.AreEqual("save-1", document.Id);
        Assert.AreEqual(9u, document.Version);
        Assert.AreEqual("Arthur", document.Metadata["author"]);
    }

    [Test]
    public void LargeCollections()
    {
        var library = new Library();
        for (var i = 0; i < 10000; i++)
            library.Textures.Add(new Texture { Path = "texture" + (i % 100) });

        var bytes = BinarySerializer.Write(library);
        var read = BinarySerializer.Read<Library>(bytes);
        Assert.AreEqual(10000, read.Textures.Count);
        Assert.AreEqual("texture99", read.Textures[9999].Path);

        // Short strings are deduplicated: 100 distinct paths stored once each
        Assert.Less(bytes.Length, 10000 * 3);
    }

    [Test]
    public void InlineSchemasAreReusedAcrossReads()
    {
        var bytes = BinarySerializer.Write(CreatePlayer());
        var first = BinarySerializer.Read(bytes);
        var second = BinarySerializer.Read(new MemoryStream(bytes));
        Assert.AreSame(first.Schemas[0], second.Schemas[0]);
        AssertPlayer(BinarySerializer.Read<Player>(bytes));
        AssertPlayer(BinarySerializer.Read<Player>(new MemoryStream(bytes)));

        // Same schemas, other encodings of the section: not mixed up
        var noDedup = BinarySerializer.Write(CreatePlayer(), new SerializationOptions { StringDedupMaxLength = 0 });
        AssertPlayer(BinarySerializer.Read<Player>(noDedup));
        var deflate = new SerializationOptions { Compression = Compression.Deflate };
        AssertPlayer(BinarySerializer.Read<Player>(BinarySerializer.Write(CreatePlayer(), deflate), deflate));
        AssertPlayer(BinarySerializer.Read<Player>(bytes));

        // Other schemas
        var library = new Library { Textures = { new Texture { Path = "a" } } };
        Assert.AreEqual("a", BinarySerializer.Read<Library>(BinarySerializer.Write(library)).Textures[0].Path);
        AssertPlayer(BinarySerializer.Read<Player>(bytes));
    }

    [Test]
    public void InlineSchemasAreNotReusedWithOtherLimits()
    {
        var bytes = BinarySerializer.Write(CreatePlayer());
        var first = BinarySerializer.Read(bytes);
        var options = new SerializationOptions();
        options.Limits.MaxCollectionCount = 1000;
        var second = BinarySerializer.Read(bytes, options);
        Assert.AreNotSame(first.Schemas[0], second.Schemas[0]);
    }
}
