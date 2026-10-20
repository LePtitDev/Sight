using System;
using System.Collections;
using System.Collections.Generic;
using Sight.Serialization.Binary;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Typed;

/// <summary>
/// Internal access to the content of an <see cref="ExternalRef{T}"/>
/// </summary>
internal interface IExternalRef
{
    public string DependencyId { get; }

    public string Key { get; }

    public uint? Version { get; }
}

/// <summary>
/// Encodes CLR objects according to their model
/// </summary>
internal sealed class TypedWriter
{
    private readonly BinaryEncoder _encoder;
    private readonly TypeTable _table;
    private readonly ClrModel _model;
    private readonly DependencyTracker _dependencies;
    private readonly Dictionary<object, int> _objectIds = new(ReferenceComparer<object>.Instance);
    private readonly int _maxDepth;
    private int _depth;

    public TypedWriter(BinaryEncoder encoder, TypeTable table, ClrModel model, DependencyTracker dependencies, int maxDepth)
    {
        _encoder = encoder;
        _table = table;
        _model = model;
        _dependencies = dependencies;
        _maxDepth = maxDepth;
    }

    public void WriteExactStruct(ClrTypeInfo info, object value)
    {
        WriteStructBody(info, value);
    }

    public void WriteExport(ClrTypeInfo info, object value)
    {
        if (_objectIds.TryGetValue(value, out var id))
        {
            _encoder.WriteVarUInt((ulong)id + 1);
            return;
        }

        _encoder.WriteVarUInt(0);
        _objectIds.Add(value, _objectIds.Count);
        WriteStructBody(info, value);
    }

    private void WriteItem(ClrValueType type, object? value, string path)
    {
        if (type.SchemaType.IsNullable)
        {
            if (value == null)
            {
                _encoder.WriteByte(0);
                return;
            }

            _encoder.WriteByte(1);
        }

        WriteValue(type, value, path);
    }

    private void WriteValue(ClrValueType type, object? value, string path)
    {
        if (value == null)
        {
            if (type.Shape == ClrShape.Any)
            {
                _encoder.WriteByte((byte)TypeKind.Null);
                return;
            }

            throw Error(path, "null is not allowed (declare the member as nullable)");
        }

        var schemaType = type.SchemaType;
        switch (type.Shape)
        {
            case ClrShape.Scalar:
                WriteScalar(schemaType, value);
                break;
            case ClrShape.Enum:
                WriteEnum(type.Enum!.Definition, type.Enum.ToSchemaValue(value));
                break;
            case ClrShape.ByteArray:
                var bytes = (byte[])value;
                _encoder.WriteVarUInt((ulong)bytes.Length);
                _encoder.WriteBytes(bytes);
                break;
            case ClrShape.List:
                WriteList(type.Element!, (IEnumerable)value, path);
                break;
            case ClrShape.Map:
                WriteMap(type, (IDictionary)value, path);
                break;
            case ClrShape.Struct:
                if (schemaType.Kind == TypeKind.Ref)
                    WriteRef(schemaType.StructDefinition!, value, path);
                else
                    WriteStruct(schemaType.StructDefinition!, value, path);

                break;
            case ClrShape.ExtRef:
                var reference = (IExternalRef)value;
                _encoder.WriteVarUInt((ulong)_dependencies.GetIndex(reference.DependencyId, reference.Version));
                _encoder.WriteString(reference.Key);
                break;
            case ClrShape.Any:
                WriteAny(value, path);
                break;
        }
    }

