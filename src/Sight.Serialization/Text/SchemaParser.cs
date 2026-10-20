using System;
using System.Collections.Generic;
using System.Linq;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Text;

/// <summary>
/// Parser of the schema language (text specification §4)
/// </summary>
internal sealed class SchemaParser
{
    public const uint FormatVersion = 1;

    private readonly TokenReader _reader;
    private readonly Func<string, uint, ulong?, Schema> _resolveImport;
    private Dictionary<string, Schema> _aliases = new(StringComparer.Ordinal);

    private SchemaParser(TokenReader reader, Func<string, uint, ulong?, Schema> resolveImport)
    {
        _reader = reader;
        _resolveImport = resolveImport;
    }

    public static Schema Parse(string text, Func<string, uint, ulong?, Schema> resolveImport)
    {
        var reader = new TokenReader(Lexer.Tokenize(text));
        var schema = Parse(reader, resolveImport, isFile: true);
        if (!reader.IsAtEnd)
            throw reader.Error($"Unexpected {reader.Current}");

        return schema;
    }

    /// <summary>
    /// Parse a schema from a token reader: a whole <c>.sgs</c> file (starting with <c>@sgs</c>), or the content of an inline block
    /// </summary>
    public static Schema Parse(TokenReader reader, Func<string, uint, ulong?, Schema> resolveImport, bool isFile)
    {
        return new SchemaParser(reader, resolveImport).ParseSchema(isFile);
    }

    /// <summary>
    /// Read only the identity (<c>@id</c> and <c>@version</c>) of a schema source
    /// </summary>
    public static (string Name, uint Version) ReadIdentity(string text)
    {
        var reader = new TokenReader(Lexer.Tokenize(text));
        var header = new SchemaParser(reader, (_, _, _) => throw new InvalidOperationException()).ParseHeader(isFile: true, resolveImports: false);
        return (header.Name, header.Version);
    }

    /// <summary>
    /// Read the identity of an inline schema block
    /// </summary>
    public static string ReadBlockName(IReadOnlyList<Token> tokens)
    {
        var reader = new TokenReader(tokens);
        return new SchemaParser(reader, (_, _, _) => throw new InvalidOperationException()).ParseHeader(isFile: false, resolveImports: false).Name;
    }

    private Header ParseHeader(bool isFile, bool resolveImports)
    {
        if (isFile)
        {
            var first = _reader.Current;
            if (first.Kind != TokenKind.Directive || first.Text != "sgs" || first.Line != 1 || first.Column != 1)
                throw TokenReader.Error(first, "A schema source must start with '@sgs'");

            _reader.Next();
            var format = _reader.ExpectUInt32();
            if (format != FormatVersion)
                throw TokenReader.Error(first, $"Unsupported schema language version {format}");

            ExpectEndOfDirective();
        }

        var header = new Header();
        Token? idToken = null;
        var hasVersion = false;
        while (_reader.Current.Kind == TokenKind.Directive)
        {
            var directive = _reader.Current;
            if (!directive.AfterNewLine)
                throw _reader.Error("A directive must start at the beginning of a line");

            _reader.Next();
            switch (directive.Text)
            {
                case "id":
                    if (idToken != null)
                        throw TokenReader.Error(directive, "Duplicate @id directive");

                    idToken = _reader.Current;
                    header.Name = _reader.Current.Kind == TokenKind.String ? _reader.Next().Value! : _reader.ExpectQualifiedName();
                    if (!Names.IsQualifiedName(header.Name))
                        throw TokenReader.Error(idToken.Value, $"Invalid schema name '{header.Name}'");

                    break;
                case "version":
                    if (hasVersion)
                        throw TokenReader.Error(directive, "Duplicate @version directive");

                    hasVersion = true;
                    header.Version = _reader.ExpectUInt32();
                    break;
                case "meta":
                    var key = _reader.Current.Kind == TokenKind.String ? _reader.Next().Value! : _reader.ExpectIdentifier().Text;
                    _reader.Expect('=');
                    if (_reader.Current.Kind != TokenKind.String)
                        throw _reader.Error($"Expected a string, found {_reader.Current}");

                    if (header.Metadata.TryGetValue(key, out _))
                        throw TokenReader.Error(directive, $"Duplicate metadata key '{key}'");

                    header.Metadata.Add(key, _reader.Next().Value!);
                    break;
                case "import":
                    ParseImport(header, directive, resolveImports);
                    break;
                default:
                    throw TokenReader.Error(directive, $"Unknown schema directive '@{directive.Text}'");
            }

            ExpectEndOfDirective();
        }

        if (idToken == null)
            throw _reader.Error("Missing @id directive");

        foreach (var alias in header.Aliases.Keys)
        {
            if (alias == header.Name || header.ImportNames.Any(x => x.Split('.')[0] == alias))
                throw _reader.Error($"Import alias '{alias}' conflicts with a schema name");
        }

        return header;
    }

