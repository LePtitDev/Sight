using System;
using System.Globalization;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Data;

/// <summary>
/// Value of an untyped document (dynamic tree)
/// </summary>
public abstract class DataValue
{
    private protected DataValue()
    {
    }

    /// <summary>
    /// The <c>null</c> value
    /// </summary>
    public static DataValue Null => DataNull.Instance;

    /// <summary>
    /// Kind of the value
    /// </summary>
    public abstract TypeKind Kind { get; }

    /// <summary>
    /// Indicates whether the value is <c>null</c>
    /// </summary>
    public bool IsNull => Kind == TypeKind.Null;

    /// <summary>
    /// Type identity (<c>schema.Type</c>) of a struct or enum value, otherwise <c>null</c>
    /// </summary>
    public virtual string? TypeId => null;

    /// <summary>
    /// Number of items of a list, map or struct
    /// </summary>
    public virtual int Count => throw InvalidAccess("Count");

    /// <summary>
    /// Field of a struct or value of a map with string keys
    /// </summary>
    public virtual DataValue this[string key]
    {
        get => throw InvalidAccess("string indexer");
        set => throw InvalidAccess("string indexer");
    }

    /// <summary>
    /// Item of a list
    /// </summary>
    public virtual DataValue this[int index]
    {
        get => throw InvalidAccess("integer indexer");
        set => throw InvalidAccess("integer indexer");
    }

    /// <summary>
    /// Get the value as a boolean
    /// </summary>
    public virtual bool AsBoolean() => throw InvalidAccess(nameof(AsBoolean));

    /// <summary>
    /// Get the value as a signed 64-bit integer
    /// </summary>
    public virtual long AsInt64() => throw InvalidAccess(nameof(AsInt64));

    /// <summary>
    /// Get the value as an unsigned 64-bit integer
    /// </summary>
    public virtual ulong AsUInt64() => throw InvalidAccess(nameof(AsUInt64));

    /// <summary>
    /// Get the value as a 64-bit float
    /// </summary>
    public virtual double AsDouble() => throw InvalidAccess(nameof(AsDouble));

    /// <summary>
    /// Get the value as a string (string value or enum value name)
    /// </summary>
    public virtual string AsString() => throw InvalidAccess(nameof(AsString));

    /// <summary>
    /// Get the content of a <c>list&lt;u8&gt;</c> (<c>bytes</c>): the stored bytes when the list was created or read from bytes, a copy otherwise
    /// </summary>
    public virtual ReadOnlyMemory<byte> AsBytes() => throw InvalidAccess(nameof(AsBytes));

    /// <summary>
    /// Get the value as a signed 32-bit integer
    /// </summary>
    public int AsInt32() => checked((int)AsInt64());

    /// <summary>
    /// Get the value as a 32-bit float
    /// </summary>
    public float AsSingle() => (float)AsDouble();

    /// <summary>
    /// Create a value from a CLR primitive (bool, number, string, byte array), or return it if it is already a <see cref="DataValue"/>
    /// </summary>
    public static DataValue From(object? value) => value switch
    {
        null => Null,
        DataValue data => data,
        bool v => v,
        byte v => v,
        sbyte v => v,
        short v => v,
        ushort v => v,
        int v => v,
        uint v => v,
        long v => v,
        ulong v => v,
        float v => v,
        double v => v,
        string v => v,
        byte[] v => v,
        _ => throw new ArgumentException($"Cannot convert {value.GetType()} to a data value", nameof(value)),
    };

#pragma warning disable CS1591
    public static implicit operator DataValue(bool value) => DataScalar.FromBoolean(value);
    public static implicit operator DataValue(byte[] value) => new DataList(value);
    public static implicit operator DataValue(byte value) => DataScalar.FromUInt64(TypeKind.U8, value);
    public static implicit operator DataValue(sbyte value) => DataScalar.FromInt64(TypeKind.S8, value);
    public static implicit operator DataValue(ushort value) => DataScalar.FromUInt64(TypeKind.U16, value);
    public static implicit operator DataValue(short value) => DataScalar.FromInt64(TypeKind.S16, value);
    public static implicit operator DataValue(uint value) => DataScalar.FromUInt64(TypeKind.U32, value);
    public static implicit operator DataValue(int value) => DataScalar.FromInt64(TypeKind.S32, value);
    public static implicit operator DataValue(ulong value) => DataScalar.FromUInt64(TypeKind.U64, value);
    public static implicit operator DataValue(long value) => DataScalar.FromInt64(TypeKind.S64, value);
    public static implicit operator DataValue(float value) => DataScalar.FromDouble(TypeKind.F32, value);
    public static implicit operator DataValue(double value) => DataScalar.FromDouble(TypeKind.F64, value);
    public static implicit operator DataValue(string? value) => value == null ? Null : DataScalar.FromString(value);
#pragma warning restore CS1591

