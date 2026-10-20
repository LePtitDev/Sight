using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    private static readonly ClrValueType AnyType = new(typeof(object), ClrShape.Any, TypeReference.Any);

    private readonly BinaryEncoder _encoder;
    private readonly TypeTable _table;
    private readonly ClrModel _model;
    private readonly DependencyTracker _dependencies;
    private readonly Dictionary<object, int> _objectIds = new(ReferenceComparer<object>.Instance);
    private readonly int _maxDepth;
    private int _depth;

    // Path of the value being written, only formatted for error messages: a struct body starts a new path
    private PathSegment[] _path = new PathSegment[16];
    private int _pathCount;

    public TypedWriter(BinaryEncoder encoder, TypeTable table, ClrModel model, DependencyTracker dependencies, int maxDepth)
    {
        _encoder = encoder;
        _table = table;
        _model = model;
        _dependencies = dependencies;
        _maxDepth = maxDepth;
    }

    public BinaryEncoder Encoder => _encoder;

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

    /// <summary>
    /// Write a non-null value through the generic (boxed) path
    /// </summary>
    public void WriteValue(ClrValueType type, object? value)
    {
        if (value == null)
        {
            if (type.Shape == ClrShape.Any)
            {
                _encoder.WriteByte((byte)TypeKind.Null);
                return;
            }

            throw Error("null is not allowed (declare the member as nullable)");
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
                var bytes = GetBytes(value);
                _encoder.WriteVarUInt((ulong)bytes.Length);
                _encoder.WriteBytes(bytes);
                break;
            case ClrShape.List:
                WriteList(type.Element!, (IEnumerable)value);
                break;
            case ClrShape.Map:
                WriteMap(type, (IDictionary)value);
                break;
            case ClrShape.Struct:
                if (schemaType.Kind == TypeKind.Ref)
                    WriteRef(schemaType.StructDefinition!, value);
                else
                    WriteStruct(schemaType.StructDefinition!, value);

                break;
            case ClrShape.ExtRef:
                var reference = (IExternalRef)value;
                _encoder.WriteVarUInt((ulong)_dependencies.GetIndex(reference.DependencyId, reference.Version));
                _encoder.WriteString(reference.Key);
                break;
            case ClrShape.Any:
                WriteAny(value);
                break;
            case ClrShape.Converted:
                WriteValue(type.Surrogate!, type.SurrogateConverter!.ToSurrogate(value));
                break;
        }
    }

    private static ReadOnlySpan<byte> GetBytes(object value) => value switch
    {
        byte[] bytes => bytes,
        ReadOnlyMemory<byte> memory => memory.Span,
        Memory<byte> memory => memory.Span,
        ArraySegment<byte> segment => segment.AsSpan(),
        _ => throw new InvalidCastException($"'{value.GetType()}' is not a byte buffer"),
    };

    public void WriteEnum(EnumDefinition definition, long value)
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

    /// <summary>
    /// Start writing the items of a list (depth check, item path)
    /// </summary>
    public void EnterList()
    {
        Enter();
        PushPath(PathKind.Index, null, 0);
    }

    /// <summary>
    /// Set the index of the list item being written
    /// </summary>
    public void SetListIndex(int index)
    {
        _path[_pathCount - 1] = new PathSegment(PathKind.Index, null, index);
    }

    public void ExitList()
    {
        _pathCount--;
        _depth--;
    }

    private void WriteItem(ClrValueType type, object? value)
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

        WriteValue(type, value);
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

    private void WriteList(ClrValueType element, IEnumerable values)
    {
        EnterList();
        var collection = values as ICollection ?? new List<object?>(Cast(values));
        _encoder.WriteVarUInt((ulong)collection.Count);
        if (element.SchemaType.Kind == TypeKind.Bool && !element.SchemaType.IsNullable)
        {
            var converter = element.Shape == ClrShape.Converted ? element.SurrogateConverter : null;
            byte current = 0;
            var index = 0;
            foreach (var item in collection)
            {
                if ((bool)(converter != null ? converter.ToSurrogate(item!) : item)!)
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
            {
                SetListIndex(index++);
                WriteItem(element, item);
            }
        }

        ExitList();
    }

    private static IEnumerable<object?> Cast(IEnumerable values)
    {
        foreach (var value in values)
            yield return value;
    }

    private void WriteMap(ClrValueType type, IDictionary map)
    {
        Enter();
        _encoder.WriteVarUInt((ulong)map.Count);
        foreach (DictionaryEntry entry in map)
        {
            WriteValue(type.Key!, entry.Key);
            PushPath(PathKind.Key, entry.Key, 0);
            WriteItem(type.Value!, entry.Value);
            _pathCount--;
        }

        _depth--;
    }

    private void WriteStruct(StructDefinition declared, object value)
    {
        var info = _model.GetStruct(value.GetType());
        WriteTag(declared, info.Definition);
        WriteStructBody(info, value);
    }

    private void WriteRef(StructDefinition declared, object value)
    {
        if (_objectIds.TryGetValue(value, out var id))
        {
            _encoder.WriteVarUInt((ulong)id + 1);
            return;
        }

        _encoder.WriteVarUInt(0);
        _objectIds.Add(value, _objectIds.Count);
        WriteStruct(declared, value);
    }

    private void WriteTag(StructDefinition declared, StructDefinition actual)
    {
        var set = _table.GetConcreteSet(declared);
        if (set.Length == 1 && ReferenceEquals(set[0], actual))
            return;

        var index = Array.IndexOf(set, actual);
        if (index < 0)
            throw Error($"a value of type '{actual.QualifiedName}' is not valid for '{declared.QualifiedName}' (is its schema imported by the file schema?)");

        if (set.Length > 1)
            _encoder.WriteVarUInt((ulong)index);
    }

    private void WriteStructBody(ClrTypeInfo info, object value)
    {
        PushPath(PathKind.Root, info.QualifiedName, 0);
        Enter();

        // Presence bitmap, completed once the values are written
        var bitmapPosition = _encoder.Length;
        var bitmapSize = (info.SlotCount + 7) >> 3;
        for (var i = 0; i < bitmapSize; i++)
            _encoder.WriteByte(0);

        PushPath(PathKind.Member, null, 0);
        var members = info.AllMembers;
        var slot = 0;
        for (var i = 0; i < members.Length; i++)
        {
            var member = members[i];
            _path[_pathCount - 1] = new PathSegment(PathKind.Member, member.Name, 0);
            if (member.Field.HasPresenceSlot)
            {
                if (member.Accessor.Write(this, value, omitAbsent: true))
                    _encoder.SetBits(bitmapPosition + (slot >> 3), (byte)(1 << (slot & 7)));

                slot++;
            }
            else
            {
                member.Accessor.Write(this, value, omitAbsent: false);
            }
        }

        _pathCount -= 2;
        _depth--;
    }

    private void WriteAny(object value)
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
                throw Error("data values cannot be stored in typed objects");
            case byte[] or ReadOnlyMemory<byte> or Memory<byte> or ArraySegment<byte>:
                var bytes = GetBytes(value);
                _encoder.WriteByte((byte)TypeKind.List);
                _encoder.WriteByte((byte)TypeKind.U8);
                _encoder.WriteVarUInt((ulong)bytes.Length);
                _encoder.WriteBytes(bytes);
                return;
            case IDictionary map:
                _encoder.WriteByte((byte)TypeKind.Map);
                _encoder.WriteByte((byte)TypeKind.String);
                _encoder.WriteByte((byte)TypeKind.Any);
                Enter();
                _encoder.WriteVarUInt((ulong)map.Count);
                foreach (DictionaryEntry entry in map)
                {
                    _encoder.WriteString(entry.Key as string ?? throw Error("maps stored in 'any' must have string keys"));
                    PushPath(PathKind.Key, entry.Key, 0);
                    WriteValue(AnyType, entry.Value);
                    _pathCount--;
                }

                _depth--;
                return;
            case string:
                break;
            case IEnumerable list:
                _encoder.WriteByte((byte)TypeKind.List);
                _encoder.WriteByte((byte)TypeKind.Any);
                WriteList(AnyType, list);
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
                WriteRef(structInfo.Definition, value);
                return;
            }

            _encoder.WriteByte((byte)TypeKind.Struct);
            _encoder.WriteVarUInt((ulong)_table.IndexOf(structInfo.Definition));
            WriteStructBody(structInfo, value);
            return;
        }

        // Converted types are stored as their surrogate (read back as the surrogate)
        var converter = ClrModelBuilder.FindConverter(type);
        if (converter != null)
        {
            WriteAny(converter.ToSurrogate(value) ?? throw Error($"converter of '{type}' returned null"));
            return;
        }

        throw Error($"a value of type '{type}' cannot be stored in 'any' (it is not part of the serialization model)");
    }

    private void Enter()
    {
        if (++_depth > _maxDepth)
            throw Error($"nesting depth exceeds the limit of {_maxDepth}");
    }

    private void PushPath(PathKind kind, object? value, int index)
    {
        if (_pathCount == _path.Length)
            Array.Resize(ref _path, _path.Length * 2);

        _path[_pathCount++] = new PathSegment(kind, value, index);
    }

    /// <summary>
    /// Path of the value being written: type of the innermost struct, then members, list indexes and map keys
    /// </summary>
    private string FormatPath()
    {
        var start = _pathCount - 1;
        while (start > 0 && _path[start].Kind != PathKind.Root)
            start--;

        var builder = new StringBuilder();
        for (var i = Math.Max(start, 0); i < _pathCount; i++)
        {
            var segment = _path[i];
            switch (segment.Kind)
            {
                case PathKind.Root:
                    builder.Append(segment.Value);
                    break;
                case PathKind.Member:
                    builder.Append('.').Append(segment.Value);
                    break;
                case PathKind.Index:
                    builder.Append('[').Append(segment.Index).Append(']');
                    break;
                case PathKind.Key:
                    builder.Append('[').Append(segment.Value).Append(']');
                    break;
            }
        }

        return builder.ToString();
    }

    private SerializationException Error(string message) => new($"{FormatPath()}: {message}");

    private enum PathKind
    {
        Root,
        Member,
        Index,
        Key,
    }

    private readonly struct PathSegment
    {
        public PathSegment(PathKind kind, object? value, int index)
        {
            Kind = kind;
            Value = value;
            Index = index;
        }

        public PathKind Kind { get; }

        public object? Value { get; }

        public int Index { get; }
    }
}
