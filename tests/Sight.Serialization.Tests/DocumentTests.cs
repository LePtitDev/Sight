using System.IO;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Tests;

public class DocumentTests
{
    [Test]
    public void SpecificationExample()
    {
        var schema = Schema.Parse("@sgs 1\n@id demo\n@version 1\nstruct Point {\n  x: s32\n  y: s32\n  label: string?\n}");
        var point = new DataStruct((StructDefinition)schema.FindType("Point")!)
        {
            ["x"] = 3,
            ["y"] = -2,
            ["label"] = "p",
        };

        var document = new DataDocument(schema, point) { Id = "p" };
        var bytes = BinarySerializer.Write(document);
        const string expected =
            "53 42 44 4C 01 00 20 02 70 00 " + // header
            "01 08 64 65 6D 6F 01 00 01 " + // schema section: 1 schema, "demo", version 1, no imports, 1 type
            "00 00 0A 50 6F 69 6E 74 00 03 " + // StructDef tag, bitmap, "Point", not abstract, 3 fields
            "02 78 00 08 00 00 00 00 " + // x: s32
            "02 79 00 08 00 00 00 00 " + // y: s32
            "0A 6C 61 62 65 6C 00 0C 01 00 00 00 " + // label: string?
            "00 01 06 03 02 70"; // root index, bitmap, x, y, label
        Assert.AreEqual(expected, PrimitiveTests.Hex(bytes));

        var read = BinarySerializer.Read(bytes);
        Assert.AreEqual("p", read.Id);
        Assert.AreEqual(3, read.Root["x"].AsInt32());
        Assert.AreEqual(-2, read.Root["y"].AsInt32());
        Assert.AreEqual("p", read.Root["label"].AsString());
    }

    [Test]
    public void FullDocumentRoundTrip()
    {
        foreach (var compression in new[] { Compression.None, Compression.Deflate })
        {
            var document = TestData.CreateDocument();
            var options = new SerializationOptions { Compression = compression };
            var bytes = BinarySerializer.Write(document, options);
            var read = BinarySerializer.Read(bytes, options);
            TestData.AssertDocument(read);

            // Writing again gives identical bytes (determinism)
            Assert.AreEqual(bytes, BinarySerializer.Write(read, options));
        }
    }

    [Test]
    public void TextRoundTrip()
    {
        var document = TestData.CreateDocument();
        var text = TextSerializer.Write(document);
        var read = TextSerializer.Read(text);
        TestData.AssertDocument(read);
        Assert.AreEqual(text, TextSerializer.Write(read));

        // Text and binary are equivalent
        Assert.AreEqual(BinarySerializer.Write(document), BinarySerializer.Write(read));
    }

    [Test]
    public void TextSpecificationExample()
    {
        var options = new SerializationOptions();
        options.Schemas.Add(Schema.Parse(SchemaTests.GameSchema));
        const string text = """
            @sbdl 1
            @id "level-01"
            @version 3
            @meta author = "Arthur"
            @schema game 3
            @dep items = game.items 1

            Player {
              party: [
                Warrior { name: "Conan", rage: 12 }
                &merlin Mage {
                  name: "Merlin"
                  level: 7
                  mana: 300
                }
              ]
              inventory: [
                { name: "Potion", count: 3 }
                { name: "Torch" }                 // count absent → default (1)
              ]
              stats: { "str": 12.5, "dex": 8 }
              leader: *merlin
              hash: 0x9F3A_22C1_0000_0001
              experience: -12
              extra: 42u32
            }

            @export hero = *merlin
            """;

        var document = TextSerializer.Read(text, options);
        Assert.AreEqual("level-01", document.Id);
        Assert.AreEqual(3u, document.Version);
        Assert.AreEqual("Arthur", document.Metadata["author"]);
        Assert.AreEqual(SchemaMode.Reference, document.SchemaMode);

        var root = document.Root;
        Assert.AreEqual("game.Warrior", root["party"][0].TypeId);
        Assert.AreEqual(1, root["party"][0]["level"].AsInt32());
        Assert.AreSame(root["party"][1], root["leader"]);
        Assert.AreSame(root["party"][1], document.Exports[0].Value);
        Assert.AreEqual(1, root["inventory"][1]["count"].AsInt32());
        Assert.AreEqual(8.0, root["stats"]["dex"].AsDouble());
        Assert.AreEqual(0x9F3A22C100000001UL, root["hash"].AsUInt64());
        Assert.AreEqual("Mage", root["class"].AsString());
        Assert.AreEqual(TypeKind.U32, root["extra"].Kind);

        var canonical = TextSerializer.Write(document, options);
        StringAssert.Contains("&1 Mage { name: \"Merlin\", level: 7, mana: 300 }", canonical);
        StringAssert.Contains("leader: *1", canonical);
        StringAssert.Contains("extra: 42u32", canonical);
        StringAssert.Contains("@export hero = *1", canonical);
        StringAssert.DoesNotContain("count: 1", canonical);
        Assert.AreEqual(canonical, TextSerializer.Write(TextSerializer.Read(canonical, options), options));
    }

