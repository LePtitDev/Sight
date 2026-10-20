using System.IO;
using System.Numerics;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;
using Sight.Serialization.Tests.Models;

[assembly: Sight.Serialization.SerializationConverter(typeof(Vector3Converter))]

namespace Sight.Serialization.Tests;

/// <summary>
/// Type aliases, bytes, positional structs and converters
/// </summary>
public class ExtensionTests
{
    private const string SceneSchema = """
        @sgs 1
        @id scene
        @version 1

        type Color = u32
        type Palette = list<Color>
        type Name = Label
        type Label = string

        enum Shape : u8 { Box, Sphere }

        struct Vec3 @positional {
          x: f32
          y: f32
          z: f32
        }

        struct Light {
          name:     Name
          tint:     Color = 0xFFFFFFFF
          hash:     Color @fixed
          palette:  Palette
          position: Vec3
          shape:    Shape
          data:     bytes
          extra:    any
        }
        """;

    [Test]
    public void ParseAliases()
    {
        var schema = Schema.Parse(SceneSchema);
        var light = (StructDefinition)schema.FindType("Light")!;
        var tint = light.FindField("tint")!.Type;
        Assert.AreEqual(TypeKind.U32, tint.Kind);
        Assert.AreEqual("Color", tint.Alias!.Name);
        Assert.AreEqual(TypeReference.U32, tint);
        Assert.AreEqual(4294967295UL, light.FindField("tint")!.DefaultValue!.AsUInt64());
        Assert.IsTrue(light.FindField("hash")!.Type.IsFixed);
        Assert.AreEqual("Palette", light.FindField("palette")!.Type.ToString(schema));
        Assert.AreEqual(TypeKind.List, light.FindField("palette")!.Type.Kind);
        Assert.AreEqual(TypeKind.String, light.FindField("name")!.Type.Kind);
        Assert.AreEqual("bytes", light.FindField("data")!.Type.ToString());
        Assert.IsTrue(light.FindField("data")!.Type.IsBytes);
        Assert.IsTrue(((StructDefinition)schema.FindType("Vec3")!).IsPositional);

        var text = schema.ToText();
        StringAssert.Contains("type Color = u32\n", text);
        StringAssert.Contains("type Palette = list<Color>\n", text);
        StringAssert.Contains("struct Vec3 @positional {", text);
        StringAssert.Contains("  hash: Color @fixed\n", text);
        StringAssert.Contains("  data: bytes\n", text);
        Assert.AreEqual(schema.Hash, Schema.Parse(text).Hash);

        // Compiled schemas keep aliases and the positional flag
        using var stream = new MemoryStream();
        schema.Save(stream);
        stream.Position = 0;
        var loaded = Schema.Load(stream);
        Assert.AreEqual(schema.Hash, loaded.Hash);
        Assert.AreEqual(text, loaded.ToText());
    }

    [Test]
    public void InvalidAliasesAndPositionalStructs()
    {
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\ntype A = B\ntype B = A"));
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\ntype A = u32?"));
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\nstruct P { x: u8 }\ntype Q = P\nstruct R { r: ref<Q> }"));
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\nstruct bytes { x: u8 }"));
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\nstruct P @positional { x: list<u8> }"));
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\nstruct P @positional { x: u8? }"));
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\nabstract struct P @positional { x: u8 }"));
    }

    [Test]
    public void TextPositionalAndAliases()
    {
        var options = new SerializationOptions();
        options.Schemas.Add(Schema.Parse(SceneSchema));
        const string text = """
            @sbdl 1
            @schema scene 1

            Light {
              name: "sun"
              hash: 7
              palette: [1, 2]
              position: (1.5, 0, -3)
              shape: Sphere
              data: b64"AQID"
              extra: [Color 255, Vec3 (1, 2, 3), bytes b64"AQ==", Name "x"]
            }
            """;

        var document = TextSerializer.Read(text, options);
        var root = document.Root;
        Assert.AreEqual(4294967295UL, root["tint"].AsUInt64());
        Assert.AreEqual(-3.0, root["position"]["z"].AsDouble());
        Assert.AreEqual(new byte[] { 1, 2, 3 }, root["data"].AsBytes().ToArray());
        var extra = root["extra"];
        Assert.AreEqual(TypeKind.U32, extra[0].Kind);
        Assert.AreEqual("scene.Vec3", extra[1].TypeId);
        Assert.AreEqual(new byte[] { 1 }, extra[2].AsBytes().ToArray());
        Assert.AreEqual(TypeKind.String, extra[3].Kind);

        // Canonical output: positional form for @positional structs, structural types in 'any'
        var canonical = TextSerializer.Write(document, options);
        StringAssert.Contains("position: (1.5, 0.0, -3.0)", canonical);
        StringAssert.Contains("extra: [255u32, Vec3 (1.0, 2.0, 3.0), b64\"AQ==\", \"x\"]", canonical);
        var binary = BinarySerializer.Write(document, options);
        Assert.AreEqual(binary, BinarySerializer.Write(TextSerializer.Read(canonical, options), options));
        Assert.AreEqual(canonical, TextSerializer.Write(BinarySerializer.Read(binary, options), options));
    }

