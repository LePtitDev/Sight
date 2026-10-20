using System;
using System.Collections.Generic;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Binary;

/// <summary>
/// Encodes untyped values according to a schema type table
/// </summary>
internal sealed class DataBinaryWriter
{
    private readonly BinaryEncoder _encoder;
    private readonly TypeTable _table;
    private readonly Func<DataExtRef, int>? _dependencyIndex;
    private readonly Dictionary<DataStruct, int> _objectIds = new(ReferenceComparer<DataStruct>.Instance);
    private readonly int _maxDepth;
    private int _depth;

    public DataBinaryWriter(BinaryEncoder encoder, TypeTable table, Func<DataExtRef, int>? dependencyIndex, int maxDepth)
    {
        _encoder = encoder;
        _table = table;
        _dependencyIndex = dependencyIndex;
        _maxDepth = maxDepth;
    }

    /// <summary>
    /// Write a struct value of an exact type, without polymorphism tag (root, exports, canonical schema)
    /// </summary>
    public void WriteExactStruct(StructDefinition type, DataStruct value, string path)
    {
        if (!ReferenceEquals(value.Definition, type))
            throw Error(path, $"expected a value of type '{type.QualifiedName}', got '{value.Definition.QualifiedName}'");

        WriteStructBody(value, path);
    }

    /// <summary>
    /// Write an exported object as <c>ref&lt;T&gt;</c> with T its exact type
    /// </summary>
    public void WriteExport(DataStruct value, string path)
    {
        if (_objectIds.TryGetValue(value, out var id))
        {
            _encoder.WriteVarUInt((ulong)id + 1);
            return;
        }

        _encoder.WriteVarUInt(0);
        _objectIds.Add(value, _objectIds.Count);
        WriteStructBody(value, path);
    }

    /// <summary>
    /// Write a value of a declared type, nullable positions being prefixed by a presence byte
    /// </summary>
    public void WriteItem(TypeReference type, DataValue? value, string path)
    {
        if (type.IsNullable)
        {
            if (value == null || value.IsNull)
            {
                _encoder.WriteByte(0);
                return;
            }

            _encoder.WriteByte(1);
        }

        WriteValue(type, value, path);
    }

    private void WriteValue(TypeReference type, DataValue? value, string path)
    {
        if (value == null || value.IsNull)
        {
            if (type.Kind == TypeKind.Any)
            {
                _encoder.WriteByte((byte)TypeKind.Null);
                return;
            }

            throw Error(path, $"null is not allowed for type {type}");
        }

        switch (type.Kind)
        {
            case TypeKind.Bool:
                _encoder.WriteBoolean(DataConvert.CoerceScalar(value, type, path).AsBoolean());
                break;
            case TypeKind.U8:
            case TypeKind.U16:
            case TypeKind.U32:
            case TypeKind.U64:
                WriteUnsigned(type.Kind, type.IsFixed, DataConvert.CoerceScalar(value, type, path).AsUInt64());
                break;
            case TypeKind.S8:
            case TypeKind.S16:
            case TypeKind.S32:
            case TypeKind.S64:
                WriteSigned(type.Kind, type.IsFixed, DataConvert.CoerceScalar(value, type, path).AsInt64());
                break;
            case TypeKind.F32:
                _encoder.WriteSingle((float)DataConvert.CoerceScalar(value, type, path).AsDouble());
                break;
            case TypeKind.F64:
                _encoder.WriteDouble(DataConvert.CoerceScalar(value, type, path).AsDouble());
                break;
            case TypeKind.String:
                _encoder.WriteString(DataConvert.CoerceScalar(value, type, path).AsString());
                break;
            case TypeKind.Enum:
                var item = (DataEnum)DataConvert.CoerceScalar(value, type, path);
                WriteEnumValue(item.Definition, item.Value.Value);
                break;
            case TypeKind.List:
                WriteList(type.ElementType!, value as DataList ?? throw Error(path, $"expected a list, got {value.Kind}"), path);
                break;
            case TypeKind.Map:
                WriteMap(type, value as DataMap ?? throw Error(path, $"expected a map, got {value.Kind}"), path);
                break;
            case TypeKind.Struct:
                WriteStruct(type.StructDefinition!, AsStruct(value, path), path);
                break;
            case TypeKind.Ref:
                WriteRef(type.StructDefinition!, AsStruct(value, path), path);
                break;
            case TypeKind.ExtRef:
                var extRef = value as DataExtRef ?? throw Error(path, $"expected an external reference, got {value.Kind}");
                if (_dependencyIndex == null)
                    throw Error(path, "external references are not allowed here");

                _encoder.WriteVarUInt((ulong)_dependencyIndex(extRef));
                _encoder.WriteString(extRef.Key);
                break;
            case TypeKind.Any:
                WriteAny(value, path);
                break;
            default:
                throw Error(path, $"type {type} cannot be written");
        }
    }

