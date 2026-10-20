using System.IO;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;
using Sight.Serialization.Tests.Models;

namespace Sight.Serialization.Tests;

public class MultiSchemaTests
{
    private const string BaseSchema = """
        @sgs 1
        @id rpg
        @version 1

        abstract struct Unit { name: string }
        struct Knight : Unit { armor: u8 }
        struct Army { units: list<Unit> }
        struct Item { name: string }
        """;

    private const string ExtensionSchema = """
        @sgs 1
        @id rpg.mods
        @version 2
        @import base = rpg 1

        struct Dragon : base.Unit { wings: u8 }
        struct Item { power: u8 }
        """;

    private static (Schema Base, Schema Extension, SerializationOptions Options) CreateSchemas()
    {
        var options = new SerializationOptions();
        var baseSchema = Schema.Parse(BaseSchema);
        options.Schemas.Add(baseSchema);
        var extension = Schema.Parse(ExtensionSchema, options);
        options.Schemas.Add(extension);
        return (baseSchema, extension, options);
    }

    private static DataDocument CreateDocument(Schema baseSchema, Schema extension)
    {
        var knight = new DataStruct((StructDefinition)baseSchema.FindType("Knight")!) { ["name"] = "Lancelot", ["armor"] = 3 };
        var dragon = new DataStruct((StructDefinition)extension.FindType("Dragon")!) { ["name"] = "Smaug", ["wings"] = 2 };
        var army = new DataStruct((StructDefinition)baseSchema.FindType("Army")!) { ["units"] = new DataList(new DataValue[] { knight, dragon }) };
        return new DataDocument(new[] { baseSchema, extension }, army);
    }

    [Test]
    public void ImportAlias()
    {
        var (_, extension, _) = CreateSchemas();
        var dragon = (StructDefinition)extension.FindType("Dragon")!;
        Assert.AreEqual("rpg.Unit", dragon.Base!.QualifiedName);

        // The alias is local to the source: the canonical form uses schema names
        var text = extension.ToText();
        StringAssert.Contains("@import rpg 1", text);
        StringAssert.Contains("struct Dragon : rpg.Unit", text);
    }

    [Test]
    public void SchemaMetadata()
    {
        var schema = Schema.Parse("@sgs 1\n@id meta\n@meta author = \"Arthur\"\n@meta \"long key\" = \"x\"\nstruct A { x: u8 }");
        Assert.AreEqual(0u, schema.Version);
        Assert.AreEqual("Arthur", schema.Metadata["author"]);
        Assert.AreEqual("@sgs 1\n@id meta\n@meta author = \"Arthur\"\n@meta \"long key\" = \"x\"\n\nstruct A {\n  x: u8\n}\n", schema.ToText());

        using var compiled = new MemoryStream();
        schema.Save(compiled);
        Assert.AreEqual("Arthur", BinarySerializer.Read(compiled.ToArray()).Metadata["author"]);
        compiled.Position = 0;
        Assert.AreEqual("x", Schema.Load(compiled).Metadata["long key"]);

        // Metadata is not part of the hash
        Assert.AreEqual(Schema.Parse("@sgs 1\n@id meta\nstruct A { x: u8 }").Hash, schema.Hash);
    }

    [TestCase("@id s\nstruct A {}", 1)]
    [TestCase("@sgs 2\n@id s\nstruct A {}", 1)]
    [TestCase("@sgs 1\nstruct A {}", 2)]
    [TestCase("@sgs 1\n@id s\n@id t\nstruct A {}", 3)]
    [TestCase("@sgs 1\n@id s\n@unknown 1\nstruct A {}", 3)]
    [TestCase("@sgs 1\n@id s @version 1\nstruct A {}", 2)]
    [TestCase("@sgs 1\n@id s\nstruct A {}\n@version 1", 4)]
    public void InvalidHeadersAreRejected(string text, int line)
    {
        var exception = Assert.Throws<SerializationException>(() => Schema.Parse(text))!;
        Assert.AreEqual(line, exception.Line, exception.Message);
    }

