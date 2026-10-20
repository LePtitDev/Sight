using System;
using System.Collections.Generic;
using System.Linq;
using Sight.Serialization.Binary;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;

namespace Sight.Serialization.Schemas;

/// <summary>
/// Conversion between <see cref="Schema"/> and <c>core.Schema</c> values, canonical encoding and hash
/// </summary>
internal static class SchemaCodec
{
    public static ulong ComputeHash(Schema schema)
    {
        using var encoder = new BinaryEncoder();
        var writer = new DataBinaryWriter(encoder, CoreSchema.Table, null, int.MaxValue);
        writer.WriteExactStruct(CoreSchema.SchemaType, ToData(schema), "schema");
        return XxHash64.Hash(encoder.WrittenSpan);
    }

    public static DataStruct ToData(Schema schema)
    {
        schema.Validate();
        var data = new DataStruct(CoreSchema.SchemaType)
        {
            ["name"] = schema.Name,
            ["version"] = schema.Version,
        };

        var imports = new DataList(TypeReference.Of(CoreSchema.SchemaRef));
        foreach (var import in schema.Imports)
        {
            imports.Add(new DataStruct(CoreSchema.SchemaRef)
            {
                ["name"] = import.Name,
                ["version"] = import.Version,
                ["hash"] = import.Hash,
            });
        }

        data["imports"] = imports;

        var types = new DataList(TypeReference.Of(CoreSchema.TypeDef));
        foreach (var type in schema.Types)
        {
            types.Add(type switch
            {
                StructDefinition definition => StructToData(schema, definition),
                EnumDefinition definition => EnumToData(definition),
                AliasDefinition definition => new DataStruct(CoreSchema.AliasDef)
                {
                    ["name"] = definition.Name,
                    ["target"] = TypeToData(schema, definition.Target),
                },
                _ => throw new InvalidOperationException(),
            });
        }

        data["types"] = types;
        return data;
    }

    /// <summary>
    /// Build schemas from <c>core.Schema</c> values; imports are searched in the list first, then resolved externally
    /// </summary>
    public static IReadOnlyList<Schema> FromData(IReadOnlyList<DataStruct> values, Func<string, uint, ulong, Schema> resolveImport)
    {
        var byName = new Dictionary<string, DataStruct>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var name = value["name"].AsString();
            if (byName.ContainsKey(name))
                throw new SerializationException($"Schema '{name}' is defined twice");

            byName.Add(name, value);
        }

        var built = new Dictionary<string, Schema>(StringComparer.Ordinal);
        var building = new HashSet<string>(StringComparer.Ordinal);
        return values.Select(x => Build(x["name"].AsString())).ToArray();

