using System;
using System.Collections.Generic;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Binary;

/// <summary>
/// Decodes untyped values according to a schema type table
/// </summary>
internal sealed class DataBinaryReader
{
    private readonly BinaryDecoder _decoder;
    private readonly TypeTable _table;
    private readonly IReadOnlyList<DependencyReference>? _dependencies;
    private readonly List<DataStruct> _objects = new();
    private readonly SerializationLimits _limits;
    private int _depth;

    public DataBinaryReader(BinaryDecoder decoder, TypeTable table, IReadOnlyList<DependencyReference>? dependencies, SerializationLimits limits)
    {
        _decoder = decoder;
        _table = table;
        _dependencies = dependencies;
        _limits = limits;
    }

    /// <summary>
    /// Read a struct value of an exact type, without polymorphism tag
    /// </summary>
    public DataStruct ReadExactStruct(StructDefinition type)
    {
        if (type.IsAbstract)
            throw Error($"cannot instantiate abstract struct '{type.QualifiedName}'");

        var value = new DataStruct(type);
        ReadStructBody(value);
        return value;
    }

    /// <summary>
    /// Read an exported object (<c>ref&lt;T&gt;</c> with T its exact type)
    /// </summary>
    public DataStruct ReadExport(StructDefinition type)
    {
        var x = _decoder.ReadVarUInt();
        if (x > 0)
        {
            var existing = GetObject(x - 1);
            if (!ReferenceEquals(existing.Definition, type))
                throw Error($"exported object type '{existing.Definition.QualifiedName}' does not match '{type.QualifiedName}'");

            return existing;
        }

        if (type.IsAbstract)
            throw Error($"cannot instantiate abstract struct '{type.QualifiedName}'");

        var value = new DataStruct(type);
        AddObject(value);
        ReadStructBody(value);
        return value;
    }

    /// <summary>
    /// Read a value of a declared type, nullable positions being prefixed by a presence byte
    /// </summary>
    public DataValue ReadItem(TypeReference type)
    {
        if (type.IsNullable)
        {
            var presence = _decoder.ReadByte();
            if (presence == 0)
                return DataValue.Null;

            if (presence != 1)
                throw Error($"invalid presence byte 0x{presence:X2}");
        }

        return ReadValue(type);
    }

    private DataValue ReadValue(TypeReference type)
    {
        switch (type.Kind)
        {
            case TypeKind.Bool:
                return DataScalar.FromBoolean(_decoder.ReadBoolean());
            case TypeKind.U8:
            case TypeKind.U16:
            case TypeKind.U32:
            case TypeKind.U64:
                return Checked(() => DataScalar.FromUInt64(type.Kind, ReadUnsigned(type.Kind, type.IsFixed)));
            case TypeKind.S8:
            case TypeKind.S16:
            case TypeKind.S32:
            case TypeKind.S64:
                return Checked(() => DataScalar.FromInt64(type.Kind, ReadSigned(type.Kind, type.IsFixed)));
            case TypeKind.F32:
                return DataScalar.FromDouble(TypeKind.F32, _decoder.ReadSingle());
            case TypeKind.F64:
                return DataScalar.FromDouble(TypeKind.F64, _decoder.ReadDouble());
            case TypeKind.String:
                return DataScalar.FromString(_decoder.ReadString());
            case TypeKind.Enum:
                return ReadEnum(type.EnumDefinition!);
            case TypeKind.List:
                return ReadList(type.ElementType!);
            case TypeKind.Map:
                return ReadMap(type);
            case TypeKind.Struct:
                return ReadStruct(type.StructDefinition!);
            case TypeKind.Ref:
                return ReadRef(type.StructDefinition!);
            case TypeKind.ExtRef:
                var index = _decoder.ReadVarUInt();
                if (_dependencies == null || index >= (ulong)_dependencies.Count)
                    throw Error($"invalid dependency index {index}");

                return new DataExtRef(_dependencies[(int)index].Id, _decoder.ReadString(), type.StructDefinition);
            case TypeKind.Any:
                return ReadAny();
            default:
                throw Error($"type {type} cannot be read");
        }
    }