    [Test]
    public void AliasConflictIsRejected()
    {
        var options = new SerializationOptions();
        options.Schemas.Add(Schema.Parse("@sgs 1\n@id a.b\nstruct T {}"));
        options.Schemas.Add(Schema.Parse("@sgs 1\n@id c\nstruct T {}"));
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\n@import a = c 0\n@import a.b 0\nstruct A { t: a.T }", options));
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\n@import x = c 0\n@import x = a.b 0\nstruct A {}", options));
        Assert.DoesNotThrow(() => Schema.Parse("@sgs 1\n@id s\n@import x = c 0\n@import a.b 0\nstruct A { t: x.T, u: a.b.T }", options));
    }

    [Test]
    public void BinaryRoundTripWithSeveralSchemas()
    {
        var (baseSchema, extension, options) = CreateSchemas();
        foreach (var mode in new[] { SchemaMode.Inline, SchemaMode.Reference, SchemaMode.None })
        {
            var document = CreateDocument(baseSchema, extension);
            document.SchemaMode = mode;
            var read = BinarySerializer.Read(BinarySerializer.Write(document), options);
            Assert.AreEqual("rpg.mods.Dragon", read.Root["units"][1].TypeId, mode.ToString());
            Assert.AreEqual(2, read.Root["units"][1]["wings"].AsInt32());
            if (mode != SchemaMode.None)
                Assert.AreEqual(new[] { "rpg", "rpg.mods" }, read.Schemas.Select(x => x.Name).ToArray());
        }
    }

    [Test]
    public void ReferenceModeListsEverySchema()
    {
        var (baseSchema, extension, _) = CreateSchemas();
        var document = CreateDocument(baseSchema, extension);
        document.SchemaMode = SchemaMode.Reference;
        var bytes = BinarySerializer.Write(document);
        Assert.AreEqual(2, bytes[9], "number of referenced schemas");
    }

    [Test]
    public void TextRoundTripWithSeveralSchemas()
    {
        var (baseSchema, extension, options) = CreateSchemas();
        var document = CreateDocument(baseSchema, extension);
        document.SchemaMode = SchemaMode.Reference;
        var text = TextSerializer.Write(document);
        StringAssert.Contains("@schema rpg 1\n@schema rpg.mods 2\n", text);
        StringAssert.Contains("Dragon { name: \"Smaug\", wings: 2 }", text);
        var read = TextSerializer.Read(text, options);
        Assert.AreEqual(text, TextSerializer.Write(read));

        document.SchemaMode = SchemaMode.Inline;
        var inline = TextSerializer.Write(document);
        StringAssert.Contains("@schema {\n  @id rpg.mods\n  @version 2\n  @import rpg 1\n", inline);
        Assert.AreEqual(inline, TextSerializer.Write(TextSerializer.Read(inline)));
    }

    [Test]
    public void AmbiguousTypeNamesMustBeQualified()
    {
        var (_, _, options) = CreateSchemas();
        const string header = "@sbdl 1\n@schema rpg 1\n@schema rpg.mods 2\n";
        var exception = Assert.Throws<SerializationException>(() => TextSerializer.Read(header + "Item { name: \"x\" }", options))!;
        StringAssert.Contains("ambiguous", exception.Message);
        Assert.AreEqual("rpg.mods.Item", TextSerializer.Read(header + "rpg.mods.Item { power: 1 }", options).Root.TypeId);
    }

    [Test]
    public void TypedDerivedTypeInExtensionSchema()
    {
        var player = new Player { Name = "p", Party = { new Dragon { Name = "Smaug", Wings = 4 } } };
        var bytes = BinarySerializer.Write(player);
        var read = BinarySerializer.Read<Player>(bytes);
        Assert.AreEqual(4, ((Dragon)read.Party[0]).Wings);

        // The extension schema is listed as a file schema next to the root schema
        var document = BinarySerializer.Read(bytes);
        Assert.AreEqual(new[] { "game", "mods" }, document.Schemas.Select(x => x.Name).ToArray());

        var character = BinarySerializer.Read<Character>(BinarySerializer.Write<Character>(new Dragon { Name = "D", Wings = 1 }));
        Assert.IsInstanceOf<Dragon>(character);
    }
}