    private void ParseImport(Header header, Token directive, bool resolveImports)
    {
        string? alias = null;
        if (_reader.Current.Kind == TokenKind.Identifier && _reader.PeekAt(1).Is('='))
        {
            alias = _reader.Next().Text;
            _reader.Next();
            if (!Names.IsIdentifier(alias) || header.Aliases.ContainsKey(alias))
                throw TokenReader.Error(directive, $"Invalid or duplicate import alias '{alias}'");
        }

        var name = _reader.ExpectQualifiedName();
        var version = _reader.ExpectUInt32();
        ulong? hash = null;
        if (_reader.Current.Kind == TokenKind.Number && !_reader.Current.AfterNewLine)
        {
            var hashToken = _reader.Next();
            var number = NumberLiteral.Parse(hashToken);
            if (!hashToken.Text.StartsWith("0x", StringComparison.Ordinal) || number.Suffix.HasValue)
                throw TokenReader.Error(hashToken, "Expected a hexadecimal hash");

            hash = number.Magnitude;
        }

        if (header.ImportNames.Contains(name))
            throw TokenReader.Error(directive, $"Duplicate import '{name}'");

        header.ImportNames.Add(name);
        if (!resolveImports)
            return;

        Schema import;
        try
        {
            import = _resolveImport(name, version, hash);
        }
        catch (SerializationException e) when (e.Line == 0)
        {
            throw new SerializationException(e.Message, directive.Line, directive.Column);
        }

        header.Imports.Add(import);
        if (alias != null)
            header.Aliases.Add(alias, import);
    }

    private void ExpectEndOfDirective()
    {
        if (!_reader.Current.AfterNewLine && !_reader.IsAtEnd)
            throw _reader.Error("Expected a line break after the directive");
    }

    private Schema ParseSchema(bool isFile)
    {
        var header = ParseHeader(isFile, resolveImports: true);
        var schema = new Schema(header.Name, header.Version, header.Imports);
        foreach (var entry in header.Metadata)
            schema.Metadata.Add(entry.Key, entry.Value);

        _aliases = header.Aliases;

        // First pass: declarations (and enum values), second pass: alias targets, third pass: bases and fields
        var structs = new List<(StructDefinition Definition, Token? BaseToken, string? BaseName, List<FieldSyntax> Fields)>();
        var aliases = new Dictionary<AliasDefinition, TypeSyntax>();
        while (!_reader.IsAtEnd)
        {
            if (_reader.Current.IsIdentifier("enum"))
                ParseEnum(schema);
            else if (_reader.Current.IsIdentifier("struct") || _reader.Current.IsIdentifier("abstract"))
                structs.Add(ParseStruct(schema));
            else if (_reader.Current.IsIdentifier("type"))
                ParseAlias(schema, aliases);
            else
                throw _reader.Error($"Expected 'enum', 'struct' or 'type', found {_reader.Current}");
        }

        // Aliases are resolved on demand: an alias may refer to another one declared later
        var resolving = new HashSet<AliasDefinition>();
        TypeDefinition Resolve(string name, Token token)
        {
            var definition = ResolveType(schema, name, token);
            if (definition is AliasDefinition alias && !alias.IsResolved && aliases.TryGetValue(alias, out var target))
            {
                if (!resolving.Add(alias))
                    throw TokenReader.Error(token, $"Cyclic type alias '{alias.Name}'");

                var type = ResolveTypeSyntax(Resolve, target);
                if (type.IsNullable)
                    throw TokenReader.Error(target.Token, "The target of a type alias cannot be nullable");

                alias.Resolve(type);
            }

            return definition;
        }

        foreach (var alias in aliases.Keys)
            Resolve(alias.Name, aliases[alias].Token);

        foreach (var (definition, baseToken, baseName, fields) in structs)
        {
            if (baseName != null)
            {
                definition.Base = Resolve(baseName, baseToken!.Value) as StructDefinition
                    ?? throw TokenReader.Error(baseToken.Value, $"Base '{baseName}' is not a struct");
            }

            foreach (var field in fields)
            {
                var type = ResolveTypeSyntax(Resolve, field.Type);
                if (field.IsFixed)
                {
                    if (!type.Kind.AllowsFixed())
                        throw TokenReader.Error(field.Token, $"@fixed is not allowed on {type}");

                    type = type.AsFixed();
                }

                DataValue? defaultValue = null;
                if (field.Default.HasValue)
                {
                    if (type.IsNullable)
                        throw TokenReader.Error(field.Default.Value, "A nullable field cannot have a default value");

                    defaultValue = ParseDefault(schema, field, type);
                }

                try
                {
                    definition.AddField(field.Name, type, defaultValue, field.Aliases);
                }
                catch (ArgumentException e)
                {
                    throw TokenReader.Error(field.Token, e.Message);
                }
            }
        }

        try
        {
            schema.Validate();
        }
        catch (SerializationException e) when (e.Line == 0)
        {
            throw new SerializationException(e.Message, _reader.Current.Line, _reader.Current.Column);
        }

        return schema;
    }

