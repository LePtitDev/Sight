using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Sight.Tokenize.Builder;

namespace Sight.Tokenize;

/// <summary>
/// Entry point to create a lexer
/// </summary>
public static class Lexer
{
    /// <summary>
    /// Create a lexer
    /// </summary>
    /// <typeparam name="TKind">Enumeration of token kinds</typeparam>
    /// <param name="configure">Declaration of the rules</param>
    /// <param name="options">Options of the lexer (default options when null)</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException">The configuration is invalid</exception>
    public static Lexer<TKind> Create<TKind>(Action<LexerBuilder<TKind>> configure, LexerOptions? options = null)
        where TKind : struct, Enum
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var builder = new LexerBuilder<TKind>();
        configure(builder);
        return builder.Build(options);
    }
}

/// <summary>
/// Declarative lexer. It is immutable and thread safe, and it never throws on an input: invalid text is reported by
/// error tokens and diagnostics.
/// <para>
/// At each position the rule of the current mode that matches the longest text wins (the first declared in case
/// of equality). If no rule matches, the character is emitted as an error token. Each step consumes at least one character
/// and each character is scanned a bounded number of times, so the tokenization is linear.
/// </para>
/// </summary>
/// <typeparam name="TKind">Enumeration of token kinds</typeparam>
public sealed class Lexer<TKind>
    where TKind : struct, Enum
{
    private const string UnexpectedCharacterCode = "LEX001";
    private const string TokenTooLongCode = "LEX002";
    private const string UnterminatedCode = "LEX003";
    private const string ModeDepthCode = "LEX004";

    private readonly CompiledMode[] _modes;
    private readonly TKind _errorKind;
    private readonly LexerOptions _options;

    internal Lexer(IReadOnlyList<ModeDefinition<TKind>> modes, TKind errorKind, LexerOptions options)
    {
        _errorKind = errorKind;
        _options = options.Clone();
        _modes = Compile(modes);
    }

    /// <summary>
    /// Tokenize a text
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public TokenizeResult<TKind> Tokenize(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        var tokens = new List<Token<TKind>>();
        var diagnostics = new List<Diagnostic>();
        var hasErrors = false;
        var stack = new List<int>();
        var modeIndex = 0;
        var position = 0;
        while (position < text.Length)
        {
            var mode = _modes[modeIndex];
            var c = text[position];

            var best = FindBest(text, position, mode, out var bestLength, out var bestUnterminated);

            if (best == null)
            {
                // No rule: the character is an error (a surrogate pair is a single character)
                var errorLength = char.IsHighSurrogate(c) && position + 1 < text.Length && char.IsLowSurrogate(text[position + 1]) ? 2 : 1;
                tokens.Add(new Token<TKind>(_errorKind, new TextSpan(position, errorLength), true));
                hasErrors = true;
                if (diagnostics.Count < _options.MaxDiagnostics)
                    diagnostics.Add(new Diagnostic(UnexpectedCharacterCode, $"Unexpected character {Describe(text, position, errorLength)}", new TextSpan(position, errorLength)));

                position += errorLength;
                continue;
            }

            var length = bestLength;
            var tooLong = false;
            if (length > _options.MaxTokenLength)
            {
                length = _options.MaxTokenLength;
                tooLong = true;

                // Don't cut a surrogate pair in two parts when it's avoidable
                if (length > 1 && char.IsHighSurrogate(text[position + length - 1]) && char.IsLowSurrogate(text[position + length]))
                    length--;
            }

            var kind = best.Kind;
            var isError = best.Type == EntryType.Error;
            if (best.Rule.IsIdentifier && !tooLong && mode.TryGetKeyword(text, position, length, out var keywordKind))
            {
                kind = keywordKind;
                isError = false;
            }
            else if (isError)
            {
                kind = _errorKind;
            }

            var span = new TextSpan(position, length);
            if (tooLong)
            {
                isError = true;
                kind = _errorKind;
                if (diagnostics.Count < _options.MaxDiagnostics)
                    diagnostics.Add(new Diagnostic(TokenTooLongCode, $"The token is longer than {_options.MaxTokenLength} characters", span));
            }
            else if (bestUnterminated)
            {
                isError = true;
                if (diagnostics.Count < _options.MaxDiagnostics)
                    diagnostics.Add(new Diagnostic(UnterminatedCode, "Unterminated token", span));
            }
            else if (isError && diagnostics.Count < _options.MaxDiagnostics)
            {
                diagnostics.Add(new Diagnostic(best.Code!, best.Message!, span));
            }

            hasErrors |= isError;
            if (isError || best.Type != EntryType.Trivia || _options.EmitTrivia)
                tokens.Add(new Token<TKind>(kind, span, isError));

            if (best.PushModeIndex >= 0)
            {
                if (stack.Count + 1 < _options.MaxModeDepth)
                {
                    stack.Add(modeIndex);
                    modeIndex = best.PushModeIndex;
                }
                else
                {
                    hasErrors = true;
                    if (diagnostics.Count < _options.MaxDiagnostics)
                        diagnostics.Add(new Diagnostic(ModeDepthCode, $"The mode stack is limited to {_options.MaxModeDepth} modes", span));
                }
            }
            else if (best.Pop && stack.Count > 0)
            {
                modeIndex = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
            }

            position += length;
        }

        return new TokenizeResult<TKind>(text, tokens.AsReadOnly(), diagnostics.AsReadOnly(), hasErrors);
    }

    // Longest match, then declaration order
    private static CompiledEntry? FindBest(string text, int position, CompiledMode mode, out int bestLength, out bool bestUnterminated)
    {
        CompiledEntry? best = null;
        bestLength = 0;
        bestUnterminated = false;

        var c = text[position];
        var candidates = c < 128 ? mode.AsciiEntries[c] : mode.Entries;
        foreach (var entry in candidates)
        {
            if (c >= 128 && !entry.Rule.CanStart(c))
                continue;

            var length = entry.Rule.Match(text, position, out var unterminated);
            if (length > bestLength)
            {
                best = entry;
                bestLength = length;
                bestUnterminated = unterminated;
            }
        }

        return best;
    }

    /// <summary>
    /// Tokenize a text (the text is copied to keep it in the result, prefer <see cref="Tokenize(string)"/> when you already have a string)
    /// </summary>
    public TokenizeResult<TKind> Tokenize(ReadOnlySpan<char> text) => Tokenize(text.ToString());

    /// <summary>
    /// Tokenize a text (the text is copied unless it is a whole string)
    /// </summary>
    public TokenizeResult<TKind> Tokenize(ReadOnlyMemory<char> text)
    {
        if (MemoryMarshal.TryGetString(text, out var source, out var start, out var length) && start == 0 && length == source.Length)
            return Tokenize(source);

        return Tokenize(text.ToString());
    }

    /// <summary>
    /// Tokenize all the text of a reader (the reader handles the encoding, the BOM and the invalid bytes)
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public TokenizeResult<TKind> Tokenize(TextReader reader)
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        return Tokenize(reader.ReadToEnd());
    }

    private static string Describe(string text, int position, int length)
    {
        var value = text.Substring(position, length);
        return length == 1 && (char.IsControl(value[0]) || char.IsSurrogate(value[0])) ? $"U+{(int)value[0]:X4}" : $"'{value}'";
    }

    private static CompiledMode[] Compile(IReadOnlyList<ModeDefinition<TKind>> definitions)
    {
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < definitions.Count; i++)
            indexes[definitions[i].Name] = i;

        var modes = new CompiledMode[definitions.Count];
        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            var displayName = i == 0 ? "the initial mode" : $"mode '{definition.Name}'";
            if (definition.Entries.Count == 0)
                throw new InvalidOperationException($"The {displayName} has no rule");

            var entries = new List<CompiledEntry>();
            foreach (var entry in definition.Entries)
            {
                var pushIndex = -1;
                if (entry.PushMode != null && !indexes.TryGetValue(entry.PushMode, out pushIndex))
                    throw new InvalidOperationException($"The {displayName} pushes the unknown mode '{entry.PushMode}'");

                if (entry.PushMode != null && pushIndex == 0)
                    throw new InvalidOperationException($"The {displayName} can't push the initial mode by its name");

                entries.Add(new CompiledEntry(entry, pushIndex));
            }

            modes[i] = new CompiledMode(entries.ToArray(), CompileKeywords(definition, displayName));
        }

        return modes;
    }

    private static KeywordTable? CompileKeywords(ModeDefinition<TKind> definition, string displayName)
    {
        if (definition.Keywords.Count == 0)
            return null;

        if (!definition.Entries.Exists(x => x.Rule.IsIdentifier))
            throw new InvalidOperationException($"The {displayName} defines keywords but no rule based on Rule.Identifier");

        var table = new KeywordTable();
        foreach (var group in definition.Keywords)
        {
            foreach (var word in group.Words)
            {
                var dictionary = group.IgnoreCase ? table.IgnoreCase : table.CaseSensitive;
                if (table.CaseSensitive.ContainsKey(word) || table.IgnoreCase.ContainsKey(word))
                    throw new InvalidOperationException($"The keyword '{word}' is defined twice in the {displayName}");

                dictionary[word] = group.Kind;
                table.MinLength = Math.Min(table.MinLength, word.Length);
                table.MaxLength = Math.Max(table.MaxLength, word.Length);
            }
        }

        return table;
    }

    private sealed class KeywordTable
    {
        public Dictionary<string, TKind> CaseSensitive { get; } = new Dictionary<string, TKind>(StringComparer.Ordinal);

        public Dictionary<string, TKind> IgnoreCase { get; } = new Dictionary<string, TKind>(StringComparer.OrdinalIgnoreCase);

        public int MinLength { get; set; } = int.MaxValue;

        public int MaxLength { get; set; }
    }

    private sealed class CompiledEntry
    {
        public CompiledEntry(RuleEntry<TKind> entry, int pushModeIndex)
        {
            Rule = entry.Rule;
            Kind = entry.Kind;
            Type = entry.Type;
            Code = entry.Code;
            Message = entry.Message;
            PushModeIndex = pushModeIndex;
            Pop = entry.Pop;
        }

        public Rule Rule { get; }

        public TKind Kind { get; }

        public EntryType Type { get; }

        public string? Code { get; }

        public string? Message { get; }

        public int PushModeIndex { get; }

        public bool Pop { get; }
    }

    private sealed class CompiledMode
    {
        private readonly KeywordTable? _keywords;

        public CompiledMode(CompiledEntry[] entries, KeywordTable? keywords)
        {
            Entries = entries;
            _keywords = keywords;

            // Rules are indexed by the first character (ASCII), the order of declaration is kept
            AsciiEntries = new CompiledEntry[128][];
            for (var c = 0; c < 128; c++)
            {
                var candidates = new List<CompiledEntry>();
                foreach (var entry in entries)
                {
                    if (entry.Rule.CanStart((char)c))
                        candidates.Add(entry);
                }

                AsciiEntries[c] = candidates.ToArray();
            }
        }

        public CompiledEntry[] Entries { get; }

        public CompiledEntry[][] AsciiEntries { get; }

        public bool TryGetKeyword(string text, int start, int length, out TKind kind)
        {
            kind = default;
            if (_keywords == null || length < _keywords.MinLength || length > _keywords.MaxLength)
                return false;

            var word = text.Substring(start, length);
            return _keywords.CaseSensitive.TryGetValue(word, out kind) || _keywords.IgnoreCase.TryGetValue(word, out kind);
        }
    }
}
