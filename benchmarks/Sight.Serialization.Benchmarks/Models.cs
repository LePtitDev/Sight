using System.Text.Json.Serialization;
using MessagePack;

[assembly: Sight.Serialization.SerializationSchema("bench", 1)]

namespace Sight.Serialization.Benchmarks;

// Small object: a handful of scalars and a short list
[SerializationType("bench.Person")]
[MessagePackObject]
public class Person
{
    [Key(0)]
    public string Name { get; set; } = string.Empty;

    [Key(1)]
    public int Age { get; set; }

    [Key(2)]
    public string Email { get; set; } = string.Empty;

    [Key(3)]
    public double Score { get; set; }

    [Key(4)]
    public bool IsActive { get; set; }

    [Key(5)]
    public long CreatedAt { get; set; }

    [Key(6)]
    public List<string> Tags { get; set; } = new();
}

// Large list of structs: numeric data
[SerializationType("bench.Sample")]
[MessagePackObject]
public struct Sample
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public float X { get; set; }

    [Key(2)]
    public float Y { get; set; }

    [Key(3)]
    public float Z { get; set; }

    [Key(4)]
    public long Timestamp { get; set; }

    [Key(5)]
    public SampleQuality Quality { get; set; }
}

[SerializationType("bench.Quality")]
public enum SampleQuality : byte
{
    Low,
    Medium,
    High,
}

[SerializationType("bench.SampleSet")]
[MessagePackObject]
public class SampleSet
{
    [Key(0)]
    public string Source { get; set; } = string.Empty;

    [Key(1)]
    public List<Sample> Samples { get; set; } = new();
}

// Object graph: polymorphism, shared references and cycles (parent / children)
[SerializationType("bench.Scene")]
public class Scene
{
    public string Name { get; set; } = string.Empty;

    [SerializationReference]
    public List<Entity> Entities { get; set; } = new();
}

[SerializationType("bench.Vector3")]
public struct Vector3
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }
}

[SerializationType("bench.Entity")]
[SerializationDerivedType(typeof(MeshEntity))]
[SerializationDerivedType(typeof(LightEntity))]
[SerializationDerivedType(typeof(GroupEntity))]
[JsonPolymorphic]
[JsonDerivedType(typeof(MeshEntity), "mesh")]
[JsonDerivedType(typeof(LightEntity), "light")]
[JsonDerivedType(typeof(GroupEntity), "group")]
public abstract class Entity
{
    public string Name { get; set; } = string.Empty;

    public Vector3 Position { get; set; }

    [SerializationReference]
    public Entity? Parent { get; set; }
}

[SerializationType("bench.Mesh")]
public class MeshEntity : Entity
{
    public string Mesh { get; set; } = string.Empty;

    public string Material { get; set; } = string.Empty;
}

[SerializationType("bench.Light")]
public class LightEntity : Entity
{
    public float Intensity { get; set; }

    public uint Color { get; set; }
}

[SerializationType("bench.Group")]
public class GroupEntity : Entity
{
    [SerializationReference]
    public List<Entity> Children { get; set; } = new();
}
