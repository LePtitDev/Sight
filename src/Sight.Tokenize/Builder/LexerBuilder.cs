using System;
using System.Collections.Generic;

namespace Sight.Tokenize.Builder;

/// <summary>
/// Declare the rules of a mode (see <see cref="LexerBuilder{TKind}.Mode"/>)
/// </summary>
/// <typeparam name="TKind">Enumeration of token kinds</typeparam>
public sealed class ModeBuilder<TKind> : RuleSetBuilder<TKind, ModeBuilder<TKind>>
    where TKind : struct, Enum
{
    internal ModeBuilder(ModeDefinition<TKind> definition)
        : base(definition)
    {
    }
}

/// <summary>
/// Declare the rules of a lexer. The rules declared on the builder itself are the rules of the initial mode.
/// </summary>
/// <typeparam name="TKind">Enumeration of token kinds</typeparam>
public sealed class LexerBuilder<TKind> : RuleSetBuilder<TKind, LexerBuilder<TKind>>
    where TKind : struct, Enum
{
    private readonly List<ModeDefinition<TKind>> _modes = new List<ModeDefinition<TKind>>();
    private TKind _errorKind;

    /// <summary>
    /// Initialize a new instance of the class <see cref="LexerBuilder{TKind}"/>
    /// </summary>
    public LexerBuilder()
        : base(new ModeDefinition<TKind>(string.Empty))
    {
    }

    /// <summary>
    /// Set the kind of the error tokens (default: the default value of <typeparamref name="TKind"/>, use
    /// <see cref="Token{TKind}.IsError"/> to detect an error)
    /// </summary>
    public LexerBuilder<TKind> ErrorKind(TKind kind)
    {
        _errorKind = kind;
        return this;
    }

    /// <summary>
    /// Declare a mode (a set of rules activated by <see cref="RuleSetBuilder{TKind,TSelf}.Push"/> and left by <see cref="RuleSetBuilder{TKind,TSelf}.Pop"/>)
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public LexerBuilder<TKind> Mode(string name, Action<ModeBuilder<TKind>> configure)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Mode name must be non empty", nameof(name));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        if (_modes.Exists(x => string.Equals(x.Name, name, StringComparison.Ordinal)))
            throw new ArgumentException($"Mode '{name}' already defined", nameof(name));

        var definition = new ModeDefinition<TKind>(name);
        configure(new ModeBuilder<TKind>(definition));
        _modes.Add(definition);
        return this;
    }

    /// <summary>
    /// Validate the configuration and create an immutable and thread safe lexer
    /// </summary>
    /// <exception cref="InvalidOperationException">The configuration is invalid (unknown mode, mode without rule, keywords without identifier rule...)</exception>
    public Lexer<TKind> Build(LexerOptions? options = null)
    {
        var modes = new List<ModeDefinition<TKind>> { Definition };
        modes.AddRange(_modes);
        return new Lexer<TKind>(modes, _errorKind, options ?? new LexerOptions());
    }
}
