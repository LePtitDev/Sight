using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Typed;

/// <summary>
/// Builds a <see cref="ClrModel"/> (schemas and mappings) from CLR types
/// </summary>
internal sealed class ClrModelBuilder
{
    private static readonly ConcurrentDictionary<string, ClrModel> s_cache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, ClrModel> s_single = new();
    private static readonly ConcurrentDictionary<(Type, Type), ClrModel> s_pairs = new();
    private static readonly ConcurrentDictionary<Type, ISurrogateConverter?> s_typeConverters = new();
    private static readonly Lazy<Dictionary<Type, SerializationConverterAttribute>> s_assemblyConverters = new(FindAssemblyConverters);

    private readonly ClrModel _model = new();
    private readonly Queue<Type> _pending = new();
    private readonly Dictionary<MemberInfo, ISurrogateConverter?> _memberConverters = new();
    private readonly Dictionary<Type, (string Schema, string Name)> _aliasIdentities = new();
    private readonly Dictionary<Type, AliasDefinition> _aliases = new();
    private readonly HashSet<Type> _converting = new();

    private ClrModelBuilder()
    {
    }

    /// <summary>
    /// Get the model of a type (cached)
    /// </summary>
    public static ClrModel Get(Type type)
    {
        return s_single.TryGetValue(type, out var model) ? model : s_single.GetOrAdd(type, Get(new[] { type }));
    }

    /// <summary>
    /// Get the model of a pair of types (cached)
    /// </summary>
    public static ClrModel Get(Type first, Type second)
    {
        return s_pairs.TryGetValue((first, second), out var model) ? model : s_pairs.GetOrAdd((first, second), Get(new[] { first, second }));
    }

    /// <summary>
    /// Get the model of a set of types (cached)
    /// </summary>
    public static ClrModel Get(IEnumerable<Type> types)
    {
        var roots = types.Distinct().OrderBy(x => x.AssemblyQualifiedName, StringComparer.Ordinal).ToArray();
        var key = string.Join("|", roots.Select(x => x.AssemblyQualifiedName));
        return s_cache.GetOrAdd(key, _ => new ClrModelBuilder().Build(roots));
    }

    /// <summary>
    /// Schema identity (schema name and type name) of a CLR type
    /// </summary>
    public static (string Schema, string Name) GetIdentity(Type type)
    {
        var attribute = type.GetCustomAttribute<SerializationTypeAttribute>(inherit: false);
        string id;
        if (attribute != null)
        {
            id = attribute.Id;
        }
        else
        {
            if (type.IsGenericType || type.IsNested && type.DeclaringType!.IsGenericType)
                throw new SerializationException($"Generic type '{type}' requires a [SerializationType] attribute");

            id = (type.Namespace ?? "global") + "." + type.Name;
        }

        var separator = id.LastIndexOf('.');
        if (separator <= 0 || !Names.IsQualifiedName(id))
            throw new SerializationException($"Invalid type identity '{id}' of '{type}': expected 'schema.Type'");

        return (id.Substring(0, separator), id.Substring(separator + 1));
    }

    private ClrModel Build(Type[] roots)
    {
        foreach (var root in roots)
            Discover(root);

        while (_pending.Count > 0)
        {
            var type = _pending.Dequeue();
            if (type.IsEnum)
                continue;

            var info = _model.Structs[type];
            var baseType = type.BaseType;
            if (baseType != null && baseType != typeof(object) && baseType != typeof(ValueType) && IsStructCandidate(baseType))
            {
                Discover(baseType);
                info.Base = _model.Structs[baseType];
            }

            foreach (var attribute in type.GetCustomAttributes<SerializationDerivedTypeAttribute>(inherit: false))
            {
                if (!type.IsAssignableFrom(attribute.DerivedType))
                    throw new SerializationException($"'{attribute.DerivedType}' does not derive from '{type}'");

                Discover(attribute.DerivedType);
            }

            foreach (var member in GetMembers(type))
                DiscoverMemberType(GetMemberType(member), GetMemberConverter(member));
        }

        CreateSchemas();
        return _model;
    }

