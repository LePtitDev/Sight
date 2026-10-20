using System;
using System.Collections.Generic;
using System.Linq;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Text;

/// <summary>
/// Types a value syntax tree with a schema (text specification §6 and §7)
/// </summary>
internal sealed class TextBinder
{
    private readonly TypeTable _table;
    private readonly IReadOnlyDictionary<string, string> _dependencies;
    private readonly SerializationLimits _limits;
    private readonly Dictionary<string, DataStruct> _anchors = new(StringComparer.Ordinal);
    private readonly List<PendingReference> _pending = new();
    private int _depth;

    public TextBinder(TypeTable table, IReadOnlyDictionary<string, string> dependencies, SerializationLimits limits)
    {
        _table = table;
        _dependencies = dependencies;
        _limits = limits;
    }

    /// <summary>
    /// Bind a struct value whose type prefix is mandatory (root, exports)
    /// </summary>
    public DataStruct BindTopLevel(SyntaxNode node)
    {
        var (braced, anchor) = node switch
        {
            BracedNode b => (b, (string?)null),
            AnchorNode a => (a.Value, a.Name),
            _ => throw Error(node, "Expected a struct value with a type prefix"),
        };

        if (braced.TypeName == null)
            throw Error(node, "A type prefix is mandatory here");

        var type = ResolveStruct(braced.TypeName, node);
        if (type.IsAbstract)
            throw Error(node, $"Cannot instantiate abstract struct '{type.QualifiedName}'");

        var value = new DataStruct(type);
        if (anchor != null)
            RegisterAnchor(anchor, value, node);

        BindFields(braced, value);
        return value;
    }

    /// <summary>
    /// Resolve a reference to an anchor (exports)
    /// </summary>
    public void BindExportReference(ReferenceNode node, Action<DataStruct> assign)
    {
        var pending = new PendingReference(node, null);
        pending.Fixups.Add(assign);
        _pending.Add(pending);
    }

    /// <summary>
    /// Resolve forward references once the whole document is bound
    /// </summary>
    public void ResolvePending()
    {
        foreach (var pending in _pending)
        {
            if (!_anchors.TryGetValue(pending.Node.Name, out var target))
                throw Error(pending.Node, $"Unresolved reference '*{pending.Node.Name}'");

            if (pending.Expected != null && !target.Definition.IsAssignableTo(pending.Expected))
                throw Error(pending.Node, $"Referenced object of type '{target.Definition.QualifiedName}' is not a '{pending.Expected.QualifiedName}'");

            foreach (var fixup in pending.Fixups)
                fixup(target);
        }

        _pending.Clear();
    }

    private DataValue Bind(SyntaxNode node, TypeReference type)
    {
        if (node is NameNode { Name: "null" })
        {
            return type.IsNullable || type.Kind == TypeKind.Any
                ? DataValue.Null
                : throw Error(node, $"null is not allowed for type {type}");
        }

        switch (type.Kind)
        {
            case TypeKind.Bool:
            case TypeKind.U8:
            case TypeKind.U16:
            case TypeKind.U32:
            case TypeKind.U64:
            case TypeKind.S8:
            case TypeKind.S16:
            case TypeKind.S32:
            case TypeKind.S64:
            case TypeKind.F32:
            case TypeKind.F64:
            case TypeKind.String:
                if (node is LiteralNode { Token.Kind: not TokenKind.Base64 } || (node is NameNode name && !name.Name.Contains(".")))
                    return Literals.ToScalar(node.Token, type.AsNonNullable());

                throw Error(node, $"Expected a value of type {type}");
            case TypeKind.Enum:
                if (node is NameNode enumName)
                {
                    var value = SchemaParser.FindEnumValue(type.EnumDefinition!, enumName.Name)
                        ?? throw Error(node, $"Enum '{type.EnumDefinition!.QualifiedName}' has no value '{enumName.Name}'");
                    return new DataEnum(value);
                }

                throw Error(node, $"Expected a value of enum '{type.EnumDefinition!.QualifiedName}'");
            case TypeKind.List:
                return BindList(node, type.ElementType!);
            case TypeKind.Map:
                return BindMap(node, type.KeyType!, type.ValueType!);
            case TypeKind.Struct:
                if (node is AnchorNode)
                    throw Error(node, "An anchor is only allowed at a reference position");

                return BindStruct(node as BracedNode ?? throw Error(node, $"Expected a struct value of type '{type.StructDefinition!.QualifiedName}'"), type.StructDefinition!);
            case TypeKind.Ref:
                return BindRef(node, type.StructDefinition!);
            case TypeKind.ExtRef:
                return BindExtRef(node, type.StructDefinition!);
            case TypeKind.Any:
                return BindAny(node);
            default:
                throw Error(node, $"Unsupported type {type}");
        }
    }