    private void WriteUnsigned(TypeKind kind, bool isFixed, ulong value)
    {
        switch (kind)
        {
            case TypeKind.U8:
                _encoder.WriteByte((byte)value);
                break;
            case TypeKind.U16 when isFixed:
                _encoder.WriteFixedUInt16((ushort)value);
                break;
            case TypeKind.U32 when isFixed:
                _encoder.WriteFixedUInt32((uint)value);
                break;
            case TypeKind.U64 when isFixed:
                _encoder.WriteFixedUInt64(value);
                break;
            default:
                _encoder.WriteVarUInt(value);
                break;
        }
    }

    private void WriteSigned(TypeKind kind, bool isFixed, long value)
    {
        switch (kind)
        {
            case TypeKind.S8:
                _encoder.WriteByte((byte)(sbyte)value);
                break;
            case TypeKind.S16 when isFixed:
                _encoder.WriteFixedUInt16((ushort)(short)value);
                break;
            case TypeKind.S32 when isFixed:
                _encoder.WriteFixedUInt32((uint)(int)value);
                break;
            case TypeKind.S64 when isFixed:
                _encoder.WriteFixedUInt64((ulong)value);
                break;
            default:
                _encoder.WriteVarInt(value);
                break;
        }
    }

    private void WriteEnumValue(EnumDefinition definition, long value)
    {
        if (definition.Underlying.IsSignedInteger())
            WriteSigned(definition.Underlying, false, value);
        else
            WriteUnsigned(definition.Underlying, false, (ulong)value);
    }

    private void WriteList(TypeReference elementType, DataList list, string path)
    {
        Enter(path);
        _encoder.WriteVarUInt((ulong)list.Count);
        if (elementType.Kind == TypeKind.U8 && !elementType.IsNullable && list.RawBytes is { } bytes)
        {
            _encoder.WriteBytes(bytes);
        }
        else if (elementType.Kind == TypeKind.Bool && !elementType.IsNullable)
        {
            byte current = 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (DataConvert.CoerceScalar(list[i], elementType, path).AsBoolean())
                    current |= (byte)(1 << (i & 7));

                if ((i & 7) == 7)
                {
                    _encoder.WriteByte(current);
                    current = 0;
                }
            }

            if ((list.Count & 7) != 0)
                _encoder.WriteByte(current);
        }
        else
        {
            for (var i = 0; i < list.Count; i++)
                WriteItem(elementType, list[i], path + "[" + i + "]");
        }