    private void Discover(Type type)
    {
        if (type.IsEnum)
        {
            if (_model.Enums.ContainsKey(type))
                return;

            var (schema, name) = GetIdentity(type);
            var info = new ClrEnumInfo(type, schema, name);
            Register(info.QualifiedName, type);
            _model.Enums.Add(type, info);
            _model.EnumsByIdentity.Add(info.QualifiedName, info);
            return;
        }

        if (!IsStructCandidate(type))
            throw new SerializationException($"Type '{type}' cannot be serialized as a struct");

        if (_model.Structs.ContainsKey(type))
            return;

        var (schemaName, typeName) = GetIdentity(type);
        var structInfo = new ClrTypeInfo(type, schemaName, typeName)
        {
            IsReference = type.GetCustomAttribute<SerializationTypeAttribute>(inherit: false)?.IsReference ?? false,
            Create = Accessors.Constructor(type),
        };

        Register(structInfo.QualifiedName, type);
        _model.Structs.Add(type, structInfo);
        _model.StructsByIdentity.Add(structInfo.QualifiedName, structInfo);
        _pending.Enqueue(type);
    }

    private void Register(string identity, Type type)
    {
        var existing = _model.StructsByIdentity.TryGetValue(identity, out var structInfo) ? structInfo.Type
            : _model.EnumsByIdentity.TryGetValue(identity, out var enumInfo) ? enumInfo.Type
            : null;

        if (existing != null && existing != type)
            throw new SerializationException($"Type identity '{identity}' is used by '{existing}' and '{type}'");
    }

    private void DiscoverMemberType(Type type, ISurrogateConverter? converter = null)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        converter ??= FindConverter(type);
        if (converter != null)
        {
            if (!_converting.Add(type))
                throw new SerializationException($"Converters of '{type}' are cyclic");

            if (converter.GetType().GetCustomAttribute<SerializationTypeAttribute>(inherit: false) != null)
                _aliasIdentities[converter.GetType()] = GetIdentity(converter.GetType());

            DiscoverMemberType(converter.SurrogateType);
            _converting.Remove(type);
            return;
        }

        if (type == typeof(object) || type == typeof(string) || IsBytesType(type) || type.IsPrimitive || ScalarKind(type).HasValue)
            return;

        if (type.IsEnum)
        {
            Discover(type);
            return;
        }

