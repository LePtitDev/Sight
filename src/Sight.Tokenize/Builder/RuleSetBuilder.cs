using System;
using System.Collections.Generic;

namespace Sight.Tokenize.Builder;

/// <summary>
/// Describe how a token is emitted by a rule
/// </summary>
internal enum EntryType
{
    Token,
    Trivia,
    Error
}

/// <summary>
/// Rule of a mode with its action
/// </summary>
internal sealed class RuleEntry<TKind>
    where TKind : struct, Enum
{
    public RuleEntry(Rule rule, TKind kind, EntryType type)
    {
        Rule = rule;
        Kind = kind;
        Type = type;
    }

    public Rule Rule { get; }

    public TKind Kind { get; }

    public EntryType Type { get; }

    public string? Code { get; set; }

    public string? Message { get; set; }

    public string? PushMode { get; set; }

    public bool Pop { get; set; }
}

internal sealed class KeywordGroup<TKind>
    where TKind : struct, Enum
{
    public KeywordGroup(TKind kind, bool ignoreCase, string[] words)
    {
        Kind = kind;
        IgnoreCase = ignoreCase;
        Words = words;
    }

    public TKind Kind { get; }

    public bool IgnoreCase { get; }

    public string[] Words { get; }
}

/// <summary>
/// Rules of a mode
/// </summary>
internal sealed class ModeDefinition<TKind>
    where TKind : struct, Enum
{
    public ModeDefinition(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public List<RuleEntry<TKind>> Entries { get; } = new List<RuleEntry<TKind>>();

    public List<KeywordGroup<TKind>> Keywords { get; } = new List<KeywordGroup<TKind>>();
}

/// <summary>
/// Base of the builders that declare the rules of a mode
/// </summary>
/// <typeparam name="TKind">Enumeration of token kinds</typeparam>
/// <typeparam name="TSelf">Type of the builder (to chain the calls)</typeparam>
public abstract class RuleSetBuilder<TKind, TSelf>
    where TKind : struct, Enum
    where TSelf : RuleSetBuilder<TKind, TSelf>
{
    private protected RuleSetBuilder(ModeDefinition<TKind> definition)
    {
        Definition = definition;
    }

    internal ModeDefinition<TKind> Definition { get; }

    /// <summary>
    /// Declare a trivia (whitespace, comment...): it is emitted only with <see cref="LexerOptions.EmitTrivia"/>
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public TSelf Trivia(TKind kind, Rule rule) => Add(new RuleEntry<TKind>(NotNull(rule), kind, EntryType.Trivia));

    /// <summary>
    /// Declare a token
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public TSelf Token(TKind kind, Rule rule) => Add(new RuleEntry<TKind>(NotNull(rule), kind, EntryType.Token));

    /// <summary>
    /// Declare a token that activates a mode (the previous mode is restored by a <see cref="Pop"/>)
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public TSelf Push(TKind kind, Rule rule, string mode)
    {
        if (string.IsNullOrEmpty(mode))
            throw new ArgumentException("Mode must be non empty", nameof(mode));

        return Add(new RuleEntry<TKind>(NotNull(rule), kind, EntryType.Token) { PushMode = mode });
    }

    /// <summary>
    /// Declare a token that restores the previous mode (ignored when there is no previous mode)
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public TSelf Pop(TKind kind, Rule rule) => Add(new RuleEntry<TKind>(NotNull(rule), kind, EntryType.Token) { Pop = true });

    /// <summary>
    /// Declare an invalid lexeme: the matched text is emitted as an error token with a diagnostic
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public TSelf Error(Rule rule, string code, string message)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code must be non empty", nameof(code));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return Add(new RuleEntry<TKind>(NotNull(rule), default, EntryType.Error) { Code = code, Message = message });
    }

    /// <summary>
    /// Declare keywords (case sensitive): when a rule based on <see cref="Rule.Identifier"/> matches one of them,
    /// the kind of the token is replaced by <paramref name="kind"/>
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public TSelf Keywords(TKind kind, params string[] words) => Keywords(kind, false, words);

    /// <inheritdoc cref="Keywords(TKind,string[])"/>
    public TSelf Keywords(TKind kind, bool ignoreCase, params string[] words)
    {
        if (words == null || words.Length == 0)
            throw new ArgumentException("At least one keyword is required", nameof(words));

        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
                throw new ArgumentException("Keywords must be non empty", nameof(words));
        }

        Definition.Keywords.Add(new KeywordGroup<TKind>(kind, ignoreCase, (string[])words.Clone()));
        return (TSelf)this;
    }

    private TSelf Add(RuleEntry<TKind> entry)
    {
        Definition.Entries.Add(entry);
        return (TSelf)this;
    }

    private static Rule NotNull(Rule rule) => rule ?? throw new ArgumentNullException(nameof(rule));
}
