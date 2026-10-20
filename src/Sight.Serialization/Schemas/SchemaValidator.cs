using System;
using System.Collections.Generic;
using System.Linq;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;

namespace Sight.Serialization.Schemas;

internal static class SchemaValidator
{
    public static void Validate(Schema schema)
    {
        CheckImportCycles(schema, new List<Schema>());
        foreach (var import in schema.Imports)
            import.Validate();

        _ = schema.Closure;

        var importNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var import in schema.Imports)
        {
            if (string.Equals(import.Name, schema.Name, StringComparison.Ordinal) || !importNames.Add(import.Name))
                throw Error(schema, $"duplicate import '{import.Name}'");
        }

        var typeNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in schema.Types)
        {
            if (!Names.IsIdentifier(type.Name) || Names.TypeKeywords.Contains(type.Name))
                throw Error(schema, $"invalid type name '{type.Name}'");

            if (!typeNames.Add(type.Name))
                throw Error(schema, $"duplicate type name '{type.Name}'");
        }

        var local = new HashSet<TypeDefinition>(schema.LocalTypes);
        foreach (var type in schema.Types)
        {
            switch (type)
            {
                case StructDefinition definition:
                    ValidateStruct(schema, definition, local);
                    break;
                case EnumDefinition definition:
                    ValidateEnum(schema, definition);
                    break;
                case AliasDefinition definition:
                    ValidateAlias(schema, definition, local);
                    break;
            }
        }
    }

    private static void CheckImportCycles(Schema schema, List<Schema> path)
    {
        if (path.Contains(schema))
            throw Error(schema, "cyclic import");

        path.Add(schema);
        foreach (var import in schema.Imports)
            CheckImportCycles(import, path);

        path.RemoveAt(path.Count - 1);
    }

    private static void ValidateStruct(Schema schema, StructDefinition definition, HashSet<TypeDefinition> local)
    {
        var context = definition.QualifiedName;
        if (definition.Base != null && !local.Contains(definition.Base))
            throw Error(schema, $"base '{definition.Base.QualifiedName}' of '{context}' is not declared by the schema or a direct import");

        var visited = new HashSet<StructDefinition>();
        for (var current = definition; current != null; current = current.Base)
        {
            if (!visited.Add(current))
                throw Error(schema, $"cyclic inheritance on '{context}'");
        }

        definition.ResetCache();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in definition.AllFields)
        {
            if (!Names.IsIdentifier(field.Name))
                throw Error(schema, $"invalid field name '{field.Name}' in '{context}'");

            if (!names.Add(field.Name))
                throw Error(schema, $"duplicate field '{field.Name}' in '{context}'");
        }

        foreach (var field in definition.Fields)
        {
            var fieldContext = $"{context}.{field.Name}";
            ValidateType(schema, field.Type, local, fieldContext);
            var aliases = new HashSet<string>(StringComparer.Ordinal);
            foreach (var alias in field.Aliases)
            {
                if (!Names.IsIdentifier(alias))
                    throw Error(schema, $"invalid alias '{alias}' of '{fieldContext}'");

                if (names.Contains(alias) || !aliases.Add(alias))
                    throw Error(schema, $"alias '{alias}' of '{fieldContext}' conflicts with another name");
            }

            if (field.DefaultValue != null && field.Type.Kind == TypeKind.Enum && field.DefaultValue is DataEnum value && !ReferenceEquals(value.Definition, field.Type.Definition))
                throw Error(schema, $"invalid default value of '{fieldContext}'");
        }

        if (definition.IsPositional)
        {
            if (definition.IsAbstract)
                throw Error(schema, $"abstract struct '{context}' cannot be positional");

            if (!definition.AllowsPositional)
                throw Error(schema, $"positional struct '{context}' must have at least one field, and only non-nullable scalar or enum fields");
        }
    }

    private static void ValidateAlias(Schema schema, AliasDefinition definition, HashSet<TypeDefinition> local)
    {
        var context = definition.QualifiedName;
        if (!definition.IsResolved)
            throw Error(schema, $"target of alias '{context}' is not resolved");

        var target = definition.Target;
        if (target.IsNullable || target.IsFixed)
            throw Error(schema, $"target of alias '{context}' cannot be nullable nor @fixed");

        if (target.Kind == TypeKind.Null)
            throw Error(schema, $"null is not a declarable type ({context})");

        ValidateType(schema, target, local, context);
    }

    private static void ValidateType(Schema schema, TypeReference type, HashSet<TypeDefinition> local, string context)
    {
        if (type.IsFixed && !type.Kind.AllowsFixed())
            throw Error(schema, $"@fixed is not allowed on {type.Kind.ToKeyword()} ({context})");

        if (type.Alias != null)
        {
            if (!local.Contains(type.Alias))
                throw Error(schema, $"type '{type.Alias.QualifiedName}' is not declared by the schema or a direct import ({context})");

            // The aliased type is validated with the alias, in the scope of its own schema
            if (type.Kind == TypeKind.Any && type.IsNullable)
                throw Error(schema, $"any cannot be nullable ({context})");

            return;
        }

        switch (type.Kind)
        {
            case TypeKind.Null:
                throw Error(schema, $"null is not a declarable type ({context})");
            case TypeKind.Any when type.IsNullable:
                throw Error(schema, $"any cannot be nullable ({context})");
            case TypeKind.Enum:
            case TypeKind.Struct:
            case TypeKind.Ref:
                if (type.Definition == null || !local.Contains(type.Definition))
                    throw Error(schema, $"type '{type.Definition?.QualifiedName}' is not declared by the schema or a direct import ({context})");

                if (type.Kind == TypeKind.Ref && type.Definition is not StructDefinition)
                    throw Error(schema, $"{type.Kind.ToKeyword()} target must be a struct ({context})");

                break;
            case TypeKind.List:
                ValidateType(schema, type.ElementType!, local, context);
                break;
            case TypeKind.Map:
                var key = type.KeyType!;
                if (key.IsNullable || (key.Kind is not TypeKind.Bool and not TypeKind.String and not TypeKind.Enum && !key.Kind.IsInteger()))
                    throw Error(schema, $"invalid map key type {key} ({context})");

                ValidateType(schema, key, local, context);
                ValidateType(schema, type.ValueType!, local, context);
                break;
        }
    }

    private static void ValidateEnum(Schema schema, EnumDefinition definition)
    {
        var context = definition.QualifiedName;
        var names = new HashSet<string>(StringComparer.Ordinal);
        var values = new HashSet<long>();
        foreach (var value in definition.Values)
        {
            if (!Names.IsIdentifier(value.Name))
                throw Error(schema, $"invalid enum value name '{value.Name}' in '{context}'");

            if (!names.Add(value.Name))
                throw Error(schema, $"duplicate enum value name '{value.Name}' in '{context}'");

            if (!values.Add(value.Value))
                throw Error(schema, $"duplicate enum value {value.Value} in '{context}'");

            if (definition.Underlying != TypeKind.U64 && !DataRanges.Fits(definition.Underlying, value.Value))
                throw Error(schema, $"value {value.Value} of '{context}.{value.Name}' does not fit {definition.Underlying.ToKeyword()}");
        }
    }

    private static SerializationException Error(Schema schema, string message)
    {
        return new SerializationException($"Invalid schema '{schema.Name}': {message}");
    }
}
