using System.Collections.Generic;

namespace Sight.Serialization.Text;

/// <summary>
/// Value syntax node, parsed without schema (text specification §6.1)
/// </summary>
internal abstract class SyntaxNode
{
    protected SyntaxNode(Token token)
    {
        Token = token;
    }

    public Token Token { get; }
}

/// <summary>
/// Number, string or Base64 literal
/// </summary>
internal sealed class LiteralNode : SyntaxNode
{
    public LiteralNode(Token token)
        : base(token)
    {
    }
}

/// <summary>
/// Identifier or qualified name: keywords (null, true, false, inf, nan) and enum values
/// </summary>
internal sealed class NameNode : SyntaxNode
{
    public NameNode(Token token, string name)
        : base(token)
    {
        Name = name;
    }

    public string Name { get; }
}

internal sealed class ListNode : SyntaxNode
{
    public ListNode(Token token)
        : base(token)
    {
    }

    public List<SyntaxNode> Items { get; } = new();
}

/// <summary>
/// <c>[Type] { key: value, ... }</c>: struct or map
/// </summary>
internal sealed class BracedNode : SyntaxNode
{
    public BracedNode(Token token, string? typeName)
        : base(token)
    {
        TypeName = typeName;
    }

    public string? TypeName { get; }

    public List<KeyValuePair<Token, SyntaxNode>> Entries { get; } = new();
}

internal sealed class AnchorNode : SyntaxNode
{
    public AnchorNode(Token token, string name, BracedNode value)
        : base(token)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public BracedNode Value { get; }
}

internal sealed class ReferenceNode : SyntaxNode
{
    public ReferenceNode(Token token, string name)
        : base(token)
    {
        Name = name;
    }

    public string Name { get; }
}

internal sealed class ExtRefNode : SyntaxNode
{
    public ExtRefNode(Token token, string alias, string key)
        : base(token)
    {
        Alias = alias;
        Key = key;
    }

    public string Alias { get; }

    public string Key { get; }
}

/// <summary>
/// Value with an explicit type, in <c>any</c> positions
/// </summary>
internal sealed class TypedNode : SyntaxNode
{
    public TypedNode(Token token, TypeSyntax type, SyntaxNode value)
        : base(token)
    {
        Type = type;
        Value = value;
    }

    public TypeSyntax Type { get; }

    public SyntaxNode Value { get; }
}

/// <summary>
/// Parser of value syntax
/// </summary>
internal static class ValueParser
{
    public static SyntaxNode ParseValue(TokenReader reader)
    {
        var token = reader.Current;
        switch (token.Kind)
        {
            case TokenKind.Number:
            case TokenKind.String:
            case TokenKind.Base64:
                reader.Next();
                return new LiteralNode(token);
            case TokenKind.Identifier:
                if (token.Text is "list" or "map" or "ref" or "extref" && reader.PeekAt(1).Is('<'))
                {
                    var type = SchemaParser.ParseTypeSyntax(reader);
                    return new TypedNode(token, type, ParseValue(reader));
                }

                var name = reader.ExpectQualifiedName();
                if (reader.Current.Is('{') && reader.Current.Line == token.Line)
                    return ParseBraced(reader, token, name);

                return new NameNode(token, name);
            case TokenKind.Punctuation:
                switch (token.Text[0])
                {
                    case '[':
                        return ParseList(reader);
                    case '{':
                        return ParseBraced(reader, token, null);
                    case '&':
                        reader.Next();
                        var anchor = ParseAnchorName(reader);
                        var valueToken = reader.Current;
                        string? typeName = null;
                        if (valueToken.Kind == TokenKind.Identifier)
                        {
                            typeName = reader.ExpectQualifiedName();
                            if (!reader.Current.Is('{') || reader.Current.Line != valueToken.Line)
                                throw reader.Error("Expected '{' after the type prefix, on the same line");
                        }

                        if (!reader.Current.Is('{'))
                            throw reader.Error("An anchor must be followed by a struct value");

                        return new AnchorNode(token, anchor, ParseBraced(reader, valueToken, typeName));
                    case '*':
                        reader.Next();
                        return new ReferenceNode(token, ParseAnchorName(reader));
                    case '<':
                        reader.Next();
                        var alias = reader.ExpectIdentifier().Text;
                        var key = string.Empty;
                        if (reader.TryConsume('#'))
                        {
                            if (reader.Current.Kind == TokenKind.String)
                                key = reader.Next().Value!;
                            else
                                key = reader.ExpectIdentifier().Text;

                            if (key.Length == 0)
                                throw TokenReader.Error(token, "Export name cannot be empty");
                        }

                        reader.Expect('>');
                        return new ExtRefNode(token, alias, key);
                }

                break;
        }

        throw reader.Error($"Expected a value, found {token}");
    }

    private static string ParseAnchorName(TokenReader reader)
    {
        var token = reader.Current;
        if (token.Kind == TokenKind.Identifier)
            return reader.Next().Text;

        if (token.Kind == TokenKind.Number)
        {
            reader.Next();
            foreach (var c in token.Text)
            {
                if (c is < '0' or > '9')
                    throw TokenReader.Error(token, $"Invalid anchor name '{token.Text}'");
            }

            return token.Text;
        }

        throw reader.Error($"Expected an anchor name, found {token}");
    }

    private static ListNode ParseList(TokenReader reader)
    {
        var node = new ListNode(reader.Expect('['));
        reader.ParseItems(']', () => node.Items.Add(ParseValue(reader)));
        return node;
    }

    private static BracedNode ParseBraced(TokenReader reader, Token token, string? typeName)
    {
        reader.Expect('{');
        var node = new BracedNode(token, typeName);
        reader.ParseItems('}', () =>
        {
            var key = reader.Current;
            if (key.Kind is not TokenKind.Identifier and not TokenKind.String and not TokenKind.Number)
                throw reader.Error($"Expected a field name or a key, found {key}");

            reader.Next();
            reader.Expect(':');
            node.Entries.Add(new KeyValuePair<Token, SyntaxNode>(key, ParseValue(reader)));
        });

        return node;
    }
}