        Schema Build(string name)
        {
            if (built.TryGetValue(name, out var existing))
                return existing;

            if (!building.Add(name))
                throw new SerializationException($"Cyclic import on schema '{name}'");

            var value = byName[name];
            var imports = new List<Schema>();
            foreach (var import in (DataList)value["imports"])
            {
                var importName = import["name"].AsString();
                var importVersion = (uint)import["version"].AsUInt64();
                if (byName.TryGetValue(importName, out var inline))
                {
                    if (inline["version"].AsUInt64() != importVersion)
                        throw new SerializationException($"Schema '{name}' imports '{importName}' {importVersion}, but version {inline["version"]} is provided");

                    imports.Add(Build(importName));
                }
                else
                {
                    imports.Add(resolveImport(importName, importVersion, import["hash"].AsUInt64()));
                }
            }

            var schema = new Schema(name, (uint)value["version"].AsUInt64(), imports);
            Populate(schema, (DataList)value["types"]);
            schema.Validate();
            built.Add(name, schema);
            return schema;
        }
    }

    private static DataStruct StructToData(Schema schema, StructDefinition definition)
    {
        var data = new DataStruct(CoreSchema.StructDef)
        {
            ["name"] = definition.Name,
            ["base"] = definition.Base != null ? (uint)schema.GetLocalIndex(definition.Base) : DataValue.Null,
            ["isAbstract"] = definition.IsAbstract,
            ["isPositional"] = definition.IsPositional,
        };

        var fields = new DataList(TypeReference.Of(CoreSchema.Field));
        foreach (var field in definition.Fields)
        {
            fields.Add(new DataStruct(CoreSchema.Field)
            {
                ["name"] = field.Name,
                ["type"] = TypeToData(schema, field.Type),
                ["aliases"] = new DataList(field.Aliases.Select(x => (DataValue)x), TypeReference.String),
                ["default"] = field.DefaultValue is DataEnum value ? value.Name : field.DefaultValue ?? DataValue.Null,
            });
        }

        data["fields"] = fields;
        return data;
    }

    private static DataStruct EnumToData(EnumDefinition definition)
    {
        var values = new DataList(TypeReference.Of(CoreSchema.EnumValue));
        foreach (var value in definition.Values)
        {
            values.Add(new DataStruct(CoreSchema.EnumValue)
            {
                ["name"] = value.Name,
                ["value"] = value.Value,
            });
        }

        return new DataStruct(CoreSchema.EnumDef)
        {
            ["name"] = definition.Name,
            ["underlying"] = KindValue(definition.Underlying),
            ["values"] = values,
        };
    }

    private static DataStruct TypeToData(Schema schema, TypeReference type)
    {
        var flags = (byte)((type.IsNullable ? 1 : 0) | (type.IsFixed ? 2 : 0));
        var args = new DataList(TypeReference.Of(CoreSchema.TypeRef));
        if (type.Alias != null)
        {
            return new DataStruct(CoreSchema.TypeRef)
            {
                ["kind"] = new DataEnum(CoreSchema.Kind.FindValue(CoreSchema.AliasKind)!),
                ["flags"] = flags,
                ["index"] = (uint)schema.GetLocalIndex(type.Alias),
                ["args"] = args,
            };
        }

        if (type.Kind == TypeKind.List)
        {
            args.Add(TypeToData(schema, type.ElementType!));
        }
        else if (type.Kind == TypeKind.Map)
        {
            args.Add(TypeToData(schema, type.KeyType!));
            args.Add(TypeToData(schema, type.ValueType!));
        }

        return new DataStruct(CoreSchema.TypeRef)
        {
            ["kind"] = KindValue(type.Kind),
            ["flags"] = flags,
            ["index"] = type.Definition != null ? (uint)schema.GetLocalIndex(type.Definition) : DataValue.Null,
            ["args"] = args,
        };
    }

    private static DataEnum KindValue(TypeKind kind) => new(CoreSchema.Kind.FindValue((long)kind)!);

    private static void Populate(Schema schema, DataList types)
    {
        // First pass: declare every type so that references can be resolved
        var declared = new List<(TypeDefinition Definition, DataStruct Data)>();
        foreach (DataStruct type in types)
        {
            var name = type["name"].AsString();
            if (ReferenceEquals(type.Definition, CoreSchema.StructDef))
            {
                var definition = schema.AddStruct(name, null, type["isAbstract"].AsBoolean());
                definition.IsPositional = type["isPositional"].AsBoolean();
                declared.Add((definition, type));
            }
            else if (ReferenceEquals(type.Definition, CoreSchema.AliasDef))
            {
                declared.Add((schema.AddUnresolvedAlias(name), type));
            }
            else
            {
                var underlying = ToKind(type["underlying"]);
                if (!underlying.IsInteger())
                    throw new SerializationException($"Invalid underlying type {underlying} for enum '{schema.Name}.{name}'");

                var definition = schema.AddEnum(name, underlying);
                foreach (DataStruct value in (DataList)type["values"])
                    definition.AddValue(value["name"].AsString(), value["value"].AsInt64());

                declared.Add((definition, type));
            }
        }

        var local = schema.LocalTypes;

        // Second pass: alias targets, resolved on demand since an alias may refer to another one declared later
        var aliases = declared.Where(x => x.Definition is AliasDefinition).ToDictionary(x => (AliasDefinition)x.Definition, x => x.Data);
        var resolving = new HashSet<AliasDefinition>();
        foreach (var alias in aliases.Keys)
            ResolveAlias(alias);

        void ResolveAlias(AliasDefinition alias)
        {
            if (alias.IsResolved)
                return;

            if (!resolving.Add(alias))
                throw new SerializationException($"Cyclic type alias '{alias.QualifiedName}'");

            alias.Resolve(TypeFromData(local, (DataStruct)aliases[alias]["target"], ResolveAlias));
        }

        // Third pass: bases and fields
        foreach (var (definition, data) in declared)
        {
            if (definition is not StructDefinition structDefinition)
                continue;

            if (!data["base"].IsNull)
                structDefinition.Base = GetLocal(local, data["base"].AsUInt64()) as StructDefinition ?? throw new SerializationException($"Base of '{structDefinition.QualifiedName}' is not a struct");

            foreach (DataStruct field in (DataList)data["fields"])
            {
                var fieldType = TypeFromData(local, (DataStruct)field["type"], ResolveAlias);
                var defaultValue = field["default"];
                structDefinition.AddField(
                    field["name"].AsString(),
                    fieldType,
                    defaultValue.IsNull ? null : defaultValue,
                    ((DataList)field["aliases"]).Select(x => x.AsString()));
            }
        }
    }

    private static TypeReference TypeFromData(IReadOnlyList<TypeDefinition> local, DataStruct data, Action<AliasDefinition> resolveAlias)
    {
        var kind = ToKind(data["kind"]);
        var flags = (byte)data["flags"].AsUInt64();
        if ((flags & ~3) != 0)
            throw new SerializationException($"Invalid type flags 0x{flags:X2}");

        var args = (DataList)data["args"];
        var index = data["index"];
        TypeReference type;
        switch (kind)
        {
            case (TypeKind)CoreSchema.AliasKind:
                if (index.IsNull || args.Count != 0)
                    throw new SerializationException("Invalid alias type reference");

                var alias = GetLocal(local, index.AsUInt64()) as AliasDefinition ?? throw new SerializationException($"Type index {index} is not a type alias");
                resolveAlias(alias);
                type = TypeReference.Of(alias);
                break;
            case TypeKind.Enum:
            case TypeKind.Struct:
            case TypeKind.Ref:
                if (index.IsNull || args.Count != 0)
                    throw new SerializationException($"Invalid {kind} type reference");

                var definition = GetLocal(local, index.AsUInt64());
                type = kind switch
                {
                    TypeKind.Ref => TypeReference.Ref(definition as StructDefinition ?? throw new SerializationException("ref target must be a struct")),
                    _ => TypeReference.Of(definition),
                };

                if ((kind == TypeKind.Enum) != (definition is EnumDefinition) || definition is AliasDefinition)
                    throw new SerializationException($"Type index {index} does not match kind {kind}");

                break;
            case TypeKind.List:
                if (!index.IsNull || args.Count != 1)
                    throw new SerializationException("Invalid list type reference");

                type = TypeReference.List(TypeFromData(local, (DataStruct)args[0], resolveAlias));
                break;
            case TypeKind.Map:
                if (!index.IsNull || args.Count != 2)
                    throw new SerializationException("Invalid map type reference");

                type = TypeReference.Map(TypeFromData(local, (DataStruct)args[0], resolveAlias), TypeFromData(local, (DataStruct)args[1], resolveAlias));
                break;
            case TypeKind.Null:
                throw new SerializationException("null is not a declarable type");
            default:
                if (!index.IsNull || args.Count != 0)
                    throw new SerializationException($"Invalid {kind} type reference");

                type = TypeReference.Primitive(kind);
                break;
        }

        return type.WithFlags((flags & 1) != 0, (flags & 2) != 0);
    }

    private static TypeDefinition GetLocal(IReadOnlyList<TypeDefinition> local, ulong index)
    {
        return index < (ulong)local.Count ? local[(int)index] : throw new SerializationException($"Invalid local type index {index}");
    }

    private static TypeKind ToKind(DataValue value) => (TypeKind)((DataEnum)value).Value.Value;
}
