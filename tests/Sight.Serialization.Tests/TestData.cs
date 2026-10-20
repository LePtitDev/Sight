using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Tests;

/// <summary>
/// Document exercising every type of the data model
/// </summary>
internal static class TestData
{
    public const string SchemaText = """
        @sgs 1
        @id test
        @version 2

        enum Color : s16 { Red = -1, Green, Blue = 1000 }

        abstract struct Shape {
          name: string
        }

        struct Circle : Shape { radius: f64 }
        struct Rect : Shape { w: u32, h: u32 = 1 }

        struct Node {
          value: s32
          next: ref<Node>?
        }

        struct All {
          b: bool
          u8: u8
          u16: u16
          u32: u32 = 7
          u64: u64 @fixed
          s8: s8
          s16: s16 @fixed
          s32: s32
          s64: s64
          f32: f32
          f64: f64
          text: string
          color: Color = Green
          flags: list<bool>
          bytes: list<u8>
          numbers: list<s32?>
          names: map<string, string?>
          byColor: map<Color, f32>
          shapes: list<Shape>
          main: Shape?
          cycle: ref<Node>
          also: ref<Node>
          icon: ref<Shape>
          dyn: list<any>
          long: string
        }
        """;

    public static DataDocument CreateDocument()
    {
        var schema = Schema.Parse(SchemaText);
        var all = Struct(schema, "All");
        all["b"] = true;
        all["u8"] = (byte)200;
        all["u16"] = (ushort)60000;
        all["u32"] = 7u;
        all["u64"] = ulong.MaxValue;
        all["s8"] = (sbyte)-100;
        all["s16"] = (short)-30000;
        all["s32"] = int.MinValue;
        all["s64"] = long.MaxValue;
        all["f32"] = 1.5f;
        all["f64"] = -0.1;
        all["text"] = "héllo \"world\"\n\ttab";
        all["color"] = new DataEnum((EnumDefinition)schema.FindType("Color")!, "Red");
        all["flags"] = new DataList(new DataValue[] { true, false, true, true, false, false, false, false, true });
        all["bytes"] = new DataList(new DataValue[] { (byte)1, (byte)2, (byte)255 });
        all["numbers"] = new DataList(new[] { 1, DataValue.Null, -3 });

        var names = new DataMap();
        names.Add("a", "x");
        names.Add("b", DataValue.Null);
        all["names"] = names;

        var byColor = new DataMap();
        byColor.Add(new DataEnum((EnumDefinition)schema.FindType("Color")!, "Blue"), 0.25f);
        all["byColor"] = byColor;

        var circle = Struct(schema, "Circle");
        circle["name"] = "c";
        circle["radius"] = 2.0;
        var rect = Struct(schema, "Rect");
        rect["name"] = "r";
        rect["w"] = 3u;
        rect["h"] = 1u;
        all["shapes"] = new DataList(new DataValue[] { circle, rect });
        all["main"] = DataValue.Null;

        var first = Struct(schema, "Node");
        var second = Struct(schema, "Node");
        first["value"] = 1;
        second["value"] = 2;
        first["next"] = second;
        second["next"] = first;
        all["cycle"] = first;
        all["also"] = second;
        all["icon"] = new DataExtRef("assets.shapes", "star");

        var map = new DataMap();
        map.Add("k", 1.25);
        all["dyn"] = new DataList(new DataValue[]
        {
            DataValue.Null,
            42,
            "s",
            new DataEnum((EnumDefinition)schema.FindType("Color")!, "Blue"),
            CloneCircle(schema),
            new DataReference(first),
            map,
            new DataList(new DataValue[] { (byte)9 }, TypeReference.U8),
        });
        all["long"] = new string('z', 100);

        var document = new DataDocument(schema, all) { Id = "test-doc", Version = 12 };
        document.Metadata["author"] = "Arthur";
        document.Metadata["tool"] = "tests";
        document.Dependencies.Add(new DependencyReference("assets.shapes", 3));
        document.Exports.Add(new KeyValuePair<string, DataStruct>("first", first));
        document.Exports.Add(new KeyValuePair<string, DataStruct>("extra", CloneCircle(schema)));
        return document;
    }