    [TestCase("@sbdl 1\n@schema s 1\nA { x: 1 y: 2 }", 3)]
    [TestCase("@sbdl 1\n@schema s 1\nA { x: 1, z: 2 }", 3)]
    [TestCase("@sbdl 1\n@schema s 1\nA { x: 300, y: 1 }", 3)]
    [TestCase("@sbdl 1\n@schema s 1\nA { x: 1.5, y: 1 }", 3)]
    [TestCase("@sbdl 1\n@schema s 1\nA { x: 1, y: 1, x: 2 }", 3)]
    [TestCase("@sbdl 1\n@schema s 1\nA { x: 1 }", 3)]
    [TestCase("@sbdl 1\n@schema s 1\n{ x: 1, y: 1 }", 3)]
    [TestCase("@sbdl 1\n@schema s 1\nA { x: 1, y: 1, s: \"a\\q\" }", 3)]
    [TestCase("@sbdl 2\n@schema s 1\nA { x: 1, y: 1 }", 1)]
    [TestCase("@schema s 1\nA { x: 1, y: 1 }", 1)]
    public void InvalidTextIsRejected(string text, int line)
    {
        var options = new SerializationOptions();
        options.Schemas.Add(Schema.Parse("@sgs 1\n@id s\n@version 1\nstruct A { x: u8, y: u8, s: string? }"));
        var exception = Assert.Throws<SerializationException>(() => TextSerializer.Read(text, options))!;
        Assert.AreEqual(line, exception.Line, exception.Message);
    }

    [Test]
    public void UnknownFieldSuggestion()
    {
        var options = new SerializationOptions();
        options.Schemas.Add(Schema.Parse("@sgs 1\n@id s\n@version 1\nstruct A { level: u8 }"));
        var exception = Assert.Throws<SerializationException>(() => TextSerializer.Read("@sbdl 1\n@schema s 1\nA { levle: 1 }", options))!;
        StringAssert.Contains("did you mean 'level'", exception.Message);
    }

    [Test]
    public void TextLiterals()
    {
        var options = new SerializationOptions();
        options.Schemas.Add(Schema.Parse("@sgs 1\n@id s\n@version 1\nstruct A { a: list<any>, b: list<u8>, c: string, d: list<f64> }"));
        const string text = "@sbdl 1\n@schema s 1\nA {\n  a: [null, true, -7, 0x2A, 0b101, 1_000, 1.5f32, 2f32, nan, -inf, \"\\u{1F600}\", b64\"AQID\", list<u8> [4, 5], map<s32, f32> { 1: 0.5 }]\n  b: b64\"AQID\"\n  c: \"\"\"\n    line 1\n      line 2\n    \"\"\"\n  d: [1, 1e21, 1.5e-7, 0.1]\n}";
        var document = TextSerializer.Read(text, options);
        var a = document.Root["a"];
        Assert.IsTrue(a[0].IsNull);
        Assert.IsTrue(a[1].AsBoolean());
        Assert.AreEqual(-7, a[2].AsInt64());
        Assert.AreEqual(42, a[3].AsInt64());
        Assert.AreEqual(5, a[4].AsInt64());
        Assert.AreEqual(1000, a[5].AsInt64());
        Assert.AreEqual(TypeKind.F32, a[6].Kind);
        Assert.AreEqual(TypeKind.F32, a[7].Kind);
        Assert.IsTrue(double.IsNaN(a[8].AsDouble()));
        Assert.IsTrue(double.IsNegativeInfinity(a[9].AsDouble()));
        Assert.AreEqual("\U0001F600", a[10].AsString());
        Assert.AreEqual(3, a[11].Count);
        Assert.AreEqual(2, a[12].Count);
        Assert.AreEqual(0.5, ((DataMap)a[13])[DataScalar.FromInt64(TypeKind.S32, 1)].AsDouble());
        Assert.AreEqual(3, document.Root["b"].Count);
        Assert.AreEqual("line 1\n  line 2", document.Root["c"].AsString());

        var canonical = TextSerializer.Write(document, options);
        StringAssert.Contains("b: b64\"AQID\"", canonical);
        StringAssert.Contains("d: [1.0, 1e21, 1.5e-7, 0.1]", canonical);
        StringAssert.Contains("map<s32, f32> { 1: 0.5 }", canonical);
        var reread = TextSerializer.Read(canonical, options);
        Assert.AreEqual(BinarySerializer.Write(document, options), BinarySerializer.Write(reread, options));
    }

