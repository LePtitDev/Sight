using System;
using System.Collections;
using System.Collections.Generic;
using Sight.Serialization.Binary;
using Sight.Serialization.Data;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Typed;

/// <summary>
/// Decodes data written with a writer schema into CLR objects (binary specification §9)
/// </summary>
internal sealed class TypedReader
{
    private static readonly ClrValueType AnyType = new(typeof(object), ClrShape.Any, TypeReference.Any);

    private readonly BinaryDecoder _decoder;
    private readonly TypeTable _table;
    private readonly ClrModel _model;
    private readonly IReadOnlyList<DependencyReference> _dependencies;
    private readonly DependencyContext _context;
    private readonly SerializationLimits _limits;
    private readonly List<object?> _objects = new();
    private readonly Dictionary<(StructDefinition, ClrTypeInfo), StructPlan> _plans = new();
    private int _depth;

    public TypedReader(BinaryDecoder decoder, TypeTable table, ClrModel model, IReadOnlyList<DependencyReference> dependencies, DependencyContext context, SerializationLimits limits)
    {
        _decoder = decoder;
        _table = table;
        _model = model;
        _dependencies = dependencies;
        _context = context;
        _limits = limits;
    }

    /// <summary>
    /// Read a struct of an exact writer type (root, exports) into its mapped CLR type
    /// </summary>
    public object ReadExactStruct(StructDefinition writerType, Type expected)
    {
        var info = MapStruct(writerType, expected);
        var value = info.Create!();
        ReadStructBody(writerType, info, value);
        return value;
    }

    /// <summary>
    /// Skip a struct of an exact writer type
    /// </summary>
    public void SkipExactStruct(StructDefinition writerType) => SkipStructBody(writerType);

    /// <summary>
    /// Read an export (<c>ref&lt;T&gt;</c> with T its exact type); returns null if its type is not mapped
    /// </summary>
    public object? ReadExport(StructDefinition writerType)
    {
        var x = _decoder.ReadVarUInt();
        if (x > 0)
            return GetObject(x - 1);

        if (!_model.StructsByIdentity.TryGetValue(writerType.QualifiedName, out var info) || info.Create == null)
        {
            AddObject(null);
            SkipStructBody(writerType);
            return null;
        }

        var value = info.Create();
        AddObject(value);
        ReadStructBody(writerType, info, value);
        return value;
    }

    private object? ReadItem(TypeReference type, ClrValueType target)
    {
        if (type.IsNullable)
        {
            var presence = _decoder.ReadByte();
            if (presence == 0)
                return Null(target);

            if (presence != 1)
                throw Error($"invalid presence byte 0x{presence:X2}");
        }

        return ReadValue(type, target);
    }

    private object? Null(ClrValueType target)
    {
        if (target.Type.IsValueType && Nullable.GetUnderlyingType(target.Type) == null)
            throw Error($"null cannot be assigned to '{target.Type}'");

        return null;
    }

    private object? ReadValue(TypeReference type, ClrValueType target)
    {
        switch (type.Kind)
        {
            case TypeKind.Bool:
                CheckScalar(type.Kind, target);
                return ClrConvert.ScalarToClr(_decoder.ReadBoolean(), target);
            case TypeKind.U8:
            case TypeKind.U16:
            case TypeKind.U32:
            case TypeKind.U64:
                CheckScalar(type.Kind, target);
                return ClrConvert.ScalarToClr(BoxUnsigned(type.Kind, ReadUnsigned(type.Kind, type.IsFixed), target), target);
            case TypeKind.S8:
            case TypeKind.S16:
            case TypeKind.S32:
            case TypeKind.S64:
                CheckScalar(type.Kind, target);
                return ClrConvert.ScalarToClr(BoxSigned(type.Kind, ReadSigned(type.Kind, type.IsFixed), target), target);
            case TypeKind.F32:
                CheckScalar(type.Kind, target);
                return ClrConvert.ScalarToClr(_decoder.ReadSingle(), target);
            case TypeKind.F64:
                CheckScalar(type.Kind, target);
                return ClrConvert.ScalarToClr(_decoder.ReadDouble(), target);
            case TypeKind.String:
                CheckScalar(type.Kind, target);
                return _decoder.ReadString();
            case TypeKind.Enum:
                return ReadEnum(type.EnumDefinition!, target);
            case TypeKind.List:
                return ReadList(type.ElementType!, target);
            case TypeKind.Map:
                return ReadMap(type, target);
            case TypeKind.Struct:
                return ReadStruct(type.StructDefinition!, target);
            case TypeKind.Ref:
                return ReadRef(type.StructDefinition!, target);
            case TypeKind.ExtRef:
                return ReadExtRef(type.StructDefinition!, target);
            case TypeKind.Any:
                if (target.Shape != ClrShape.Any)
                    throw Error($"a value of type 'any' cannot be read into '{target.Type}'");

                return ReadAny();
            default:
                throw Error($"type {type} cannot be read");
        }
    }