    private void WriteScalar(TypeReference type, object value)
    {
        switch (type.Kind)
        {
            case TypeKind.Bool:
                _encoder.WriteBoolean((bool)value);
                break;
            case TypeKind.U8:
                _encoder.WriteByte((byte)value);
                break;
            case TypeKind.S8:
                _encoder.WriteByte((byte)(sbyte)value);
                break;
            case TypeKind.U16:
                if (type.IsFixed)
                    _encoder.WriteFixedUInt16((ushort)value);
                else
                    _encoder.WriteVarUInt((ushort)value);

                break;
            case TypeKind.U32:
                if (type.IsFixed)
                    _encoder.WriteFixedUInt32((uint)value);
                else
                    _encoder.WriteVarUInt((uint)value);

                break;
            case TypeKind.U64:
                if (type.IsFixed)
                    _encoder.WriteFixedUInt64((ulong)value);
                else
                    _encoder.WriteVarUInt((ulong)value);

                break;
            case TypeKind.S16:
                if (type.IsFixed)
                    _encoder.WriteFixedUInt16((ushort)(short)value);
                else
                    _encoder.WriteVarInt((short)value);

                break;
            case TypeKind.S32:
                if (type.IsFixed)
                    _encoder.WriteFixedUInt32((uint)(int)value);
                else
                    _encoder.WriteVarInt((int)value);

                break;
            case TypeKind.S64:
                if (type.IsFixed)
                    _encoder.WriteFixedUInt64((ulong)(long)value);
                else
                    _encoder.WriteVarInt((long)value);

                break;
            case TypeKind.F32:
                _encoder.WriteSingle((float)value);
                break;
            case TypeKind.F64:
                _encoder.WriteDouble((double)value);
                break;
            case TypeKind.String:
                _encoder.WriteString((string)value);
                break;
        }
    }

    private void WriteEnum(EnumDefinition definition, long value)
    {
        switch (definition.Underlying)
        {
            case TypeKind.U8:
            case TypeKind.S8:
                _encoder.WriteByte((byte)value);
                break;
            case TypeKind.S16:
            case TypeKind.S32:
            case TypeKind.S64:
                _encoder.WriteVarInt(value);
                break;
            default:
                _encoder.WriteVarUInt((ulong)value);
                break;
        }
    }

    private void WriteList(ClrValueType element, IEnumerable values, string path)
    {
        Enter(path);
        var collection = values as ICollection ?? new List<object?>(Cast(values));
        _encoder.WriteVarUInt((ulong)collection.Count);
        if (element.SchemaType.Kind == TypeKind.Bool && !element.SchemaType.IsNullable)
        {
            byte current = 0;
            var index = 0;
            foreach (var item in collection)
            {
                if ((bool)item!)
                    current |= (byte)(1 << (index & 7));

                if ((index & 7) == 7)
                {
                    _encoder.WriteByte(current);
                    current = 0;
                }

                index++;
            }

            if ((index & 7) != 0)
                _encoder.WriteByte(current);
        }
        else
        {
            var index = 0;
            foreach (var item in collection)
                WriteItem(element, item, path + "[" + index++ + "]");
        }

        _depth--;
    }

    private static IEnumerable<object?> Cast(IEnumerable values)
    {
        foreach (var value in values)
            yield return value;
    }

    private void WriteMap(ClrValueType type, IDictionary map, string path)
    {
        Enter(path);
        _encoder.WriteVarUInt((ulong)map.Count);
        foreach (DictionaryEntry entry in map)
        {
            WriteValue(type.Key!, entry.Key, path);
            WriteItem(type.Value!, entry.Value, path + "[" + entry.Key + "]");
        }

        _depth--;
    }

    private void WriteStruct(StructDefinition declared, object value, string path)
    {
        var info = _model.GetStruct(value.GetType());
        WriteTag(declared, info.Definition, path);
        WriteStructBody(info, value);
    }