    private protected InvalidOperationException InvalidAccess(string member)
    {
        return new InvalidOperationException($"{member} is not supported by a value of kind {Kind}");
    }
}

/// <summary>
/// The <c>null</c> value
/// </summary>
public sealed class DataNull : DataValue
{
    private DataNull()
    {
    }

    /// <summary>
    /// Unique instance
    /// </summary>
    public static DataNull Instance { get; } = new();

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Null;

    /// <inheritdoc />
    public override string ToString() => "null";
}

/// <summary>
/// Boolean, number or string value
/// </summary>
public sealed class DataScalar : DataValue, IEquatable<DataScalar>
{
    private readonly TypeKind _kind;
    private readonly ulong _bits;
    private readonly string? _string;

    private DataScalar(TypeKind kind, ulong bits, string? value)
    {
        _kind = kind;
        _bits = bits;
        _string = value;
    }

    /// <inheritdoc />
    public override TypeKind Kind => _kind;

    /// <summary>
    /// Create a boolean value
    /// </summary>
    public static DataScalar FromBoolean(bool value) => new(TypeKind.Bool, value ? 1UL : 0UL, null);

    /// <summary>
    /// Create a signed integer value
    /// </summary>
    public static DataScalar FromInt64(TypeKind kind, long value)
    {
        if (!kind.IsInteger())
            throw new ArgumentException($"{kind} is not an integer kind", nameof(kind));

        if (kind.IsUnsignedInteger())
        {
            if (value < 0)
                throw new OverflowException($"Value {value} is out of range for {kind.ToKeyword()}");

            return FromUInt64(kind, (ulong)value);
        }

        if (!DataRanges.Fits(kind, value))
            throw new OverflowException($"Value {value} is out of range for {kind.ToKeyword()}");

        return new DataScalar(kind, (ulong)value, null);
    }

    /// <summary>
    /// Create an unsigned integer value
    /// </summary>
    public static DataScalar FromUInt64(TypeKind kind, ulong value)
    {
        if (!kind.IsInteger())
            throw new ArgumentException($"{kind} is not an integer kind", nameof(kind));

        if (kind.IsSignedInteger())
        {
            if (value > long.MaxValue)
                throw new OverflowException($"Value {value} is out of range for {kind.ToKeyword()}");

            return FromInt64(kind, (long)value);
        }

        if (!DataRanges.Fits(kind, value))
            throw new OverflowException($"Value {value} is out of range for {kind.ToKeyword()}");

        return new DataScalar(kind, value, null);
    }

    /// <summary>
    /// Create a floating point value
    /// </summary>
    public static DataScalar FromDouble(TypeKind kind, double value)
    {
        if (!kind.IsFloat())
            throw new ArgumentException($"{kind} is not a floating point kind", nameof(kind));

        if (kind == TypeKind.F32)
            value = (float)value;

        return new DataScalar(kind, (ulong)BitConverter.DoubleToInt64Bits(value), null);
    }

    /// <summary>
    /// Create a string value
    /// </summary>
    public static DataScalar FromString(string value) => new(TypeKind.String, 0, value ?? throw new ArgumentNullException(nameof(value)));

    /// <inheritdoc />
    public override bool AsBoolean() => _kind == TypeKind.Bool ? _bits != 0 : base.AsBoolean();

    /// <inheritdoc />
    public override long AsInt64()
    {
        if (_kind.IsSignedInteger())
            return (long)_bits;

        if (_kind.IsUnsignedInteger())
            return _bits <= long.MaxValue ? (long)_bits : throw new OverflowException($"Value {_bits} does not fit in a signed 64-bit integer");

        return base.AsInt64();
    }

    /// <inheritdoc />
    public override ulong AsUInt64()
    {
        if (_kind.IsUnsignedInteger())
            return _bits;

        if (_kind.IsSignedInteger())
            return (long)_bits >= 0 ? _bits : throw new OverflowException($"Value {(long)_bits} does not fit in an unsigned 64-bit integer");

        return base.AsUInt64();
    }