    [Test]
    public void InvalidPositionalValues()
    {
        var options = new SerializationOptions();
        options.Schemas.Add(Schema.Parse("@sgs 1\n@id s\n@version 1\nstruct P { x: u8, y: u8 }\nstruct L { items: list<u8> }\nstruct A { p: P, l: L, x: any }"));

        // Positional values are accepted for any eligible struct, with every field
        var document = TextSerializer.Read("@sbdl 1\n@schema s 1\nA { p: (1, 2), l: { items: [] }, x: 1 }", options);
        Assert.AreEqual(2, document.Root["p"]["y"].AsInt64());
        StringAssert.Contains("p: { x: 1, y: 2 }", TextSerializer.Write(document, options));

        Assert.Throws<SerializationException>(() => TextSerializer.Read("@sbdl 1\n@schema s 1\nA { p: (1), l: { items: [] }, x: 1 }", options));
        Assert.Throws<SerializationException>(() => TextSerializer.Read("@sbdl 1\n@schema s 1\nA { p: (1, 2), l: ([]), x: 1 }", options));
        Assert.Throws<SerializationException>(() => TextSerializer.Read("@sbdl 1\n@schema s 1\nA { p: (1, 2), l: { items: [] }, x: (1, 2) }", options));
    }

    [Test]
    public void BytesAreStoredWithoutConversion()
    {
        var schema = Schema.Parse("@sgs 1\n@id s\nstruct A { data: bytes }");
        var root = new DataStruct((StructDefinition)schema.FindType("A")!) { ["data"] = new byte[] { 1, 2, 3 } };
        var document = BinarySerializer.Read(BinarySerializer.Write(new DataDocument(schema, root)));
        var data = (DataList)document.Root["data"];
        Assert.IsNotNull(data.RawBytes);
        Assert.AreEqual(3, data.Count);
        Assert.AreEqual(2, data[1].AsInt64());

        // Modifying the list converts it to values
        data.Add((byte)4);
        Assert.IsNull(data.RawBytes);
        Assert.AreEqual(new byte[] { 1, 2, 3, 4 }, data.AsBytes().ToArray());
        Assert.Throws<ArgumentException>(() => new DataList(new byte[1], TypeReference.U16));
    }

    [Test]
    public void ConvertersInSchema()
    {
        var text = Schema.FromType<Scene>().ToText();
        StringAssert.Contains("type Color = u32\n", text);
        StringAssert.Contains("struct Vec3 @positional {", text);
        StringAssert.Contains("  tint: Color\n", text);
        StringAssert.Contains("  highlight: Color?\n", text);
        StringAssert.Contains("  palette: list<Color>\n", text);
        StringAssert.Contains("  position: Vec3\n", text);
        StringAssert.Contains("  speed: s32\n", text);
        StringAssert.Contains("  hash: Color @fixed\n", text);
        StringAssert.Contains("  memory: bytes\n", text);
        StringAssert.Contains("  segment: bytes\n", text);
    }

    [Test]
    public void ConvertersRoundTrip()
    {
        var scene = new Scene
        {
            Tint = new Color(255, 128, 64, 255),
            Highlight = new Color(1, 2, 3, 4),
            Palette = { new Color(0, 0, 0, 255), new Color(255, 255, 255, 255) },
            Position = new Vector3(1, 2, 3),
            Speed = 1.5f,
            Hash = new Color(9, 9, 9, 9),
            Memory = new byte[] { 1, 2 },
            Segment = new ArraySegment<byte>(new byte[] { 0, 7, 8, 0 }, 1, 2),
            Extra = new Color(5, 6, 7, 8),
        };

        var copy = BinarySerializer.Read<Scene>(BinarySerializer.Write(scene));
        Assert.AreEqual(scene.Tint, copy.Tint);
        Assert.AreEqual(scene.Highlight, copy.Highlight);
        Assert.AreEqual(scene.Palette, copy.Palette);
        Assert.AreEqual(scene.Position, copy.Position);
        Assert.AreEqual(1.5f, copy.Speed);
        Assert.AreEqual(scene.Hash, copy.Hash);
        Assert.AreEqual(new byte[] { 1, 2 }, copy.Memory.ToArray());
        Assert.AreEqual(new byte[] { 7, 8 }, copy.Segment.ToArray());

        // In 'any', a converted value is stored (and read back) as its surrogate
        Assert.AreEqual(new Color(5, 6, 7, 8).Rgba, copy.Extra);

        var text = TextSerializer.Write(scene);
        StringAssert.Contains("position: (1.0, 2.0, 3.0)", text);
        StringAssert.Contains("speed: 384", text);
        var reread = TextSerializer.Read<Scene>(text);
        Assert.AreEqual(scene.Tint, reread.Tint);
        Assert.AreEqual(scene.Position, reread.Position);

        scene.Highlight = null;
        Assert.IsNull(BinarySerializer.Read<Scene>(BinarySerializer.Write(scene)).Highlight);
    }