    private static object BoxUnsigned(TypeKind kind, ulong value, ClrValueType target)
    {
        if (target.Shape != ClrShape.Any)
            return value;

        return kind switch
        {
            TypeKind.U8 => (byte)value,
            TypeKind.U16 => (ushort)value,
            TypeKind.U32 => (uint)value,
            _ => value,
        };
    }

    private static object BoxSigned(TypeKind kind, long value, ClrValueType target)
    {
        if (target.Shape != ClrShape.Any)
            return value;

        return kind switch
        {
            TypeKind.S8 => (sbyte)value,
            TypeKind.S16 => (short)value,
            TypeKind.S32 => (int)value,
            _ => value,
        };
    }

    private void CheckScalar(TypeKind writer, ClrValueType target)
    {
        if (target.Shape == ClrShape.Any)
            return;

        if (target.Shape != ClrShape.Scalar)
            throw Error($"a value of type {writer.ToKeyword()} cannot be read into '{target.Type}'");

        var reader = target.SchemaType.Kind;
        if (!IsWidening(writer, reader))
            throw Error($"type {writer.ToKeyword()} cannot be converted to {reader.ToKeyword()} ('{target.Type}')");
    }

    private static bool IsWidening(TypeKind writer, TypeKind reader)
    {
        if (writer == reader)
            return true;

        if (writer.IsUnsignedInteger() && reader.IsUnsignedInteger())
            return reader.IntegerBits() >= writer.IntegerBits();

        if (writer.IsSignedInteger() && reader.IsSignedInteger())
            return reader.IntegerBits() >= writer.IntegerBits();

        if (writer.IsUnsignedInteger() && reader.IsSignedInteger())
            return reader.IntegerBits() > writer.IntegerBits();

        return writer == TypeKind.F32 && reader == TypeKind.F64;
    }

    private ulong ReadUnsigned(TypeKind kind, bool isFixed)
    {
        ulong value = kind switch
        {
            TypeKind.U8 => _decoder.ReadByte(),
            TypeKind.U16 when isFixed => _decoder.ReadFixedUInt16(),
            TypeKind.U32 when isFixed => _decoder.ReadFixedUInt32(),
            TypeKind.U64 when isFixed => _decoder.ReadFixedUInt64(),
            _ => _decoder.ReadVarUInt(),
        };

        if (!DataRanges.Fits(kind, value))
            throw Error($"value {value} is out of range for {kind.ToKeyword()}");

        return value;
    }

    private long ReadSigned(TypeKind kind, bool isFixed)
    {
        long value = kind switch
        {
            TypeKind.S8 => (sbyte)_decoder.ReadByte(),
            TypeKind.S16 when isFixed => (short)_decoder.ReadFixedUInt16(),
            TypeKind.S32 when isFixed => (int)_decoder.ReadFixedUInt32(),
            TypeKind.S64 when isFixed => (long)_decoder.ReadFixedUInt64(),
            _ => _decoder.ReadVarInt(),
        };

        if (!DataRanges.Fits(kind, value))
            throw Error($"value {value} is out of range for {kind.ToKeyword()}");

        return value;
    }

    private long ReadEnumValue(EnumDefinition definition)
    {
        return definition.Underlying.IsSignedInteger()
            ? ReadSigned(definition.Underlying, false)
            : unchecked((long)ReadUnsigned(definition.Underlying, false));
    }

    private object ReadEnum(EnumDefinition definition, ClrValueType target)
    {
        var value = ReadEnumValue(definition);
        if (target.Shape == ClrShape.Any)
        {
            var name = definition.FindValue(value)?.Name ?? throw Error($"value {value} is not declared by enum '{definition.QualifiedName}'");
            if (_model.EnumsByIdentity.TryGetValue(definition.QualifiedName, out var info) && info.ValuesByName.TryGetValue(name, out var clr))
                return clr;
        }

        try
        {
            return ClrConvert.EnumToClr(definition, value, target);
        }
        catch (SerializationException e)
        {
            throw Error(e.Message);
        }
    }