    private ulong ReadUnsigned(TypeKind kind, bool isFixed)
    {
        return kind switch
        {
            TypeKind.U8 => _decoder.ReadByte(),
            TypeKind.U16 when isFixed => _decoder.ReadFixedUInt16(),
            TypeKind.U32 when isFixed => _decoder.ReadFixedUInt32(),
            TypeKind.U64 when isFixed => _decoder.ReadFixedUInt64(),
            _ => _decoder.ReadVarUInt(),
        };
    }

    private long ReadSigned(TypeKind kind, bool isFixed)
    {
        return kind switch
        {
            TypeKind.S8 => (sbyte)_decoder.ReadByte(),
            TypeKind.S16 when isFixed => (short)_decoder.ReadFixedUInt16(),
            TypeKind.S32 when isFixed => (int)_decoder.ReadFixedUInt32(),
            TypeKind.S64 when isFixed => (long)_decoder.ReadFixedUInt64(),
            _ => _decoder.ReadVarInt(),
        };
    }

    private DataEnum ReadEnum(EnumDefinition definition)
    {
        long value;
        if (definition.Underlying.IsSignedInteger())
        {
            value = Checked(() => DataScalar.FromInt64(definition.Underlying, ReadSigned(definition.Underlying, false))).AsInt64();
        }
        else
        {
            var raw = ReadUnsigned(definition.Underlying, false);
            if (!DataRanges.Fits(definition.Underlying, raw))
                throw Error($"value {raw} out of range for enum '{definition.QualifiedName}'");

            value = (long)raw;
        }

        var item = definition.FindValue(value) ?? throw Error($"value {value} is not declared by enum '{definition.QualifiedName}'");
        return new DataEnum(item);
    }

    private DataList ReadList(TypeReference elementType)
    {
        Enter();
        var count = ReadCount();
        var list = new DataList(elementType);
        if (elementType.Kind == TypeKind.Bool && !elementType.IsNullable)
        {
            byte current = 0;
            for (var i = 0; i < count; i++)
            {
                if ((i & 7) == 0)
                    current = _decoder.ReadByte();

                list.Add(DataScalar.FromBoolean((current & (1 << (i & 7))) != 0));
            }

            var used = count & 7;
            if (used != 0 && (current >> used) != 0)
                throw Error("non-zero padding bits in packed booleans");
        }
        else
        {
            for (var i = 0; i < count; i++)
                list.Add(ReadItem(elementType));
        }

        _depth--;
        return list;
    }

    private DataMap ReadMap(TypeReference type)
    {
        Enter();
        var count = ReadCount();
        var map = new DataMap(type.KeyType, type.ValueType);
        for (var i = 0; i < count; i++)
        {
            var key = ReadValue(type.KeyType!);
            var value = ReadItem(type.ValueType!);
            if (map.ContainsKey(key))
                throw Error($"duplicate map key '{key}'");

            map.Add(key, value);
        }

        _depth--;
        return map;
    }

    private DataStruct ReadStruct(StructDefinition declared)
    {
        var value = new DataStruct(ReadConcreteType(declared));
        ReadStructBody(value);
        return value;
    }

    private DataStruct ReadRef(StructDefinition declared)
    {
        var x = _decoder.ReadVarUInt();
        if (x > 0)
        {
            var existing = GetObject(x - 1);
            if (!existing.Definition.IsAssignableTo(declared))
                throw Error($"referenced object of type '{existing.Definition.QualifiedName}' is not a '{declared.QualifiedName}'");

            return existing;
        }

        var value = new DataStruct(ReadConcreteType(declared));
        AddObject(value);
        ReadStructBody(value);
        return value;
    }

    private StructDefinition ReadConcreteType(StructDefinition declared)
    {
        var set = _table.GetConcreteSet(declared);
        if (set.Length == 0)
            throw Error($"struct '{declared.QualifiedName}' has no concrete type");

        if (set.Length == 1)
            return set[0];

        var tag = _decoder.ReadVarUInt();
        return tag < (ulong)set.Length ? set[(int)tag] : throw Error($"invalid polymorphism tag {tag} for '{declared.QualifiedName}'");
    }