    private void ParseEnum(Schema schema)
    {
        _reader.ExpectIdentifier("enum");
        var nameToken = _reader.ExpectIdentifier();
        _reader.Expect(':');
        var underlyingToken = _reader.ExpectIdentifier();
        var underlying = ParsePrimitive(underlyingToken.Text);
        if (underlying == null || !underlying.Value.IsInteger())
            throw TokenReader.Error(underlyingToken, $"Invalid underlying type '{underlyingToken.Text}'");

        CheckTypeName(schema, nameToken);
        var definition = schema.AddEnum(nameToken.Text, underlying.Value);
        _reader.Expect('{');
        long? previous = null;
        _reader.ParseItems('}', () =>
        {
            var valueToken = _reader.ExpectIdentifier();
            long value;
            if (_reader.TryConsume('='))
            {
                var numberToken = _reader.Current;
                if (numberToken.Kind != TokenKind.Number)
                    throw _reader.Error($"Expected an integer, found {numberToken}");

                _reader.Next();
                var number = NumberLiteral.Parse(numberToken);
                if (number.IsFloat)
                    throw TokenReader.Error(numberToken, "Enum values must be integers");

                value = Literals.ToInteger(numberToken, number, underlying.Value).AsInt64Bits();
            }
            else
            {
                value = previous.HasValue ? previous.Value + 1 : 0;
            }

            if (!Names.IsIdentifier(valueToken.Text))
                throw TokenReader.Error(valueToken, $"Invalid enum value name '{valueToken.Text}'");

            definition.AddValue(valueToken.Text, value);
            previous = value;
        });
    }

    private (StructDefinition, Token?, string?, List<FieldSyntax>) ParseStruct(Schema schema)
    {
        var isAbstract = false;
        if (_reader.Current.IsIdentifier("abstract"))
        {
            _reader.Next();
            isAbstract = true;
        }

        _reader.ExpectIdentifier("struct");
        var nameToken = _reader.ExpectIdentifier();
        CheckTypeName(schema, nameToken);
        Token? baseToken = null;
        string? baseName = null;
        if (_reader.TryConsume(':'))
        {
            baseToken = _reader.Current;
            baseName = _reader.ExpectQualifiedName();
        }

        var definition = schema.AddStruct(nameToken.Text, null, isAbstract);
        while (_reader.Current.Kind == TokenKind.Directive && !_reader.Current.AfterNewLine)
        {
            var annotation = _reader.Next();
            if (annotation.Text != "positional" || definition.IsPositional)
                throw TokenReader.Error(annotation, $"Invalid or duplicate annotation '@{annotation.Text}'");

            definition.IsPositional = true;
        }

        var fields = new List<FieldSyntax>();
        _reader.Expect('{');
        _reader.ParseItems('}', () => fields.Add(ParseField()));
        return (definition, baseToken, baseName, fields);
    }