    private object ReadList(TypeReference elementType, ClrValueType target)
    {
        Enter();
        var count = ReadCount();
        object result;
        if (target.Shape == ClrShape.ByteArray || (target.Shape == ClrShape.Any && elementType.Kind == TypeKind.U8 && !elementType.IsNullable))
        {
            if (elementType.Kind != TypeKind.U8 || elementType.IsNullable)
                throw Error($"list<{elementType}> cannot be read into a byte array");

            var bytes = new byte[count];
            _decoder.ReadBytes(bytes);
            result = bytes;
        }
        else if (target.Shape is ClrShape.List or ClrShape.Any)
        {
            var element = target.Element ?? AnyType;
            var list = target.CreateCollection?.Invoke(Math.Min(count, 1024)) ?? new List<object?>(Math.Min(count, 1024));
            var add = target.AddItem ?? ((collection, item) => ((List<object?>)collection).Add(item));
            if (elementType.Kind == TypeKind.Bool && !elementType.IsNullable)
            {
                CheckScalar(TypeKind.Bool, element);
                byte current = 0;
                for (var i = 0; i < count; i++)
                {
                    if ((i & 7) == 0)
                        current = _decoder.ReadByte();

                    add(list, (current & (1 << (i & 7))) != 0);
                }

                if ((count & 7) != 0 && (current >> (count & 7)) != 0)
                    throw Error("non-zero padding bits in packed booleans");
            }
            else
            {
                for (var i = 0; i < count; i++)
                    add(list, ReadItem(elementType, element));
            }

            result = target.Complete != null ? target.Complete(list) : list;
        }
        else
        {
            throw Error($"a list cannot be read into '{target.Type}'");
        }

        _depth--;
        return result;
    }

    private object ReadMap(TypeReference type, ClrValueType target)
    {
        if (target.Shape is not ClrShape.Map and not ClrShape.Any)
            throw Error($"a map cannot be read into '{target.Type}'");

        Enter();
        var count = ReadCount();
        var map = (IDictionary)(target.CreateCollection?.Invoke(Math.Min(count, 1024)) ?? new Dictionary<object, object?>());
        var keyTarget = target.Key ?? AnyType;
        var valueTarget = target.Value ?? AnyType;
        for (var i = 0; i < count; i++)
        {
            var key = ReadValue(type.KeyType!, keyTarget)!;
            if (map.Contains(key))
                throw Error($"duplicate map key '{key}'");

            map.Add(key, ReadItem(type.ValueType!, valueTarget));
        }

        _depth--;
        return map;
    }

    private object ReadStruct(StructDefinition declared, ClrValueType target)
    {
        var writerType = ReadConcreteType(declared);
        var info = MapStruct(writerType, target.Shape == ClrShape.Any ? typeof(object) : StructType(target));
        var value = info.Create!();
        ReadStructBody(writerType, info, value);
        return value;
    }

    private object? ReadRef(StructDefinition declared, ClrValueType target)
    {
        var x = _decoder.ReadVarUInt();
        if (x > 0)
        {
            var existing = GetObject(x - 1) ?? throw Error("reference to an object that was not read (removed field)");
            var expected = target.Shape == ClrShape.Any ? typeof(object) : StructType(target);
            if (!expected.IsInstanceOfType(existing))
                throw Error($"referenced object of type '{existing.GetType()}' cannot be assigned to '{expected}'");

            return existing;
        }

        var writerType = ReadConcreteType(declared);
        var info = MapStruct(writerType, target.Shape == ClrShape.Any ? typeof(object) : StructType(target));
        var value = info.Create!();
        AddObject(value);
        ReadStructBody(writerType, info, value);
        return value;
    }

    private object ReadExtRef(StructDefinition declared, ClrValueType target)
    {
        var index = _decoder.ReadVarUInt();
        if (index >= (ulong)_dependencies.Count)
            throw Error($"invalid dependency index {index}");

        var key = _decoder.ReadString();
        var dependency = _dependencies[(int)index];
        if (target.Shape != ClrShape.ExtRef)
        {
            if (target.Shape == ClrShape.Any)
                return new DataExtRef(dependency.Id, key, declared);

            throw Error($"an external reference cannot be read into '{target.Type}'");
        }

        return ExternalRefFactory.Create(target.ExtRefTarget!, dependency.Id, key, dependency.Version, _context);
    }

    private object? ReadAny()
    {
        var type = ReadDescriptor(isTopLevel: true);
        if (type.Kind == TypeKind.Null)
            return null;

        if (type.Kind == TypeKind.Struct)
            return ReadExactStruct(type.StructDefinition!, typeof(object));

        return ReadValue(type, AnyType);
    }