    private void WriteRef(StructDefinition declared, object value, string path)
    {
        if (_objectIds.TryGetValue(value, out var id))
        {
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
            throw Error(path, $"a value of type '{actual.QualifiedName}' is not valid for '{declared.QualifiedName}' (is its schema imported by the file schema?)");

        if (set.Length > 1)
            _encoder.WriteVarUInt((ulong)index);
    }

    private void WriteStructBody(ClrTypeInfo info, object value)
    {
        var path = info.QualifiedName;
        Enter(path);
        var members = info.AllMembers;
        var values = new object?[members.Length];
        var slotCount = 0;
        byte current = 0;
        for (var i = 0; i < members.Length; i++)
        {
            var member = members[i];
            var memberValue = member.Getter(value);
            values[i] = memberValue;
            if (!member.Field.HasPresenceSlot)
                continue;

            var present = memberValue != null && (member.DefaultValue == null || !ClrConvert.AreEqual(memberValue, member.DefaultClrValue));
            if (present)
                current |= (byte)(1 << (slotCount & 7));

            if ((slotCount & 7) == 7)
            {
                _encoder.WriteByte(current);
                current = 0;
            }

            slotCount++;
        }

        if ((slotCount & 7) != 0)
            _encoder.WriteByte(current);

        for (var i = 0; i < members.Length; i++)
        {
            var member = members[i];
            var memberValue = values[i];
            if (member.Field.HasPresenceSlot)
            {
                if (memberValue == null || (member.DefaultValue != null && ClrConvert.AreEqual(memberValue, member.DefaultClrValue)))
                    continue;
            }

            WriteValue(member.ValueType, memberValue, path + "." + member.Name);
        }

        _depth--;
    }

    private void WriteAny(object value, string path)
    {
        var type = value.GetType();
        var kind = ClrModelBuilder.ScalarKind(type);
        if (kind.HasValue)
        {
            var schemaType = TypeReference.Primitive(kind.Value);
            _encoder.WriteByte((byte)kind.Value);
            WriteScalar(schemaType, value);
            return;
        }

        switch (value)
        {
            case DataValue:
                throw Error(path, "data values cannot be stored in typed objects");
            case byte[] bytes:
                _encoder.WriteByte((byte)TypeKind.List);
                _encoder.WriteByte((byte)TypeKind.U8);
                _encoder.WriteVarUInt((ulong)bytes.Length);
                _encoder.WriteBytes(bytes);
                return;
            case IDictionary map:
                _encoder.WriteByte((byte)TypeKind.Map);
                _encoder.WriteByte((byte)TypeKind.String);
                _encoder.WriteByte((byte)TypeKind.Any);
                Enter(path);
                _encoder.WriteVarUInt((ulong)map.Count);
                foreach (DictionaryEntry entry in map)
                {
                    _encoder.WriteString(entry.Key as string ?? throw Error(path, "maps stored in 'any' must have string keys"));
                    WriteValue(AnyType, entry.Value, path + "[" + entry.Key + "]");
                }

                _depth--;
                return;
            case string:
                break;
            case IEnumerable list:
                _encoder.WriteByte((byte)TypeKind.List);
                _encoder.WriteByte((byte)TypeKind.Any);
                WriteList(AnyType, list, path);
                return;
        }

        if (type.IsEnum && _model.Enums.TryGetValue(type, out var enumInfo))
        {
            _encoder.WriteByte((byte)TypeKind.Enum);
            _encoder.WriteVarUInt((ulong)_table.IndexOf(enumInfo.Definition));
            WriteEnum(enumInfo.Definition, enumInfo.ToSchemaValue(value));
            return;
        }

        if (_model.Structs.TryGetValue(type, out var structInfo))
        {
            if (structInfo.IsReference)
            {
                _encoder.WriteByte((byte)TypeKind.Ref);
                _encoder.WriteVarUInt((ulong)_table.IndexOf(structInfo.Definition));
                WriteRef(structInfo.Definition, value, path);
                return;
            }

            _encoder.WriteByte((byte)TypeKind.Struct);
            _encoder.WriteVarUInt((ulong)_table.IndexOf(structInfo.Definition));
            WriteStructBody(structInfo, value);
            return;
        }

        throw Error(path, $"a value of type '{type}' cannot be stored in 'any' (it is not part of the serialization model)");
    }

    private static readonly ClrValueType AnyType = new(typeof(object), ClrShape.Any, TypeReference.Any);

    private void Enter(string path)
    {
        if (++_depth > _maxDepth)
            throw Error(path, $"nesting depth exceeds the limit of {_maxDepth}");
    }

    private static SerializationException Error(string path, string message) => new($"{path}: {message}");
}
