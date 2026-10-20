using System.Numerics;

namespace Sight.Serialization.Tests.Models;

/// <summary>
/// Type without serialized members, converted to an integer wherever it is used
/// </summary>
[SerializationConverter(typeof(ColorConverter))]
public readonly struct Color : IEquatable<Color>
{
    public Color(byte r, byte g, byte b, byte a)
    {
        Rgba = (uint)((r << 24) | (g << 16) | (b << 8) | a);
    }

    public Color(uint rgba)
    {
        Rgba = rgba;
    }

    public uint Rgba { get; }

    public bool Equals(Color other) => Rgba == other.Rgba;

    public override bool Equals(object? obj) => obj is Color other && Equals(other);

    public override int GetHashCode() => (int)Rgba;
}

// Named converter: values are declared with the alias 'scene.Color'
[SerializationType("scene.Color")]
public sealed class ColorConverter : SerializationConverter<Color, uint>
{
    public override uint ToSurrogate(Color value) => value.Rgba;

    public override Color FromSurrogate(uint surrogate) => new(surrogate);
}

[SerializationType("scene.Vec3", IsPositional = true)]
public sealed class Vec3Data
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }
}

// Third-party type, converted with an assembly-level declaration
public sealed class Vector3Converter : SerializationConverter<Vector3, Vec3Data>
{
    public override Vec3Data ToSurrogate(Vector3 value) => new() { X = value.X, Y = value.Y, Z = value.Z };

    public override Vector3 FromSurrogate(Vec3Data surrogate) => new(surrogate.X, surrogate.Y, surrogate.Z);
}

// Parameterized converter: fixed-point number with a given number of fractional bits
public sealed class FixedPointConverter : SerializationConverter<float, int>
{
    private readonly float _scale;

    public FixedPointConverter(int fractionalBits)
    {
        _scale = 1 << fractionalBits;
    }

    public override int ToSurrogate(float value) => (int)Math.Round(value * _scale);

    public override float FromSurrogate(int surrogate) => surrogate / _scale;
}

[SerializationType("scene.Scene")]
public class Scene
{
    public Color Tint { get; set; }

    public Color? Highlight { get; set; }

    public List<Color> Palette { get; set; } = new();

    public Vector3 Position { get; set; }

    [SerializationConverter(typeof(FixedPointConverter), 8)]
    public float Speed { get; set; }

    [SerializationFixed]
    public Color Hash { get; set; }

    public ReadOnlyMemory<byte> Memory { get; set; }

    public ArraySegment<byte> Segment { get; set; }

    public object? Extra { get; set; }
}

[SerializationType("scene.Wrong")]
public class WrongConverterModel
{
    [SerializationConverter(typeof(FixedPointConverter), 8)]
    public int Wrong { get; set; }
}