        if (type.IsArray)
        {
            DiscoverMemberType(type.GetElementType()!);
            return;
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(Link<>) || IsListDefinition(definition) || IsMapDefinition(definition))
            {
                foreach (var argument in type.GetGenericArguments())
                    DiscoverMemberType(argument);

                return;
            }
        }

        Discover(type);
    }

    private void CreateSchemas()
    {
        // Schemas and definitions, sorted by name for a stable layout
        var schemaNames = _model.Structs.Values.Select(x => x.SchemaName)
            .Concat(_model.Enums.Values.Select(x => x.SchemaName))
            .Concat(_aliasIdentities.Values.Select(x => x.Schema))
            .Distinct()
            .OrderBy(x => x, StringComparer.Ordinal);
        foreach (var name in schemaNames)
            _model.Schemas.Add(name, new Schema(name, GetSchemaVersion(name)));

        var declarations = _model.Structs.Values.Select(x => (Name: x.Name, Schema: x.SchemaName, Item: (object)x))
            .Concat(_model.Enums.Values.Select(x => (Name: x.Name, Schema: x.SchemaName, Item: (object)x)))
            .OrderBy(x => x.Schema, StringComparer.Ordinal)
            .ThenBy(x => x.Name, StringComparer.Ordinal);

        foreach (var declaration in declarations)
        {
            var schema = _model.Schemas[declaration.Schema];
            switch (declaration.Item)
            {
                case ClrTypeInfo info:
                    info.Definition = schema.AddStruct(info.Name, null, info.IsAbstract);
                    info.Definition.IsPositional = info.Type.GetCustomAttribute<SerializationTypeAttribute>(inherit: false)?.IsPositional ?? false;
                    break;
                case ClrEnumInfo info:
                    var underlying = ScalarKind(Enum.GetUnderlyingType(info.Type))!.Value;
                    info.Definition = schema.AddEnum(info.Name, underlying);
                    foreach (var field in info.Type.GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(x => x.MetadataToken))
                    {
                        var value = field.GetValue(null)!;
                        var schemaValue = underlying.IsSignedInteger() ? Convert.ToInt64(value) : unchecked((long)Convert.ToUInt64(value));
                        info.Definition.AddValue(field.Name, schemaValue);
                        info.ValuesByName[field.Name] = value;
                    }

                    break;
            }
        }

        // Bases and members
        foreach (var info in _model.Structs.Values.OrderBy(x => x.QualifiedName, StringComparer.Ordinal))
        {
            var schema = _model.Schemas[info.SchemaName];
            if (info.Base != null)
            {
                info.Definition.Base = info.Base.Definition;
                AddImport(schema, info.Base.Definition.Schema);
            }

            foreach (var member in GetMembers(info.Type))
            {
                var clrMember = CreateMember(schema, member);
                info.OwnMembers.Add(clrMember);
                clrMember.Field = info.Definition.AddField(clrMember.Name, clrMember.ValueType.SchemaType, clrMember.DefaultValue, clrMember.Aliases);
            }
        }

        foreach (var schema in _model.Schemas.Values)
            schema.Validate();

        foreach (var info in _model.Structs.Values)
        {
            var chain = new List<ClrTypeInfo>();
            for (var current = info; current != null; current = current.Base)
                chain.Insert(0, current);

            info.AllMembers = chain.SelectMany(x => x.OwnMembers).ToArray();
            info.SlotCount = info.AllMembers.Count(x => x.Field.HasPresenceSlot);
        }
    }

    private ClrMember CreateMember(Schema schema, MemberInfo member)
    {
        var name = member.GetCustomAttribute<SerializationFieldAttribute>()?.Name ?? CamelCase(member.Name);
        var isFixed = member.GetCustomAttribute<SerializationFixedAttribute>() != null;
        var isReference = member.GetCustomAttribute<SerializationReferenceAttribute>() != null;
        var valueType = Map(schema, GetMemberType(member), NullabilityNode.Of(member), isReference, $"{member.DeclaringType}.{member.Name}", GetMemberConverter(member));
        if (isFixed)
        {
            if (!valueType.SchemaType.Kind.AllowsFixed())
                throw new SerializationException($"[SerializationFixed] is not valid on '{member.DeclaringType}.{member.Name}'");

            // The surrogates of a converted value are written with the encoding of the member
            for (var current = valueType; current != null; current = current.Surrogate)
                current.SchemaType = current.SchemaType.AsFixed();
        }

        var clrMember = new ClrMember(member, name, valueType)
        {
            Aliases = member.GetCustomAttribute<SerializationAliasAttribute>()?.Aliases ?? Array.Empty<string>(),
            Accessor = valueType.Converter.CreateAccessor(member, valueType),
        };

        var defaultAttribute = member.GetCustomAttribute<DefaultValueAttribute>();
        if (defaultAttribute != null)
        {
            var type = valueType.SchemaType;
            var raw = defaultAttribute.Value;
            var data = raw != null && raw.GetType().IsEnum ? DataScalar.FromString(raw.ToString() ?? string.Empty) : DataValue.From(raw);
            clrMember.DefaultValue = DataConvert.CoerceScalar(data, type, $"Default value of '{member.DeclaringType}.{member.Name}'");
            clrMember.DefaultClrValue = ClrConvert.FromData(clrMember.DefaultValue, valueType);
            clrMember.Accessor.SetDefault(clrMember.DefaultClrValue);
        }

        return clrMember;
    }

    private ClrValueType Map(Schema schema, Type type, NullabilityNode nullability, bool forceReference, string context, ISurrogateConverter? converter = null)
    {
        var value = MapShape(schema, type, nullability, forceReference, context, converter);
        value.Converter = ClrConverter.Create(value);
        return value;
    }

    private ClrValueType MapShape(Schema schema, Type type, NullabilityNode nullability, bool forceReference, string context, ISurrogateConverter? converter)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            var inner = Map(schema, underlying, NullabilityNode.Unknown, forceReference, context, converter);
            var result = new ClrValueType(type, inner.Shape, inner.SchemaType.AsNullable())
            {
                Struct = inner.Struct,
                Enum = inner.Enum,
                Complete = inner.Complete,
                Surrogate = inner.Surrogate,
                SurrogateConverter = inner.SurrogateConverter,
            };

            return result;
        }

        var nullable = !type.IsValueType && nullability.IsNullable != false;
        converter ??= FindConverter(type);
        if (converter != null)
        {
            var converted = MapConverted(schema, type, forceReference, context, converter);
            if (nullable)
                converted.SchemaType = converted.SchemaType.AsNullable();

            return converted;
        }

        ClrValueType value;
        var kind = ScalarKind(type);
        if (type == typeof(object))
        {
            return new ClrValueType(type, ClrShape.Any, TypeReference.Any);
        }
        else if (kind.HasValue)
        {
            value = new ClrValueType(type, ClrShape.Scalar, TypeReference.Primitive(kind.Value));
        }
        else if (type.IsEnum)
        {
            var info = _model.Enums[type];
            AddImport(schema, info.Definition.Schema);
            value = new ClrValueType(type, ClrShape.Enum, TypeReference.Of(info.Definition)) { Enum = info };
        }
        else if (IsBytesType(type))
        {
            value = new ClrValueType(type, ClrShape.ByteArray, TypeReference.Bytes) { Complete = BytesFactory(type) };
        }
        else if (type.IsArray)
        {
            if (type.GetArrayRank() != 1)
                throw new SerializationException($"Multi-dimensional arrays are not supported ({context})");

            var elementType = type.GetElementType()!;
            var element = Map(schema, elementType, nullability.Element ?? NullabilityNode.Unknown, forceReference, context);
            var listType = typeof(List<>).MakeGenericType(elementType);
            value = new ClrValueType(type, ClrShape.List, TypeReference.List(element.SchemaType))
            {
                Element = element,
                CreateCollection = CollectionFactory(listType),
                AddItem = (list, item) => ((IList)list).Add(item),
                Complete = list =>
                {
                    var source = (IList)list;
                    var array = Array.CreateInstance(elementType, source.Count);
                    source.CopyTo(array, 0);
                    return array;
                },
            };
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Link<>))
        {
            var target = type.GetGenericArguments()[0];
            var info = _model.FindStruct(target) ?? throw new SerializationException($"Link target '{target}' is not a struct ({context})");
            AddImport(schema, info.Definition.Schema);
            value = new ClrValueType(type, ClrShape.Link, TypeReference.Ref(info.Definition)) { Struct = info, LinkTarget = target };
        }
        else if (type.IsGenericType && IsMapDefinition(type.GetGenericTypeDefinition()))
        {
            var arguments = type.GetGenericArguments();
            var key = Map(schema, arguments[0], nullability.Argument(0), false, context);
            key.SchemaType = key.SchemaType.AsNonNullable();
            var item = Map(schema, arguments[1], nullability.Argument(1), forceReference, context);
            var concrete = type.IsInterface || type.IsAbstract ? typeof(Dictionary<,>).MakeGenericType(arguments) : type;
            value = new ClrValueType(type, ClrShape.Map, TypeReference.Map(key.SchemaType, item.SchemaType))
            {
                Key = key,
                Value = item,
                CreateCollection = CollectionFactory(concrete),
            };
        }
        else if (type.IsGenericType && IsListDefinition(type.GetGenericTypeDefinition()))
        {
            var elementType = type.GetGenericArguments()[0];
            var element = Map(schema, elementType, nullability.Argument(0), forceReference, context);
            var definition = type.GetGenericTypeDefinition();
            var concrete = type.IsInterface
                ? (definition == typeof(ISet<>) ? typeof(HashSet<>) : typeof(List<>)).MakeGenericType(elementType)
                : type;

            var add = concrete.GetMethod("Add", new[] { elementType }) ?? throw new SerializationException($"Collection '{concrete}' has no Add method ({context})");
            var addDelegate = CompileAdd(concrete, elementType, add);
            value = new ClrValueType(type, ClrShape.List, TypeReference.List(element.SchemaType))
            {
                Element = element,
                CreateCollection = CollectionFactory(concrete),
                AddItem = addDelegate,
            };
        }
        else if (IsStructCandidate(type))
        {
            var info = _model.Structs[type];
            AddImport(schema, info.Definition.Schema);
            var reference = forceReference || info.IsReference;
            value = new ClrValueType(type, ClrShape.Struct, reference ? TypeReference.Ref(info.Definition) : TypeReference.Of(info.Definition)) { Struct = info };
        }
        else
        {
            throw new SerializationException($"Type '{type}' is not supported ({context})");
        }

        if (nullable)
            value.SchemaType = value.SchemaType.AsNullable();

        return value;
    }

    /// <summary>
    /// Map a value serialized through a converter: the surrogate is mapped in place of the value, named by an alias
    /// if the converter declares a type identity
    /// </summary>
    private ClrValueType MapConverted(Schema schema, Type type, bool forceReference, string context, ISurrogateConverter converter)
    {
        var converterType = converter.GetType();
        var aliasSchema = _aliasIdentities.TryGetValue(converterType, out var identity) ? _model.Schemas[identity.Schema] : null;
        if (!_converting.Add(type))
            throw new SerializationException($"Converters of '{type}' are cyclic ({context})");

        var surrogate = Map(aliasSchema ?? schema, converter.SurrogateType, NullabilityNode.Unknown, forceReference, context);
        _converting.Remove(type);
        surrogate.SchemaType = surrogate.SchemaType.AsNonNullable();

        var schemaType = surrogate.SchemaType;
        if (aliasSchema != null)
        {
            if (!_aliases.TryGetValue(converterType, out var alias))
            {
                if (aliasSchema.FindOwnType(identity.Name) != null)
                    throw new SerializationException($"Type identity '{aliasSchema.Name}.{identity.Name}' of converter '{converterType}' is already used");

                alias = aliasSchema.AddAlias(identity.Name, schemaType);
                _aliases.Add(converterType, alias);
            }
            else if (!alias.Target.Equals(schemaType))
            {
                throw new SerializationException($"Converter '{converterType}' maps to {alias.Target} and {schemaType} ({context})");
            }

            AddImport(schema, aliasSchema);
            schemaType = TypeReference.Of(alias);
        }

        return new ClrValueType(type, ClrShape.Converted, schemaType)
        {
            Surrogate = surrogate,
            SurrogateConverter = converter,
        };
    }

    /// <summary>
    /// Converter applied to a member (<see cref="SerializationConverterAttribute"/> on the member), if any
    /// </summary>
    private ISurrogateConverter? GetMemberConverter(MemberInfo member)
    {
        if (_memberConverters.TryGetValue(member, out var converter))
            return converter;

        var attributes = member.GetCustomAttributes<SerializationConverterAttribute>().ToArray();
        if (attributes.Length > 1)
            throw new SerializationException($"'{member.DeclaringType}.{member.Name}' declares several converters");

        if (attributes.Length == 1)
        {
            var type = GetMemberType(member);
            converter = CreateConverter(attributes[0], Nullable.GetUnderlyingType(type) ?? type, $"{member.DeclaringType}.{member.Name}");
        }

        _memberConverters.Add(member, converter);
        return converter;
    }

    /// <summary>
    /// Converter of every value of a CLR type: declared on the type, or on a loaded assembly (cached)
    /// </summary>
    internal static ISurrogateConverter? FindConverter(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(object))
            return null;

        return s_typeConverters.GetOrAdd(type, x =>
        {
            var attributes = x.GetCustomAttributes<SerializationConverterAttribute>(inherit: false).ToArray();
            if (attributes.Length > 1)
                throw new SerializationException($"Type '{x}' declares several converters");

            var attribute = attributes.Length == 1 ? attributes[0] : s_assemblyConverters.Value.TryGetValue(x, out var declared) ? declared : null;
            return attribute != null ? CreateConverter(attribute, x, $"'{x}'") : null;
        });
    }

    private static ISurrogateConverter CreateConverter(SerializationConverterAttribute attribute, Type expected, string context)
    {
        var (converted, _) = GetConvertedTypes(attribute.ConverterType)
            ?? throw new SerializationException($"'{attribute.ConverterType}' does not derive from SerializationConverter<T, TSurrogate> ({context})");

        if (converted != expected)
            throw new SerializationException($"Converter '{attribute.ConverterType}' converts '{converted}', not '{expected}' ({context})");

        try
        {
            return (ISurrogateConverter)Activator.CreateInstance(attribute.ConverterType, attribute.Arguments)!;
        }
        catch (Exception e) when (e is MissingMethodException or MemberAccessException or TargetInvocationException or ArgumentException)
        {
            var message = e is TargetInvocationException { InnerException: { } inner } ? inner.Message : e.Message;
            throw new SerializationException($"Cannot create converter '{attribute.ConverterType}' ({context}): {message}");
        }
    }

    /// <summary>
    /// Type arguments of the <see cref="SerializationConverter{T, TSurrogate}"/> base of a converter type
    /// </summary>
    private static (Type Type, Type Surrogate)? GetConvertedTypes(Type converterType)
    {
        if (converterType.IsAbstract || converterType.IsGenericTypeDefinition)
            return null;

        for (var current = converterType; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(SerializationConverter<,>))
            {
                var arguments = current.GetGenericArguments();
                return (arguments[0], arguments[1]);
            }
        }

        return null;
    }

    /// <summary>
    /// Converters declared with <c>[assembly: SerializationConverter(...)]</c> on the loaded assemblies, by converted type
    /// </summary>
    private static Dictionary<Type, SerializationConverterAttribute> FindAssemblyConverters()
    {
        var result = new Dictionary<Type, SerializationConverterAttribute>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            IEnumerable<SerializationConverterAttribute> attributes;
            try
            {
                attributes = assembly.GetCustomAttributes<SerializationConverterAttribute>();
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var attribute in attributes)
            {
                var (converted, _) = GetConvertedTypes(attribute.ConverterType)
                    ?? throw new SerializationException($"'{attribute.ConverterType}' does not derive from SerializationConverter<T, TSurrogate> (assembly '{assembly.GetName().Name}')");

                if (result.TryGetValue(converted, out var existing) && existing.ConverterType != attribute.ConverterType)
                    throw new SerializationException($"Type '{converted}' has several assembly converters: '{existing.ConverterType}' and '{attribute.ConverterType}'");

                result[converted] = attribute;
            }
        }

        return result;
    }

    private static bool IsBytesType(Type type)
    {
        return type == typeof(byte[]) || type == typeof(ReadOnlyMemory<byte>) || type == typeof(Memory<byte>) || type == typeof(ArraySegment<byte>);
    }

    /// <summary>
    /// Conversion of the byte array read from data to a memory type
    /// </summary>
    private static Func<object, object>? BytesFactory(Type type)
    {
        if (type == typeof(ReadOnlyMemory<byte>))
            return bytes => new ReadOnlyMemory<byte>((byte[])bytes);

        if (type == typeof(Memory<byte>))
            return bytes => new Memory<byte>((byte[])bytes);

        if (type == typeof(ArraySegment<byte>))
            return bytes => new ArraySegment<byte>((byte[])bytes);

        return null;
    }

    private static void AddImport(Schema schema, Schema import)
    {
        if (!ReferenceEquals(schema, import) && !schema.Imports.Contains(import))
            schema.AddImport(import);
    }

    private static Func<int, object> CollectionFactory(Type type)
    {
        var withCapacity = type.GetConstructor(new[] { typeof(int) });
        if (withCapacity != null && (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>))))
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(int), "capacity");
            return System.Linq.Expressions.Expression.Lambda<Func<int, object>>(
                System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.New(withCapacity, parameter), typeof(object)), parameter).Compile();
        }

        var constructor = Accessors.Constructor(type) ?? throw new SerializationException($"Cannot instantiate collection '{type}'");
        return _ => constructor();
    }

    private static Action<object, object?> CompileAdd(Type collection, Type element, MethodInfo add)
    {
        var instance = System.Linq.Expressions.Expression.Parameter(typeof(object), "collection");
        var item = System.Linq.Expressions.Expression.Parameter(typeof(object), "item");
        var call = System.Linq.Expressions.Expression.Call(
            System.Linq.Expressions.Expression.Convert(instance, collection),
            add,
            System.Linq.Expressions.Expression.Convert(item, element));

        return System.Linq.Expressions.Expression.Lambda<Action<object, object?>>(call, instance, item).Compile();
    }

    private static IEnumerable<MemberInfo> GetMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var members = new List<MemberInfo>();
        foreach (var property in type.GetProperties(flags))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetMethod?.IsPublic != true || property.IsDefined(typeof(SerializationIgnoreAttribute)))
                continue;

            if (property.SetMethod == null)
                continue;

            members.Add(property);
        }

        foreach (var field in type.GetFields(flags))
        {
            if (field.IsInitOnly || field.IsDefined(typeof(SerializationIgnoreAttribute)))
                continue;

            members.Add(field);
        }

        return members.OrderBy(x => x.MetadataToken);
    }

    private static Type GetMemberType(MemberInfo member) => member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;

    private static uint GetSchemaVersion(string name)
    {
        // Version declared on any loaded assembly with [assembly: SerializationSchema(name, version)]
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            IEnumerable<SerializationSchemaAttribute> attributes;
            try
            {
                attributes = assembly.GetCustomAttributes<SerializationSchemaAttribute>();
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var attribute in attributes)
            {
                if (string.Equals(attribute.Name, name, StringComparison.Ordinal))
                    return attribute.Version;
            }
        }

        return 0;
    }

    private static string CamelCase(string name)
    {
        if (name.Length == 0 || char.IsLower(name[0]))
            return name;

        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i > 0 && i + 1 < chars.Length && !char.IsUpper(chars[i + 1]))
                break;

            if (!char.IsUpper(chars[i]))
                break;

            chars[i] = char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }

    internal static TypeKind? ScalarKind(Type type)
    {
        if (type == typeof(bool))
            return TypeKind.Bool;

        if (type == typeof(byte))
            return TypeKind.U8;

        if (type == typeof(ushort))
            return TypeKind.U16;

        if (type == typeof(uint))
            return TypeKind.U32;

        if (type == typeof(ulong))
            return TypeKind.U64;

        if (type == typeof(sbyte))
            return TypeKind.S8;

        if (type == typeof(short))
            return TypeKind.S16;

        if (type == typeof(int))
            return TypeKind.S32;

        if (type == typeof(long))
            return TypeKind.S64;

        if (type == typeof(float))
            return TypeKind.F32;

        if (type == typeof(double))
            return TypeKind.F64;

        if (type == typeof(string))
            return TypeKind.String;

        return null;
    }

    private static bool IsStructCandidate(Type type)
    {
        return (type.IsClass || (type.IsValueType && !type.IsPrimitive && !type.IsEnum))
            && type != typeof(string)
            && type != typeof(object)
            && !type.IsArray
            && !typeof(Delegate).IsAssignableFrom(type)
            && !(type.IsGenericType && (IsListDefinition(type.GetGenericTypeDefinition()) || IsMapDefinition(type.GetGenericTypeDefinition())))
            && type.Namespace?.StartsWith("System", StringComparison.Ordinal) != true;
    }

    private static bool IsListDefinition(Type definition)
    {
        return definition == typeof(List<>) || definition == typeof(IList<>) || definition == typeof(ICollection<>) || definition == typeof(IEnumerable<>)
            || definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(HashSet<>) || definition == typeof(ISet<>);
    }

    private static bool IsMapDefinition(Type definition)
    {
        return definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(SortedDictionary<,>);
    }
}