    [Test]
    public void SchemaModes()
    {
        var schema = Schema.Parse("@sgs 1\n@id s\n@version 4\nstruct A { x: u8 }");
        var root = new DataStruct((StructDefinition)schema.FindType("A")!) { ["x"] = 7 };

        var reference = new DataDocument(schema, root) { SchemaMode = SchemaMode.Reference };
        var bytes = BinarySerializer.Write(reference);
        Assert.Throws<SerializationException>(() => BinarySerializer.Read(bytes));
        var options = new SerializationOptions();
        options.Schemas.Add(schema);
        Assert.AreEqual(7, BinarySerializer.Read(bytes, options).Root["x"].AsInt32());

        var other = new SerializationOptions();
        other.Schemas.Add(Schema.Parse("@sgs 1\n@id s\n@version 4\nstruct A { x: u16 }"));
        StringAssert.Contains("hash", Assert.Throws<SerializationException>(() => BinarySerializer.Read(bytes, other))!.Message);

        var none = new DataDocument(schema, root) { SchemaMode = SchemaMode.None };
        bytes = BinarySerializer.Write(none);
        Assert.Throws<SerializationException>(() => BinarySerializer.Read(bytes));
        Assert.AreEqual(7, BinarySerializer.Read(bytes, new SerializationOptions { Schemas = { schema } }).Root["x"].AsInt32());
    }

    [TestCase("54 42 44 4C 01 00 20 00 00")]
    [TestCase("53 42 44 4C 02 00 20 00 00")]
    [TestCase("53 42 44 4C 01 03 20 00 00")]
    [TestCase("53 42 44 4C 01 80 20 00 00")]
    [TestCase("53 42 44 4C 01 00 20 00")]
    public void MalformedHeadersAreRejected(string hex)
    {
        var bytes = hex.Split(' ').Select(x => Convert.ToByte(x, 16)).ToArray();
        Assert.Throws<SerializationException>(() => BinarySerializer.Read(bytes));
    }

    [Test]
    public void TrailingBytesAreRejected()
    {
        var bytes = BinarySerializer.Write(TestData.CreateDocument()).Concat(new byte[] { 0 }).ToArray();
        Assert.Throws<SerializationException>(() => BinarySerializer.Read(bytes));
    }

    [Test]
    public void TruncatedDataIsRejected()
    {
        var bytes = BinarySerializer.Write(TestData.CreateDocument());
        for (var length = 0; length < bytes.Length; length += 7)
            Assert.Throws<SerializationException>(() => BinarySerializer.Read(bytes.Take(length).ToArray()), $"length {length}");
    }

    [Test]
    public void DepthLimit()
    {
        var schema = Schema.Parse("@sgs 1\n@id s\n@version 1\nstruct Node { next: Node? }");
        var type = (StructDefinition)schema.FindType("Node")!;
        var root = new DataStruct(type);
        var current = root;
        for (var i = 0; i < 50; i++)
        {
            var next = new DataStruct(type);
            current["next"] = next;
            current = next;
        }

        var bytes = BinarySerializer.Write(new DataDocument(schema, root));
        var options = new SerializationOptions();
        options.Limits.MaxDepth = 10;
        Assert.Throws<SerializationException>(() => BinarySerializer.Read(bytes, options));
        Assert.DoesNotThrow(() => BinarySerializer.Read(bytes));
    }

    [Test]
    public void StreamsAndAsync()
    {
        var document = TestData.CreateDocument();
        using var binary = new MemoryStream();
        BinarySerializer.WriteAsync(binary, document).GetAwaiter().GetResult();
        binary.Position = 0;
        TestData.AssertDocument(BinarySerializer.ReadAsync(binary).GetAwaiter().GetResult());

        using var text = new MemoryStream();
        TextSerializer.WriteAsync(text, document).GetAwaiter().GetResult();
        text.Position = 0;
        TestData.AssertDocument(TextSerializer.ReadAsync(text).GetAwaiter().GetResult());
    }
}
