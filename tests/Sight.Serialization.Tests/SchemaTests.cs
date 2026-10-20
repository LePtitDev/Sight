using System.IO;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Tests;

public class SchemaTests
{
    internal const string GameSchema = """
        @sgs 1
        @id game
        @version 3

        enum Class : u8 { Warrior, Mage = 5, Rogue }

        struct Item {
          name:  string
          count: u16 = 1
        }

        abstract struct Character {
          name:  string
          level: u8 = 1
        }

        struct Warrior : Character { rage: u16 }
        struct Mage : Character { mana: u32 }

        struct Player {
          party:      list<ref<Character>>
          inventory:  list<Item>
          stats:      map<string, f32>
          leader:     ref<Character>?
          class:      Class = Mage
          hash:       u64 @fixed
          experience: s32 @alias(xp, exp)
          extra:      any
        }
        """;

    [Test]
    public void ParseSchema()
    {
        var schema = Schema.Parse(GameSchema);
        Assert.AreEqual("game", schema.Name);
        Assert.AreEqual(3u, schema.Version);
        Assert.AreEqual(6, schema.Types.Count);

        var classes = (EnumDefinition)schema.FindType("Class")!;
        Assert.AreEqual(TypeKind.U8, classes.Underlying);
        Assert.AreEqual(new long[] { 0, 5, 6 }, classes.Values.Select(x => x.Value).ToArray());

        var warrior = (StructDefinition)schema.FindType("game.Warrior")!;
        Assert.AreEqual("Character", warrior.Base!.Name);
        Assert.AreEqual(new[] { "name", "level", "rage" }, warrior.AllFields.Select(x => x.Name).ToArray());

        var player = (StructDefinition)schema.FindType("Player")!;
        Assert.AreEqual("list<ref<Character>>", player.FindField("party")!.Type.ToString(schema));
        Assert.AreEqual("ref<Character>?", player.FindField("leader")!.Type.ToString(schema));
        Assert.IsTrue(player.FindField("hash")!.Type.IsFixed);
        Assert.AreEqual(new[] { "xp", "exp" }, player.FindField("experience")!.Aliases.ToArray());
        Assert.AreEqual("Mage", player.FindField("class")!.DefaultValue!.AsString());
    }

    [Test]
    public void PrintAndParseAreStable()
    {
        var schema = Schema.Parse(GameSchema);
        var text = schema.ToText();
        var reparsed = Schema.Parse(text);
        Assert.AreEqual(text, reparsed.ToText());
        Assert.AreEqual(schema.Hash, reparsed.Hash);
    }

    [Test]
    public void HashDependsOnContent()
    {
        var a = Schema.Parse("@sgs 1\n@id s\n@version 1\nstruct A { x: u8 }");
        var b = Schema.Parse("@sgs 1\n@id s\n@version 1\nstruct A { x: u16 }");
        var c = Schema.Parse("@sgs 1\n@id s\n@version 1\n// comment\nstruct A {\n  x: u8,\n}");
        Assert.AreNotEqual(a.Hash, b.Hash);
        Assert.AreEqual(a.Hash, c.Hash);
    }

    [TestCase("@sgs 1\n@id s\n@version 1\nstruct A { x: unknown }", 4)]
    [TestCase("@sgs 1\n@id s\n@version 1\nstruct A { x: u8 y: u8 }", 4)]
    [TestCase("@sgs 1\n@id s\n@version 1\nstruct A { x: u8? = 1 }", 4)]
    [TestCase("@sgs 1\n@id s\n@version 1\nstruct A { x: any? }", 4)]
    [TestCase("@sgs 1\n@id s\n@version 1\nstruct A { x: map<f32, u8> }", 5)]
    [TestCase("@sgs 1\n@id s\n@version 1\nstruct A { x: u8 @fixed }", 4)]
    [TestCase("@sgs 1\n@id s\n@version 1\nenum E : u8 { A = 300 }", 4)]
    [TestCase("@sgs 1\n@id s\n@version 1\nstruct A {}\nstruct A {}", 5)]
    [TestCase("@sgs 1\n@id s\n@version 1\nstruct A : B {}\nstruct B : A {}", 5)]
    public void InvalidSchemasAreRejected(string text, int line)
    {
        var exception = Assert.Throws<SerializationException>(() => Schema.Parse(text))!;
        Assert.Greater(exception.Line, 0, exception.Message);
        Assert.LessOrEqual(exception.Line, line, exception.Message);
    }

    [Test]
    public void ImportsAreResolvedFromOptions()
    {
        var assets = Schema.Parse("@sgs 1\n@id assets\n@version 2\nstruct Texture { path: string }");
        var options = new SerializationOptions();
        options.Schemas.Add(assets);
        var game = Schema.Parse("@sgs 1\n@id game\n@version 1\n@import assets 2\nstruct Hero { icon: ref<Texture>, other: assets.Texture }", options);
        Assert.AreSame(assets, game.Imports[0]);
        Assert.AreEqual("ref<assets.Texture>", ((StructDefinition)game.FindType("Hero")!).Fields[0].Type.ToString(game));
    }

    [Test]
    public void CompiledSchemaRoundTrip()
    {
        var schema = Schema.Parse(GameSchema);
        using var compiled = new MemoryStream();
        schema.Save(compiled);
        compiled.Position = 0;
        var loaded = Schema.Load(compiled);
        Assert.AreEqual(schema.Hash, loaded.Hash);
        Assert.AreEqual(schema.ToText(), loaded.ToText());

        // A compiled schema is an SBDL file whose root is core.Schema
        var document = BinarySerializer.Read(compiled.ToArray());
        Assert.AreEqual("game", document.Id);
        Assert.AreEqual(3u, document.Version);
        Assert.AreEqual("core.Schema", document.Root.TypeId);
    }

    [Test]
    public void CompileFromSource()
    {
        using var source = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(GameSchema));
        using var output = new MemoryStream();
        Schema.Compile(source, output);
        output.Position = 0;
        Assert.AreEqual(Schema.Parse(GameSchema).Hash, Schema.Load(output).Hash);
    }

    [Test]
    public void BuildSchemaWithApi()
    {
        var schema = new Schema("api", 1);
        var point = schema.AddStruct("Point");
        point.AddField("x", TypeReference.S32);
        point.AddField("y", TypeReference.S32, defaultValue: 0);
        point.AddField("label", TypeReference.String.AsNullable());
        schema.Validate();
        Assert.IsTrue(schema.IsFrozen);
        Assert.Throws<InvalidOperationException>(() => schema.AddStruct("Other"));
        Assert.AreEqual("@sgs 1\n@id api\n@version 1\n\nstruct Point {\n  x: s32\n  y: s32 = 0\n  label: string?\n}\n", schema.ToText());
    }

    [Test]
    public void CoreSchemaHashIsZero()
    {
        Assert.AreEqual(0UL, Schema.Core.Hash);
        Assert.AreEqual(10, Schema.Core.Types.Count);
        Assert.AreEqual("Schema", Schema.Core.Types[8].Name);
    }
}