        _depth--;
    }

    private void WriteMap(TypeReference type, DataMap map, string path)
    {
        Enter(path);
        _encoder.WriteVarUInt((ulong)map.Count);
        foreach (var entry in map)
        {
            WriteValue(type.KeyType!, entry.Key, path + "{key}");
            WriteItem(type.ValueType!, entry.Value, path + "[" + entry.Key + "]");
        }

        _depth--;
    }

    private void WriteStruct(StructDefinition declared, DataStruct value, string path)
    {
        WriteTag(declared, value.Definition, path);
        WriteStructBody(value, path);
    }

    private void WriteRef(StructDefinition declared, DataStruct value, string path)
    {
        if (_objectIds.TryGetValue(value, out var id))
        {
            if (!value.Definition.IsAssignableTo(declared))
                throw Error(path, $"referenced object of type '{value.Definition.QualifiedName}' is not a '{declared.QualifiedName}'");

            _encoder.WriteVarUInt((ulong)id + 1);
            return;
        }

        _encoder.WriteVarUInt(0);
        _objectIds.Add(value, _objectIds.Count);
        WriteStruct(declared, value, path);
    }

    private void WriteTag(StructDefinition declared, StructDefinition actual, string path)
    {
        var set = _table.GetConcreteSet(declared);
        var index = Array.IndexOf(set, actual);
        if (index < 0)
            throw Error(path, $"a value of type '{actual.QualifiedName}' is not valid for type '{declared.QualifiedName}'");

        if (set.Length > 1)
            _encoder.WriteVarUInt((ulong)index);
    }

    private void WriteStructBody(DataStruct value, string path)
    {
        Enter(path);
        var fields = value.Definition.AllFields;
        var slotCount = 0;
        foreach (var field in fields)
        {
            if (field.HasPresenceSlot)
                slotCount++;
        }

        if (slotCount > 0)
        {
            byte current = 0;
            var slot = 0;
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (!field.HasPresenceSlot)
                    continue;

                if (IsPresent(field, value.GetAt(i)))
                    current |= (byte)(1 << (slot & 7));

                if ((slot & 7) == 7)
                {
                    _encoder.WriteByte(current);
                    current = 0;
                }

                slot++;
            }

            if ((slotCount & 7) != 0)
                _encoder.WriteByte(current);
        }

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var fieldValue = value.GetAt(i);
            var fieldPath = path + "." + field.Name;
            if (field.HasPresenceSlot)
            {
                if (IsPresent(field, fieldValue))
                    WriteValue(field.Type, fieldValue, fieldPath);
            }
            else
            {
                if (fieldValue == null)
                    throw Error(fieldPath, "required field is not set");

                WriteValue(field.Type, fieldValue, fieldPath);
            }
        }

        _depth--;
    }

    private static bool IsPresent(FieldDefinition field, DataValue? value)
    {
        if (value == null || value.IsNull)
            return false;

        if (field.DefaultValue == null)
            return true;

        var coerced = DataConvert.CoerceScalar(value, field.Type, field.Name);
        return !coerced.Equals(field.DefaultValue);
    }

    private void WriteAny(DataValue value, string path)
    {
        var type = DescribeAny(value, path);
        WriteDescriptor(type);
        if (value is DataStruct data)
            WriteStructBody(data, path);
        else
            WriteValue(type, value is DataReference reference ? reference.Target : value, path);
    }

    private TypeReference DescribeAny(DataValue value, string path)
    {
        return value switch
        {
            DataScalar => TypeReference.Primitive(value.Kind),
            DataEnum data => TypeReference.Of(data.Definition),
            DataStruct data => TypeReference.Of(data.Definition),
            DataReference data => TypeReference.Ref(data.Target.Definition),
            DataList data => TypeReference.List(data.ElementType ?? TypeReference.Any),
            DataMap data => TypeReference.Map(data.KeyType ?? TypeReference.String, data.ValueType ?? TypeReference.Any),
            DataExtRef data => TypeReference.ExtRef(data.TargetType ?? throw Error(path, "an external reference stored in 'any' requires a target type")),
            _ => throw Error(path, $"a value of kind {value.Kind} cannot be stored in 'any'"),
        };
    }

    private void WriteDescriptor(TypeReference type)
    {
        var header = (byte)type.Kind;
        if (type.IsNullable)
            header |= 0x20;

        if (type.IsFixed)
            header |= 0x40;

        _encoder.WriteByte(header);
        switch (type.Kind)
        {
            case TypeKind.Enum:
            case TypeKind.Struct:
            case TypeKind.Ref:
            case TypeKind.ExtRef:
                _encoder.WriteVarUInt((ulong)_table.IndexOf(type.Definition!));
                break;
            case TypeKind.List:
                WriteDescriptor(type.ElementType!);
                break;
            case TypeKind.Map:
                WriteDescriptor(type.KeyType!);
                WriteDescriptor(type.ValueType!);
                break;
        }
    }

    private static DataStruct AsStruct(DataValue value, string path)
    {
        return value switch
        {
            DataStruct data => data,
            DataReference reference => reference.Target,
            _ => throw Error(path, $"expected a struct, got {value.Kind}"),
        };
    }

    private void Enter(string path)
    {
        if (++_depth > _maxDepth)
            throw Error(path, $"nesting depth exceeds the limit of {_maxDepth}");
    }

    private static SerializationException Error(string path, string message) => new($"{path}: {message}");
}
