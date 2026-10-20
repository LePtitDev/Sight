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

    private readonly ClrModel _model = new();
    private readonly Queue<Type> _pending = new();

    private ClrModelBuilder()
    {
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
                DiscoverMemberType(GetMemberType(member));
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

    private void DiscoverMemberType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(object) || type == typeof(string) || type == typeof(byte[]) || type.IsPrimitive || ScalarKind(type).HasValue)
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
            if (definition == typeof(ExternalRef<>) || IsListDefinition(definition) || IsMapDefinition(definition))
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
        var schemaNames = _model.Structs.Values.Select(x => x.SchemaName).Concat(_model.Enums.Values.Select(x => x.SchemaName)).Distinct().OrderBy(x => x, StringComparer.Ordinal);
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
        }
    }

    private ClrMember CreateMember(Schema schema, MemberInfo member)
    {
        var name = member.GetCustomAttribute<SerializationFieldAttribute>()?.Name ?? CamelCase(member.Name);
        var isFixed = member.GetCustomAttribute<SerializationFixedAttribute>() != null;
        var isReference = member.GetCustomAttribute<SerializationReferenceAttribute>() != null;
        var valueType = Map(schema, GetMemberType(member), NullabilityNode.Of(member), isReference, $"{member.DeclaringType}.{member.Name}");
        if (isFixed)
        {
            if (!valueType.SchemaType.Kind.AllowsFixed())
                throw new SerializationException($"[SerializationFixed] is not valid on '{member.DeclaringType}.{member.Name}'");

            valueType.SchemaType = valueType.SchemaType.AsFixed();
        }

        var clrMember = new ClrMember(member, name, valueType)
        {
            Aliases = member.GetCustomAttribute<SerializationAliasAttribute>()?.Aliases ?? Array.Empty<string>(),
            Getter = Accessors.Getter(member),
            Setter = Accessors.Setter(member),
        };

        var defaultAttribute = member.GetCustomAttribute<DefaultValueAttribute>();
        if (defaultAttribute != null)
        {
            var type = valueType.SchemaType;
            var raw = defaultAttribute.Value;
            var data = raw != null && raw.GetType().IsEnum ? DataScalar.FromString(raw.ToString() ?? string.Empty) : DataValue.From(raw);
            clrMember.DefaultValue = DataConvert.CoerceScalar(data, type, $"Default value of '{member.DeclaringType}.{member.Name}'");
            clrMember.DefaultClrValue = ClrConvert.FromData(clrMember.DefaultValue, valueType);
        }

        return clrMember;
    }

    private ClrValueType Map(Schema schema, Type type, NullabilityNode nullability, bool forceReference, string context)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            var inner = Map(schema, underlying, NullabilityNode.Unknown, forceReference, context);
            var result = new ClrValueType(type, inner.Shape, inner.SchemaType.AsNullable())
            {
                Struct = inner.Struct,
                Enum = inner.Enum,
            };

            return result;
        }

        var nullable = !type.IsValueType && nullability.IsNullable != false;
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
        else if (type == typeof(byte[]))
        {
            value = new ClrValueType(type, ClrShape.ByteArray, TypeReference.List(TypeReference.U8));
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
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ExternalRef<>))
        {
            var target = type.GetGenericArguments()[0];
            var info = _model.FindStruct(target) ?? throw new SerializationException($"External reference target '{target}' is not a struct ({context})");
            AddImport(schema, info.Definition.Schema);
            value = new ClrValueType(type, ClrShape.ExtRef, TypeReference.ExtRef(info.Definition)) { Struct = info, ExtRefTarget = target };
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