    private TypeReference ReadDescriptor(bool isTopLevel)
    {
        var header = _decoder.ReadByte();
        var kind = (TypeKind)(header & 0x1F);
        var isNullable = (header & 0x20) != 0;
        var isFixed = (header & 0x40) != 0;
        if ((header & 0x80) != 0 || kind > TypeKind.Any || (isNullable && (isTopLevel || kind is TypeKind.Any or TypeKind.Null)) || (isFixed && !kind.AllowsFixed()))
            throw Error($"invalid type descriptor 0x{header:X2}");

        TypeReference type = kind switch
        {
            TypeKind.Null => TypeReference.Null,
            TypeKind.Enum => TypeReference.Of(_table.Get(_decoder.ReadVarUInt()) as EnumDefinition ?? throw Error("type descriptor index is not an enum")),
            TypeKind.Struct => TypeReference.Of(_table.GetStruct(_decoder.ReadVarUInt())),
            TypeKind.Ref => TypeReference.Ref(_table.GetStruct(_decoder.ReadVarUInt())),
            TypeKind.ExtRef => TypeReference.ExtRef(_table.GetStruct(_decoder.ReadVarUInt())),
            TypeKind.List => TypeReference.List(ReadDescriptor(isTopLevel: false)),
            TypeKind.Map => TypeReference.Map(ReadDescriptor(isTopLevel: false), ReadDescriptor(isTopLevel: false)),
            _ => TypeReference.Primitive(kind),
        };

        return kind == TypeKind.Null ? type : type.WithFlags(isNullable, isFixed);
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

    private static Type StructType(ClrValueType target)
    {
        if (target.Shape != ClrShape.Struct)
            throw new SerializationException($"A struct cannot be read into '{target.Type}'");

        return target.Struct!.Type;
    }

    private ClrTypeInfo MapStruct(StructDefinition writerType, Type expected)
    {
        if (!_model.StructsByIdentity.TryGetValue(writerType.QualifiedName, out var info))
            throw Error($"type '{writerType.QualifiedName}' has no CLR type (declare it with [SerializationDerivedType] on its base type)");

        if (!expected.IsAssignableFrom(info.Type))
            throw Error($"type '{writerType.QualifiedName}' ('{info.Type}') cannot be assigned to '{expected}'");

        if (info.Create == null)
            throw Error($"cannot instantiate abstract type '{info.Type}'");

        return info;
    }

    private void ReadStructBody(StructDefinition writerType, ClrTypeInfo info, object value)
    {
        Enter();
        var plan = GetPlan(writerType, info);
        var fields = writerType.AllFields;
        Span<byte> bitmap = plan.BitmapSize <= 64 ? stackalloc byte[plan.BitmapSize] : new byte[plan.BitmapSize];
        _decoder.ReadBytes(bitmap);
        if ((plan.SlotCount & 7) != 0 && (bitmap[plan.BitmapSize - 1] >> (plan.SlotCount & 7)) != 0)
            throw Error($"non-zero padding bits in the presence bitmap of '{writerType.QualifiedName}'");

        var slot = 0;
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var member = plan.Members[i];
            var present = true;
            if (field.HasPresenceSlot)
            {
                present = (bitmap[slot >> 3] & (1 << (slot & 7))) != 0;
                slot++;
            }

            if (member == null)
            {
                if (present)
                    Skip(field.Type);

                continue;
            }

            object? memberValue;
            if (present)
                memberValue = ReadValue(field.Type, member.ValueType);
            else if (field.DefaultValue != null)
                memberValue = plan.Defaults[i];
            else
                memberValue = Null(member.ValueType);

            member.Setter(value, memberValue);
        }

        foreach (var (member, defaultValue) in plan.Missing)
            member.Setter(value, defaultValue);

        _depth--;
    }

    private StructPlan GetPlan(StructDefinition writerType, ClrTypeInfo info)
    {
        if (_plans.TryGetValue((writerType, info), out var plan))
            return plan;

        var fields = writerType.AllFields;
        plan = new StructPlan(fields.Count);
        var matched = new HashSet<ClrMember>();
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            if (field.HasPresenceSlot)
                plan.SlotCount++;

            foreach (var member in info.AllMembers)
            {
                if (string.Equals(member.Name, field.Name, StringComparison.Ordinal) || Array.IndexOf(member.Aliases, field.Name) >= 0)
                {
                    plan.Members[i] = member;
                    matched.Add(member);
                    if (field.DefaultValue != null)
                    {
                        try
                        {
                            plan.Defaults[i] = ClrConvert.FromData(field.DefaultValue, member.ValueType);
                        }
                        catch (SerializationException e)
                        {
                            throw Error($"default value of '{writerType.QualifiedName}.{field.Name}': {e.Message}");
                        }
                    }

                    break;
                }
            }
        }