    /// <inheritdoc />
    public override double AsDouble()
    {
        if (_kind.IsFloat())
            return BitConverter.Int64BitsToDouble((long)_bits);

        if (_kind.IsSignedInteger())
            return (long)_bits;

        if (_kind.IsUnsignedInteger())
            return _bits;

        return base.AsDouble();
    }

    /// <inheritdoc />
    public override string AsString() => _string ?? base.AsString();

    /// <summary>
    /// Raw 64-bit pattern of an integer value
    /// </summary>
    internal long AsInt64Bits() => (long)_bits;

    /// <inheritdoc />
    public bool Equals(DataScalar? other)
    {
        return other != null && _kind == other._kind && _bits == other._bits && string.Equals(_string, other._string, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as DataScalar);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)_kind * 397) ^ _bits.GetHashCode() ^ (_string != null ? StringComparer.Ordinal.GetHashCode(_string) : 0);
        }
    }

    /// <inheritdoc />
    public override string ToString() => _kind switch
    {
        TypeKind.Bool => _bits != 0 ? "true" : "false",
        TypeKind.String => _string!,
        _ when _kind.IsSignedInteger() => ((long)_bits).ToString(CultureInfo.InvariantCulture),
        _ when _kind.IsUnsignedInteger() => _bits.ToString(CultureInfo.InvariantCulture),
        _ => AsDouble().ToString("R", CultureInfo.InvariantCulture),
    };
}

/// <summary>
/// Value of an enum
/// </summary>
public sealed class DataEnum : DataValue, IEquatable<DataEnum>
{
    /// <summary>
    /// Initialize a new instance of <see cref="DataEnum"/>
    /// </summary>
    public DataEnum(EnumValueDefinition value)
    {
        Value = value;
    }

    /// <summary>
    /// Initialize a new instance of <see cref="DataEnum"/> from a value name
    /// </summary>
    public DataEnum(EnumDefinition definition, string name)
    {
        Value = definition.FindValue(name) ?? throw new ArgumentException($"Enum '{definition.QualifiedName}' has no value '{name}'", nameof(name));
    }

    /// <summary>
    /// Value definition
    /// </summary>
    public EnumValueDefinition Value { get; }

    /// <summary>
    /// Enum definition
    /// </summary>
    public EnumDefinition Definition => Value.Owner;

    /// <summary>
    /// Name of the value
    /// </summary>
    public string Name => Value.Name;

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Enum;

    /// <inheritdoc />
    public override string? TypeId => Definition.QualifiedName;

    /// <inheritdoc />
    public override string AsString() => Value.Name;

    /// <inheritdoc />
    public override long AsInt64() => Value.Value;

    /// <inheritdoc />
    public bool Equals(DataEnum? other) => other != null && ReferenceEquals(Value, other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as DataEnum);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => Value.Name;
}

/// <summary>
/// External reference, at a <c>ref&lt;T&gt;</c> or <c>any</c> position: root value (empty key) or export of a dependency
/// </summary>
public sealed class DataExtRef : DataValue
{
    /// <summary>
    /// Initialize a new instance of <see cref="DataExtRef"/>
    /// </summary>
    public DataExtRef(string dependencyId, string key = "", StructDefinition? targetType = null)
    {
        DependencyId = dependencyId ?? throw new ArgumentNullException(nameof(dependencyId));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        TargetType = targetType;
    }

    /// <summary>
    /// Identifier of the dependency (declared in <see cref="DataDocument.Dependencies"/>)
    /// </summary>
    public string DependencyId { get; }

    /// <summary>
    /// Export name, or empty for the root value of the dependency
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Declared target type, required when the value is stored in an <c>any</c> position
    /// </summary>
    public StructDefinition? TargetType { get; }

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Ref;

    /// <inheritdoc />
    public override string ToString() => Key.Length == 0 ? $"<{DependencyId}>" : $"<{DependencyId}#{Key}>";
}

/// <summary>
/// Reference to a struct of the same document, as stored in an <c>any</c> position
/// </summary>
/// <remarks>At <c>ref&lt;T&gt;</c> positions, the referenced <see cref="DataStruct"/> is used directly.</remarks>
public sealed class DataReference : DataValue
{
    /// <summary>
    /// Initialize a new instance of <see cref="DataReference"/>
    /// </summary>
    public DataReference(DataStruct target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    /// <summary>
    /// Referenced struct
    /// </summary>
    public DataStruct Target { get; }

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Ref;

    /// <inheritdoc />
    public override string? TypeId => Target.TypeId;

    /// <inheritdoc />
    public override DataValue this[string key]
    {
        get => Target[key];
        set => Target[key] = value;
    }
}