    private void ParseAlias(Schema schema, Dictionary<AliasDefinition, TypeSyntax> aliases)
    {
        _reader.ExpectIdentifier("type");
        var nameToken = _reader.ExpectIdentifier();
        CheckTypeName(schema, nameToken);
        _reader.Expect('=');
        var target = ParseTypeSyntax(_reader);
        aliases.Add(schema.AddUnresolvedAlias(nameToken.Text), target);
    }

    private FieldSyntax ParseField()
    {
        var nameToken = _reader.ExpectIdentifier();
        if (!Names.IsIdentifier(nameToken.Text))
            throw TokenReader.Error(nameToken, $"Invalid field name '{nameToken.Text}'");

        _reader.Expect(':');
        var field = new FieldSyntax(nameToken, ParseTypeSyntax(_reader));
        if (_reader.TryConsume('='))
        {
            field.Default = _reader.Current;
            if (_reader.Current.Kind == TokenKind.Identifier)
                field.DefaultName = _reader.ExpectQualifiedName();
            else
                _reader.Next();
        }

        while (_reader.Current.Kind == TokenKind.Directive && !_reader.Current.AfterNewLine)
        {
            var annotation = _reader.Next();
            switch (annotation.Text)
            {
                case "fixed" when !field.IsFixed:
                    field.IsFixed = true;
                    break;
                case "alias" when field.Aliases.Count == 0:
                    _reader.Expect('(');
                    do
                    {
                        var alias = _reader.ExpectIdentifier();
                        if (!Names.IsIdentifier(alias.Text))
                            throw TokenReader.Error(alias, $"Invalid alias '{alias.Text}'");

                        field.Aliases.Add(alias.Text);
                    }
                    while (_reader.TryConsume(','));

                    _reader.Expect(')');
                    break;
                default:
                    throw TokenReader.Error(annotation, $"Invalid or duplicate annotation '@{annotation.Text}'");
            }
        }

        return field;
    }

    /// <summary>
    /// Parse a type expression (text specification §3)
    /// </summary>
    public static TypeSyntax ParseTypeSyntax(TokenReader reader)
    {
        var token = reader.Current;
        var name = reader.ExpectQualifiedName();
        var syntax = new TypeSyntax(token, name);
        switch (name)
        {
            case "list":
                reader.Expect('<');
                syntax.Arguments.Add(ParseTypeSyntax(reader));
                reader.Expect('>');
                break;
            case "map":
                reader.Expect('<');
                syntax.Arguments.Add(ParseTypeSyntax(reader));
                reader.Expect(',');
                syntax.Arguments.Add(ParseTypeSyntax(reader));
                reader.Expect('>');
                break;
            case "ref":
                reader.Expect('<');
                var targetToken = reader.Current;
                syntax.Arguments.Add(new TypeSyntax(targetToken, reader.ExpectQualifiedName()));
                reader.Expect('>');
                break;
        }

        if (reader.Current.Is('?'))
        {
            reader.Next();
            syntax.IsNullable = true;
        }

        return syntax;
    }

    /// <summary>
    /// Resolve a type expression against a schema
    /// </summary>
    public static TypeReference ResolveTypeSyntax(Func<string, Token, TypeDefinition> resolve, TypeSyntax syntax)
    {
        TypeReference type;
        switch (syntax.Name)
        {
            case "list":
                type = TypeReference.List(ResolveTypeSyntax(resolve, syntax.Arguments[0]));
                break;
            case "map":
                type = TypeReference.Map(ResolveTypeSyntax(resolve, syntax.Arguments[0]), ResolveTypeSyntax(resolve, syntax.Arguments[1]));
                break;
            case "ref":
                var target = resolve(syntax.Arguments[0].Name, syntax.Arguments[0].Token) as StructDefinition
                    ?? throw TokenReader.Error(syntax.Arguments[0].Token, "ref target must be a struct");
                type = TypeReference.Ref(target);
                break;
            case "bytes":
                type = TypeReference.Bytes;
                break;
            default:
                var primitive = ParsePrimitive(syntax.Name);
                type = primitive.HasValue ? TypeReference.Primitive(primitive.Value) : TypeReference.Of(resolve(syntax.Name, syntax.Token));
                break;
        }

        if (syntax.IsNullable)
        {
            if (type.Kind == TypeKind.Any)
                throw TokenReader.Error(syntax.Token, "any cannot be nullable");

            type = type.AsNullable();
        }

        return type;
    }