    private static SerializationOptions LibraryOptions()
    {
        var library = new Library { Textures = { new Texture { Path = "a.png" }, new Texture { Path = "b.png" } } };
        var libraryOptions = new SerializationOptions { Id = "assets.library", Version = 4 };
        libraryOptions.Exports["second"] = library.Textures[1];
        var libraryBytes = BinarySerializer.Write(library, libraryOptions);

        var options = new SerializationOptions();
        options.Dependencies.Add("assets.library", 4, () => new MemoryStream(libraryBytes));
        return options;
    }

    [Test]
    public void ReferencesMixLocalAndExternalValues()
    {
        var options = LibraryOptions();
        var local = new Texture { Path = "local.png" };
        var atlas = new Atlas
        {
            Links = { local, local, new Link<Texture>("assets.library", "second") },
            Main = local,
        };

        var bytes = BinarySerializer.Write(atlas, options);
        var read = BinarySerializer.Read<Atlas>(bytes, options);
        Assert.IsFalse(read.Links[0].IsExternal);
        Assert.AreEqual("local.png", read.Links[0].Resolve().Path);
        Assert.AreSame(read.Links[0].Resolve(), read.Links[1].Resolve());
        Assert.AreSame(read.Links[0].Resolve(), read.Main);
        Assert.IsTrue(read.Links[2].IsExternal);
        Assert.IsFalse(read.Links[2].IsResolved);
        Assert.AreEqual("b.png", read.Links[2].Resolve().Path);

        // Untyped: the same position holds structs (in place or shared) and external references
        var document = BinarySerializer.Read(bytes, options);
        var links = document.Root["links"];
        Assert.AreSame(links[0], links[1]);
        Assert.AreEqual("second", ((DataExtRef)links[2]).Key);

        var text = TextSerializer.Write(document, options);
        StringAssert.Contains("links: [&1 { path: \"local.png\" }, *1, <library#second>]", text);
        Assert.AreEqual(bytes, BinarySerializer.Write(TextSerializer.Read(text, options), options));
    }

    [Test]
    public void ExternalReferencesOfPlainMembersAreResolvedWhileReading()
    {
        var options = LibraryOptions();
        options.Schemas.Add(Schema.FromType<Atlas>());
        const string text = "@sbdl 1\n@schema lib 0\n@dep library = assets.library 4\n\nAtlas { links: [<library>], main: <library#second> }";

        var atlas = TextSerializer.Read<Atlas>(text, options);
        Assert.AreEqual("b.png", atlas.Main!.Path);
        Assert.IsTrue(atlas.Links[0].IsExternal);

        // Root of a dependency of another type: error at resolution
        Assert.Throws<SerializationException>(() => TextSerializer.Read<Atlas>(text.Replace("<library#second>", "<library>"), options));
    }

    [Test]
    public void ExternalReferencesInAny()
    {
        var schema = Schema.Parse("@sgs 1\n@id s\n@version 1\nstruct T { path: string }\nstruct A { x: any }");
        var options = new SerializationOptions();
        options.Schemas.Add(schema);
        const string text = "@sbdl 1\n@schema s 1\n@dep lib = assets.library 4\n\nA { x: ref<T> <lib#second> }";

        var document = TextSerializer.Read(text, options);
        var value = (DataExtRef)document.Root["x"];
        Assert.AreEqual("s.T", value.TargetType!.QualifiedName);
        var reread = BinarySerializer.Read(BinarySerializer.Write(document, options), options);
        StringAssert.Contains("x: ref<T> <library#second>", TextSerializer.Write(reread, options));

        Assert.Throws<SerializationException>(() => TextSerializer.Read(text.Replace("ref<T> ", string.Empty), options));
        Assert.Throws<SerializationException>(() => Schema.Parse("@sgs 1\n@id s\nstruct T { x: extref<T> }"));
    }

    [Test]
    public void InvalidConverters()
    {
        var exception = Assert.Throws<SerializationException>(() => Schema.FromType<WrongConverterModel>())!;
        StringAssert.Contains("converts", exception.Message);
    }
}