    public static void AssertDocument(DataDocument document)
    {
        Assert.AreEqual("test-doc", document.Id);
        Assert.AreEqual(12u, document.Version);
        Assert.AreEqual("Arthur", document.Metadata["author"]);
        Assert.AreEqual(new DependencyReference("assets.shapes", 3), document.Dependencies.Single());

        var all = document.Root;
        Assert.IsTrue(all["b"].AsBoolean());
        Assert.AreEqual(200, all["u8"].AsInt32());
        Assert.AreEqual(60000, all["u16"].AsInt32());
        Assert.AreEqual(7, all["u32"].AsInt32());
        Assert.AreEqual(ulong.MaxValue, all["u64"].AsUInt64());
        Assert.AreEqual(-100, all["s8"].AsInt32());
        Assert.AreEqual(-30000, all["s16"].AsInt32());
        Assert.AreEqual(int.MinValue, all["s32"].AsInt32());
        Assert.AreEqual(long.MaxValue, all["s64"].AsInt64());
        Assert.AreEqual(1.5f, all["f32"].AsSingle());
        Assert.AreEqual(-0.1, all["f64"].AsDouble());
        Assert.AreEqual("héllo \"world\"\n\ttab", all["text"].AsString());
        Assert.AreEqual("Red", all["color"].AsString());
        Assert.AreEqual(new[] { true, false, true, true, false, false, false, false, true }, ((DataList)all["flags"]).Select(x => x.AsBoolean()).ToArray());
        Assert.AreEqual(new[] { 1, 2, 255 }, ((DataList)all["bytes"]).Select(x => x.AsInt32()).ToArray());
        Assert.IsTrue(all["numbers"][1].IsNull);
        Assert.AreEqual(-3, all["numbers"][2].AsInt32());
        Assert.AreEqual("x", all["names"]["a"].AsString());
        Assert.IsTrue(all["names"]["b"].IsNull);
        Assert.AreEqual(0.25f, ((DataMap)all["byColor"]).Single().Value.AsSingle());
        Assert.AreEqual("test.Circle", all["shapes"][0].TypeId);
        Assert.AreEqual(2.0, all["shapes"][0]["radius"].AsDouble());
        Assert.AreEqual("test.Rect", all["shapes"][1].TypeId);
        Assert.AreEqual(1, all["shapes"][1]["h"].AsInt32());
        Assert.IsTrue(all["main"].IsNull);

        var first = all["cycle"];
        var second = all["also"];
        Assert.AreSame(second, first["next"]);
        Assert.AreSame(first, second["next"]);
        Assert.AreEqual(2, second["value"].AsInt32());

        var icon = (DataExtRef)all["icon"];
        Assert.AreEqual("assets.shapes", icon.DependencyId);
        Assert.AreEqual("star", icon.Key);

        var dyn = all["dyn"];
        Assert.IsTrue(dyn[0].IsNull);
        Assert.AreEqual(42, dyn[1].AsInt32());
        Assert.AreEqual(TypeKind.S32, dyn[1].Kind);
        Assert.AreEqual("s", dyn[2].AsString());
        Assert.AreEqual("Blue", dyn[3].AsString());
        Assert.AreEqual("test.Circle", dyn[4].TypeId);
        Assert.AreSame(first, ((DataReference)dyn[5]).Target);
        Assert.AreEqual(1.25, dyn[6]["k"].AsDouble());
        Assert.AreEqual(TypeKind.U8, ((DataList)dyn[7]).ElementType!.Kind);
        Assert.AreEqual(new string('z', 100), all["long"].AsString());

        Assert.AreEqual(2, document.Exports.Count);
        Assert.AreSame(first, document.Exports[0].Value);
        Assert.AreEqual("extra", document.Exports[1].Key);
        Assert.AreEqual(5.0, document.Exports[1].Value["radius"].AsDouble());
    }

    private static DataStruct CloneCircle(Schema schema)
    {
        var circle = Struct(schema, "Circle");
        circle["name"] = "dyn";
        circle["radius"] = 5.0;
        return circle;
    }

    private static DataStruct Struct(Schema schema, string name) => new((StructDefinition)schema.FindType(name)!);
}