        foreach (var member in info.AllMembers)
        {
            if (matched.Contains(member))
                continue;

            if (member.ValueType.SchemaType.IsNullable)
                plan.Missing.Add((member, null));
            else if (member.DefaultValue != null)
                plan.Missing.Add((member, member.DefaultClrValue));
            else
                throw Error($"field '{member.Name}' of '{info.QualifiedName}' is missing in the data and has no default value");
        }

        plan.BitmapSize = (plan.SlotCount + 7) / 8;
        _plans.Add((writerType, info), plan);
        return plan;
    }

    private void Skip(TypeReference type)
    {
        switch (type.Kind)
        {
            case TypeKind.Bool:
            case TypeKind.U8:
            case TypeKind.S8:
                _decoder.ReadByte();
                break;
            case TypeKind.U16:
            case TypeKind.U32:
            case TypeKind.U64:
            case TypeKind.S16:
            case TypeKind.S32:
            case TypeKind.S64:
                if (type.IsFixed)
                    _decoder.Skip(type.Kind.IntegerBits() / 8);
                else
                    _decoder.ReadVarUInt();

                break;
            case TypeKind.F32:
                _decoder.Skip(4);
                break;
            case TypeKind.F64:
                _decoder.Skip(8);
                break;
            case TypeKind.String:
                _decoder.ReadString();
                break;
            case TypeKind.Enum:
                ReadEnumValue(type.EnumDefinition!);
                break;
            case TypeKind.List:
                Enter();
                var count = ReadCount();
                var element = type.ElementType!;
                if (element.Kind == TypeKind.Bool && !element.IsNullable)
                {
                    _decoder.Skip((count + 7) / 8);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                        SkipItem(element);
                }

                _depth--;
                break;
            case TypeKind.Map:
                Enter();
                var entries = ReadCount();
                for (var i = 0; i < entries; i++)
                {
                    Skip(type.KeyType!);
                    SkipItem(type.ValueType!);
                }

                _depth--;
                break;
            case TypeKind.Struct:
                SkipStructBody(ReadConcreteType(type.StructDefinition!));
                break;
            case TypeKind.Ref:
                if (_decoder.ReadVarUInt() == 0)
                {
                    var concrete = ReadConcreteType(type.StructDefinition!);
                    AddObject(null);
                    SkipStructBody(concrete);
                }

                break;
            case TypeKind.ExtRef:
                _decoder.ReadVarUInt();
                _decoder.ReadString();
                break;
            case TypeKind.Any:
                var descriptor = ReadDescriptor(isTopLevel: true);
                if (descriptor.Kind == TypeKind.Struct)
                    SkipStructBody(descriptor.StructDefinition!);
                else if (descriptor.Kind != TypeKind.Null)
                    Skip(descriptor);

                break;
        }
    }

    private void SkipItem(TypeReference type)
    {
        if (type.IsNullable && _decoder.ReadByte() == 0)
            return;

        Skip(type);
    }

    private void SkipStructBody(StructDefinition type)
    {
        Enter();
        var fields = type.AllFields;
        var slotCount = 0;
        foreach (var field in fields)
        {
            if (field.HasPresenceSlot)
                slotCount++;
        }

        var bitmap = new byte[(slotCount + 7) / 8];
        _decoder.ReadBytes(bitmap);
        var slot = 0;
        foreach (var field in fields)
        {
            var present = true;
            if (field.HasPresenceSlot)
            {
                present = (bitmap[slot >> 3] & (1 << (slot & 7))) != 0;
                slot++;
            }

            if (present)
                Skip(field.Type);
        }

        _depth--;
    }

    private int ReadCount()
    {
        var count = _decoder.ReadVarUInt();
        if (count > (ulong)_limits.MaxCollectionCount)
            throw Error($"collection count {count} exceeds the limit of {_limits.MaxCollectionCount}");

        return (int)count;
    }

    private object? GetObject(ulong id)
    {
        return id < (ulong)_objects.Count ? _objects[(int)id] : throw Error($"reference to unknown object id {id}");
    }

    private void AddObject(object? value)
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

    private SerializationException Error(string message) => new($"Invalid data at position {_decoder.Position}: {message}");

    private sealed class StructPlan
    {
        public StructPlan(int fieldCount)
        {
            Members = new ClrMember?[fieldCount];
            Defaults = new object?[fieldCount];
        }

        public ClrMember?[] Members { get; }

        public object?[] Defaults { get; }

        public List<(ClrMember Member, object? Default)> Missing { get; } = new();

        public int SlotCount { get; set; }

        public int BitmapSize { get; set; }
    }
}