    private void ReadStructBody(DataStruct value)
    {
        Enter();
        var fields = value.Definition.AllFields;
        var slotCount = 0;
        foreach (var field in fields)
        {
            if (field.HasPresenceSlot)
                slotCount++;
        }

        var bitmapSize = (slotCount + 7) / 8;
        Span<byte> bitmap = bitmapSize <= 64 ? stackalloc byte[bitmapSize] : new byte[bitmapSize];
        _decoder.ReadBytes(bitmap);
        if ((slotCount & 7) != 0 && (bitmap[bitmapSize - 1] >> (slotCount & 7)) != 0)
            throw Error($"non-zero padding bits in the presence bitmap of '{value.Definition.QualifiedName}'");

        var slot = 0;
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            if (field.HasPresenceSlot)
            {
                var present = (bitmap[slot >> 3] & (1 << (slot & 7))) != 0;
                slot++;
                value.SetAt(i, present ? ReadValue(field.Type) : field.DefaultValue ?? DataValue.Null);
            }
            else
            {
                value.SetAt(i, ReadValue(field.Type));
            }
        }

        _depth--;
    }

    private DataValue ReadAny()
    {
        var type = ReadDescriptor(isTopLevel: true);
        if (type.Kind == TypeKind.Null)
            return DataValue.Null;

        if (type.Kind == TypeKind.Struct)
            return ReadExactStruct(type.StructDefinition!);

        var value = ReadValue(type);
        return type.Kind == TypeKind.Ref ? new DataReference((DataStruct)value) : value;
    }

    private TypeReference ReadDescriptor(bool isTopLevel)
    {
        var header = _decoder.ReadByte();
        if ((header & 0x80) != 0)
            throw Error($"invalid type descriptor 0x{header:X2}");

        var kind = (TypeKind)(header & 0x1F);
        var isNullable = (header & 0x20) != 0;
        var isFixed = (header & 0x40) != 0;
        if (kind > TypeKind.Any || (isNullable && (isTopLevel || kind is TypeKind.Any or TypeKind.Null)) || (isFixed && !kind.AllowsFixed()))
            throw Error($"invalid type descriptor 0x{header:X2}");

        TypeReference type;
        switch (kind)
        {
            case TypeKind.Null:
                return TypeReference.Null;
            case TypeKind.Enum:
                type = TypeReference.Of(_table.Get(_decoder.ReadVarUInt()) as EnumDefinition ?? throw Error("type descriptor index is not an enum"));
                break;
            case TypeKind.Struct:
                type = TypeReference.Of(_table.GetStruct(_decoder.ReadVarUInt()));
                break;
            case TypeKind.Ref:
                type = TypeReference.Ref(_table.GetStruct(_decoder.ReadVarUInt()));
                break;
            case TypeKind.ExtRef:
                type = TypeReference.ExtRef(_table.GetStruct(_decoder.ReadVarUInt()));
                break;
            case TypeKind.List:
                type = TypeReference.List(ReadDescriptor(isTopLevel: false));
                break;
            case TypeKind.Map:
                type = TypeReference.Map(ReadDescriptor(isTopLevel: false), ReadDescriptor(isTopLevel: false));
                break;
            default:
                type = TypeReference.Primitive(kind);
                break;
        }

        return type.WithFlags(isNullable, isFixed);
    }

    private int ReadCount()
    {
        var count = _decoder.ReadVarUInt();
        if (count > (ulong)_limits.MaxCollectionCount)
            throw Error($"collection count {count} exceeds the limit of {_limits.MaxCollectionCount}");

        return (int)count;
    }

    private DataStruct GetObject(ulong id)
    {
        return id < (ulong)_objects.Count ? _objects[(int)id] : throw Error($"reference to unknown object id {id}");
    }

    private void AddObject(DataStruct value)
    {
        if (_objects.Count >= _limits.MaxObjectCount)
            throw Error($"object count exceeds the limit of {_limits.MaxObjectCount}");

        _objects.Add(value);
    }

    private void Enter()
    {
        if (++_depth > _limits.MaxDepth)
            throw Error($"nesting depth exceeds the limit of {_limits.MaxDepth}");
    }

    private T Checked<T>(Func<T> func)
    {
        try
        {
            return func();
        }
        catch (OverflowException e)
        {
            throw Error(e.Message);
        }
    }

    private SerializationException Error(string message) => new($"Invalid data at position {_decoder.Position}: {message}");
}