    private DataValue ParseDefault(Schema schema, FieldSyntax field, TypeReference type)
    {
        var token = field.Default!.Value;
        if (type.Kind == TypeKind.Enum)
        {
            var enumDefinition = type.EnumDefinition!;
            var name = field.DefaultName ?? throw TokenReader.Error(token, $"Expected a value of enum '{enumDefinition.QualifiedName}'");
            var value = FindEnumValue(enumDefinition, name) ?? throw TokenReader.Error(token, $"Enum '{enumDefinition.QualifiedName}' has no value '{name}'");
            return new DataEnum(value);
        }

        if (field.DefaultName != null && field.DefaultName.Contains("."))
            throw TokenReader.Error(token, $"Invalid default value '{field.DefaultName}'");

        return Literals.ToScalar(token, type);
    }

    /// <summary>
    /// Find an enum value from a plain or qualified name (<c>Value</c>, <c>Enum.Value</c>, <c>schema.Enum.Value</c>)
    /// </summary>
    public static EnumValueDefinition? FindEnumValue(EnumDefinition definition, string name)
    {
        var separator = name.LastIndexOf('.');
        if (separator < 0)
            return definition.FindValue(name);

        var prefix = name.Substring(0, separator);
        return prefix == definition.Name || prefix == definition.QualifiedName ? definition.FindValue(name.Substring(separator + 1)) : null;
    }

    private TypeDefinition ResolveType(Schema schema, string name, Token token)
    {
        try
        {
            var separator = name.LastIndexOf('.');
            if (separator > 0 && _aliases.TryGetValue(name.Substring(0, separator), out var aliased))
                return aliased.FindOwnType(name.Substring(separator + 1)) ?? throw TokenReader.Error(token, $"Unknown type '{name}'");

            return schema.FindType(name) ?? throw TokenReader.Error(token, $"Unknown type '{name}'");
        }
        catch (SerializationException e) when (e.Line == 0)
        {
            throw TokenReader.Error(token, e.Message);
        }
    }

    private static void CheckTypeName(Schema schema, Token token)
    {
        if (!Names.IsIdentifier(token.Text) || Names.TypeKeywords.Contains(token.Text))
            throw TokenReader.Error(token, $"Invalid type name '{token.Text}'");

        if (schema.FindOwnType(token.Text) != null)
            throw TokenReader.Error(token, $"Duplicate type name '{token.Text}'");
    }

    public static TypeKind? ParsePrimitive(string name) => name switch
    {
        "bool" => TypeKind.Bool,
        "u8" => TypeKind.U8,
        "u16" => TypeKind.U16,
        "u32" => TypeKind.U32,
        "u64" => TypeKind.U64,
        "s8" => TypeKind.S8,
        "s16" => TypeKind.S16,
        "s32" => TypeKind.S32,
        "s64" => TypeKind.S64,
        "f32" => TypeKind.F32,
        "f64" => TypeKind.F64,
        "string" => TypeKind.String,
        "any" => TypeKind.Any,
        _ => null,
    };

    private sealed class Header
    {
        public string Name { get; set; } = string.Empty;

        public uint Version { get; set; }

        public MetadataCollection Metadata { get; } = new();

        public List<Schema> Imports { get; } = new();

        public HashSet<string> ImportNames { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, Schema> Aliases { get; } = new(StringComparer.Ordinal);
    }

    private sealed class FieldSyntax
    {
        public FieldSyntax(Token token, TypeSyntax type)
        {
            Token = token;
            Type = type;
        }

        public Token Token { get; }

        public string Name => Token.Text;

        public TypeSyntax Type { get; }

        public Token? Default { get; set; }

        public string? DefaultName { get; set; }

        public bool IsFixed { get; set; }

        public List<string> Aliases { get; } = new();
    }
}

/// <summary>
/// Unresolved type expression
/// </summary>
internal sealed class TypeSyntax
{
    public TypeSyntax(Token token, string name)
    {
        Token = token;
        Name = name;
    }

    public Token Token { get; }

    public string Name { get; }

    public List<TypeSyntax> Arguments { get; } = new();

    public bool IsNullable { get; set; }
}