    private DataList BindList(SyntaxNode node, TypeReference elementType)
    {
        if (node is LiteralNode { Token.Kind: TokenKind.Base64 } base64)
        {
            if (elementType.Kind != TypeKind.U8 || elementType.IsNullable)
                throw Error(node, "Base64 literals are only valid for list<u8>");

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64.Token.Value!);
            }
            catch (FormatException)
            {
                throw Error(node, "Invalid Base64 literal");
            }

            if (base64.Token.Value!.Length % 4 != 0)
                throw Error(node, "Invalid Base64 literal (padding is mandatory)");

            return new DataList(bytes.Select(x => (DataValue)x), elementType);
        }

        if (node is not ListNode listNode)
            throw Error(node, $"Expected a list of {elementType}");

        Enter(node);
        var list = new DataList(elementType);
        for (var i = 0; i < listNode.Items.Count; i++)
        {
            var index = i;
            list.Add(Track(Bind(listNode.Items[i], elementType), x => list[index] = x));
        }

        _depth--;
        return list;
    }

    private DataMap BindMap(SyntaxNode node, TypeReference keyType, TypeReference valueType)
    {
        if (node is not BracedNode braced || braced.TypeName != null)
            throw Error(node, $"Expected a map of {keyType} to {valueType}");

        Enter(node);
        var map = new DataMap(keyType, valueType);
        foreach (var entry in braced.Entries)
        {
            var key = BindKey(entry.Key, keyType);
            if (map.ContainsKey(key))
                throw TokenReader.Error(entry.Key, $"Duplicate map key {entry.Key}");

            map.Add(key, Track(Bind(entry.Value, valueType), x => map[key] = x));
        }

        _depth--;
        return map;
    }

    private static DataValue BindKey(Token key, TypeReference keyType)
    {
        switch (keyType.Kind)
        {
            case TypeKind.String:
                if (key.Kind == TokenKind.String)
                    return DataScalar.FromString(key.Value!);

                break;
            case TypeKind.Bool:
                if (key.IsIdentifier("true") || key.IsIdentifier("false"))
                    return DataScalar.FromBoolean(key.Text == "true");

                break;
            case TypeKind.Enum:
                if (key.Kind == TokenKind.Identifier)
                {
                    return new DataEnum(keyType.EnumDefinition!.FindValue(key.Text)
                        ?? throw TokenReader.Error(key, $"Enum '{keyType.EnumDefinition!.QualifiedName}' has no value '{key.Text}'"));
                }

                break;
            default:
                if (keyType.Kind.IsInteger() && key.Kind == TokenKind.Number)
                    return Literals.ToScalar(key, keyType);

                break;
        }

        throw TokenReader.Error(key, $"Invalid key {key} for type {keyType}");
    }

    private DataStruct BindStruct(BracedNode node, StructDefinition declared)
    {
        var type = declared;
        if (node.TypeName != null)
        {
            type = ResolveStruct(node.TypeName, node);
            if (!type.IsAssignableTo(declared))
                throw Error(node, $"Type '{type.QualifiedName}' is not a '{declared.QualifiedName}'");
        }

        if (type.IsAbstract)
            throw Error(node, $"A type prefix is mandatory: '{type.QualifiedName}' is abstract");

        var value = new DataStruct(type);
        BindFields(node, value);
        return value;
    }

    private void BindFields(BracedNode node, DataStruct value)
    {
        Enter(node);
        var definition = value.Definition;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in node.Entries)
        {
            var key = entry.Key;
            if (key.Kind != TokenKind.Identifier)
                throw TokenReader.Error(key, $"Expected a field name, found {key}");

            var field = definition.FindField(key.Text);
            if (field == null)
            {
                var suggestion = Suggest(definition, key.Text);
                throw TokenReader.Error(key, $"Unknown field '{key.Text}' in '{definition.QualifiedName}'" + (suggestion != null ? $" (did you mean '{suggestion}'?)" : string.Empty));
            }

            if (!seen.Add(field.Name))
                throw TokenReader.Error(key, $"Duplicate field '{field.Name}'");

            var name = field.Name;
            value[name] = Track(Bind(entry.Value, field.Type), x => value[name] = x);
        }

        foreach (var field in definition.AllFields)
        {
            if (seen.Contains(field.Name))
                continue;

            if (field.Type.IsNullable)
                value[field.Name] = DataValue.Null;
            else if (field.DefaultValue != null)
                value[field.Name] = field.DefaultValue;
            else
                throw Error(node, $"Missing required field '{field.Name}' in '{definition.QualifiedName}'");
        }

        _depth--;
    }

    private DataValue BindRef(SyntaxNode node, StructDefinition declared)
    {
        switch (node)
        {
            case ReferenceNode reference:
                var pending = new PendingReference(reference, declared);
                _pending.Add(pending);
                return pending;
            case AnchorNode anchor:
                var anchored = BindStruct(anchor.Value, declared);
                RegisterAnchor(anchor.Name, anchored, node);
                return anchored;
            case BracedNode braced:
                return BindStruct(braced, declared);
            default:
                throw Error(node, $"Expected a reference or a struct value of type '{declared.QualifiedName}'");
        }
    }

    private DataExtRef BindExtRef(SyntaxNode node, StructDefinition declared)
    {
        if (node is not ExtRefNode extRef)
            throw Error(node, $"Expected an external reference to '{declared.QualifiedName}'");

        if (!_dependencies.TryGetValue(extRef.Alias, out var id))
            throw Error(node, $"Undeclared dependency alias '{extRef.Alias}'");

        return new DataExtRef(id, extRef.Key, declared);
    }

    private DataValue BindAny(SyntaxNode node)
    {
        switch (node)
        {
            case NameNode name:
                switch (name.Name)
                {
                    case "true":
                    case "false":
                        return DataScalar.FromBoolean(name.Name == "true");
                    case "inf":
                    case "nan":
                    case "inff32":
                    case "nanf32":
                    case "inff64":
                    case "nanf64":
                        return Literals.ToAnyNumber(node.Token);
                }

                var separator = name.Name.LastIndexOf('.');
                if (separator < 0)
                    throw Error(node, $"Enum value '{name.Name}' must be qualified with its type in 'any'");

                var enumName = name.Name.Substring(0, separator);
                var definition = ResolveType(enumName, node) as EnumDefinition ?? throw Error(node, $"'{enumName}' is not an enum");
                return new DataEnum(definition.FindValue(name.Name.Substring(separator + 1)) ?? throw Error(node, $"Enum '{definition.QualifiedName}' has no value '{name.Name.Substring(separator + 1)}'"));
            case LiteralNode literal:
                return literal.Token.Kind switch
                {
                    TokenKind.Number => Literals.ToAnyNumber(literal.Token),
                    TokenKind.String => DataScalar.FromString(literal.Token.Value!),
                    _ => BindList(node, TypeReference.U8),
                };
            case ListNode:
                return BindList(node, TypeReference.Any);
            case BracedNode braced:
                if (braced.TypeName == null)
                    return BindMap(node, TypeReference.String, TypeReference.Any);

                var type = ResolveStruct(braced.TypeName, node);
                if (type.IsAbstract)
                    throw Error(node, $"Cannot instantiate abstract struct '{type.QualifiedName}'");

                var value = new DataStruct(type);
                BindFields(braced, value);
                return value;
            case AnchorNode anchor:
                if (anchor.Value.TypeName == null)
                    throw Error(node, "A type prefix is mandatory in 'any'");

                return new DataReference((DataStruct)BindRef(node, ResolveStruct(anchor.Value.TypeName, node)));
            case ReferenceNode reference:
                var pending = new PendingReference(reference, null) { WrapInReference = true };
                _pending.Add(pending);
                return pending;
            case TypedNode typed:
                TypeReference explicitType;
                try
                {
                    explicitType = SchemaParser.ResolveTypeSyntax((name, token) => ResolveType(name, new NameNode(token, name)), typed.Type);
                }
                catch (SerializationException e) when (e.Line == 0)
                {
                    throw Error(node, e.Message);
                }

                if (explicitType.IsNullable)
                    throw Error(node, "The type of an 'any' value cannot be nullable");

                var bound = Bind(typed.Value, explicitType);
                return explicitType.Kind == TypeKind.Ref && bound is DataStruct target ? new DataReference(target) : bound;
            default:
                throw Error(node, "This value requires an explicit type in 'any'");
        }
    }

    private DataValue Track(DataValue value, Action<DataValue> fixup)
    {
        if (value is PendingReference pending)
        {
            pending.Fixups.Add(target => fixup(pending.WrapInReference ? new DataReference(target) : target));
        }

        return value;
    }

    private void RegisterAnchor(string name, DataStruct value, SyntaxNode node)
    {
        if (_anchors.ContainsKey(name))
            throw Error(node, $"Duplicate anchor '&{name}'");

        _anchors.Add(name, value);
    }

    private StructDefinition ResolveStruct(string name, SyntaxNode node)
    {
        return ResolveType(name, node) as StructDefinition ?? throw Error(node, $"'{name}' is not a struct");
    }

    private TypeDefinition ResolveType(string name, SyntaxNode node)
    {
        try
        {
            return _table.FindByName(name) ?? throw Error(node, $"Unknown type '{name}'");
        }
        catch (SerializationException e) when (e.Line == 0)
        {
            throw Error(node, e.Message);
        }
    }

    private static string? Suggest(StructDefinition definition, string name)
    {
        string? best = null;
        var bestDistance = int.MaxValue;
        foreach (var field in definition.AllFields)
        {
            var distance = Distance(field.Name.ToLowerInvariant(), name.ToLowerInvariant());
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = field.Name;
            }
        }

        return bestDistance <= Math.Max(2, name.Length / 3) ? best : null;
    }

    private static int Distance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    private void Enter(SyntaxNode node)
    {
        if (++_depth > _limits.MaxDepth)
            throw Error(node, $"Nesting depth exceeds the limit of {_limits.MaxDepth}");
    }

    private static SerializationException Error(SyntaxNode node, string message) => TokenReader.Error(node.Token, message);

    /// <summary>
    /// Placeholder of a reference resolved after binding
    /// </summary>
    private sealed class PendingReference : DataValue
    {
        public PendingReference(ReferenceNode node, StructDefinition? expected)
        {
            Node = node;
            Expected = expected;
        }

        public ReferenceNode Node { get; }

        public StructDefinition? Expected { get; }

        public bool WrapInReference { get; set; }

        public List<Action<DataStruct>> Fixups { get; } = new();

        public override TypeKind Kind => TypeKind.Ref;
    }
}
