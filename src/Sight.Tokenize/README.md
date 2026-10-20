# Sight.Tokenize

[![](https://img.shields.io/nuget/v/Sight.Tokenize.svg)](https://www.nuget.org/packages/Sight.Tokenize/)

A declarative lexer: describe the tokens of a language with a builder (a small DSL) and get tokens back. It is meant for
syntax highlighting, tools and parsers front-ends, not to compete with `Utf8JsonReader` or `XmlReader`.

The lexer is:

- **lossless**: trivia (whitespaces, comments) can be emitted, and the concatenation of all tokens is exactly the source text;
- **error tolerant**: it never throws on an input, invalid text becomes error tokens and diagnostics;
- **safe on untrusted input**: linear time and bounded memory (see [Safeguards](#safeguards)).

> Version 0.2.0 replaces the whole API of 0.1.x (`ITokenizer`, `JsonTokenizer`, `XmlTokenizer`, `DynamicTokenizer`, `SymbolToken`...).
> The JSON and XML tokenizers are now predefined grammars built with the same builder.

## How to use?

```csharp
enum Kind { Whitespace, Comment, LBracket, RBracket, Equals, Semicolon, Keyword, Number, String, Identifier, Error }

var lexer = Lexer.Create<Kind>(b => b
    .ErrorKind(Kind.Error)
    .Trivia(Kind.Whitespace, Rule.Whitespace)
    .Trivia(Kind.Comment, Rule.LineComment("#"))
    .Token(Kind.LBracket, "[").Token(Kind.RBracket, "]")
    .Token(Kind.Equals, "=").Token(Kind.Semicolon, ";")
    .Token(Kind.Number, Rule.Number)
    .Token(Kind.String, Rule.Delimited("'", "'", escape: "\\"))
    .Token(Kind.Identifier, Rule.Identifier)
    .Keywords(Kind.Keyword, "NULL", "TRUE", "FALSE"));

TokenizeResult<Kind> result = lexer.Tokenize("[ a = 'x' ; b = -12.5 ; c = NULL ]");

foreach (var token in result.Tokens)
    Console.WriteLine($"{token.Kind} {result.GetText(token)} @ {result.GetLineColumn(token)}");

foreach (var diagnostic in result.Diagnostics)
    Console.WriteLine($"{diagnostic.Code} {diagnostic.Span}: {diagnostic.Message}");
```

- `Tokenize` accepts a `string`, a `ReadOnlySpan<char>`, a `ReadOnlyMemory<char>` or a `TextReader` (the reader handles
  the encoding, the BOM and the invalid bytes: use a `StreamReader` for a `Stream`). The tokenization is synchronous.
- A `Token<TKind>` is a `readonly struct` (`Kind`, `Span` and `IsError`). `TextSpan` is expressed in UTF-16 code units
  (like `string.Substring`).
- `TokenizeResult<TKind>` gives `Tokens`, `Diagnostics`, `HasErrors`, `GetText(token)`, `GetSpan(token)` (no allocation)
  and `GetLineColumn(...)` (1-based, the line starts table is built on the first call).
- The lexer is immutable and thread safe. `Build()` validates the configuration (unknown mode, mode without rule, empty
  literal, keywords without `Rule.Identifier` rule...) and throws on construction, never during the tokenization.

### Rules

A rule recognizes a lexeme at the current position and returns its length (0: no match). Rules never allocate.

| Rule | Description |
| --- | --- |
| `"text"` or `Rule.Literal(text, ignoreCase)` | Literal, possibly multi-characters (`"=>"`, `"=="`) |
| `Rule.Whitespace` | Run of whitespaces |
| `Rule.Identifier` | Letter or `_` followed by letters, digits or `_` |
| `Rule.Number` | `-? digits (. digits)? ([eE] [+-]? digits)?` |
| `Rule.Delimited(begin, end, escape, multiline)` | String, block comment... With an escape sequence (or a doubled end when `escape == end`) |
| `Rule.LineComment(prefix)` | Prefix up to the end of the line |
| `Rule.Regex(pattern, options, timeout, firstChars)` | Anchored at the current position, with a mandatory timeout (ReDoS), non backtracking engine when available |
| `Rule.Custom(matcher, firstChars)` | Your own `int Matcher(ReadOnlySpan<char> input)` |

`firstChars` is an optional hint (characters that can start a match): rules are indexed by first character, opaque rules are tried at each position.

### Semantics

- **Longest match**: at each position the rule of the current mode that matches the longest text wins; in case of equality the first
  declared rule wins (so declare `Token(Kind.Arrow, "=>")` in any order, but declare a keyword literal before `Rule.Identifier` if it must win).
- **Trivia** (`Trivia(...)`) are emitted only with `LexerOptions.EmitTrivia = true`.
- **Keywords**: `Keywords(kind, "NULL", ...)` is a dictionary lookup applied when a rule based on `Rule.Identifier` matches
  (`Keywords(kind, ignoreCase: true, ...)` for a case insensitive lookup).
- **Modes**: a stack. `Push(kind, rule, "mode")` activates a mode, `Pop(kind, rule)` restores the previous one.

```csharp
var lexer = Lexer.Create<Kind>(b => b
    .Push(Kind.Tag, "<", "tag")
    .Token(Kind.Text, Rule.Regex("[^<]+"))
    .Mode("tag", m => m
        .Trivia(Kind.Whitespace, Rule.Whitespace)
        .Token(Kind.Identifier, Rule.Identifier)
        .Pop(Kind.Tag, ">")));
```

- **Errors**: if no rule matches, the character is emitted as an error token (`IsError`) and a `LEX001` diagnostic.
  `Error(rule, code, message)` declares an invalid lexeme (ex. a number followed by letters) reported as a single error token.
  An unterminated `Rule.Delimited` is reported once (`LEX003`): the error token covers the rest of the line, or the rest of the
  document when `multiline`, and the text is never scanned again.

### Safeguards

- Each step consumes at least one character, and each character is scanned a bounded number of times: the tokenization is linear.
- `LexerOptions` (second parameter of `Lexer.Create`): `EmitTrivia`, `MaxTokenLength` (a longer match is cut and reported, `LEX002`),
  `MaxDiagnostics` (stored diagnostics, `HasErrors` stays accurate) and `MaxModeDepth` (a deeper `Push` is ignored and reported, `LEX004`).
- `Rule.Regex` has a timeout and a timeout is "no match".

## Predefined grammars

```csharp
var json = Grammars.Json.Tokenize(text);                 // strict JSON
var jsonc = Grammars.JsonWithComments.Tokenize(text);    // with // and /* */ comments
var xml = Grammars.Xml.Tokenize(text);

// To keep the trivia (lossless tokenization)
var lexer = Grammars.CreateJson(allowComments: false, new LexerOptions { EmitTrivia = true });
```

- `Grammars.Json` (`JsonKind`): strict numbers (`-? (0|[1-9]\d*) (\.\d+)? ([eE][+-]?\d+)?`), strings with validated escapes (`\uXXXX`)
  and no control characters, keywords `true`/`false`/`null`. Invalid lexemes are one error token (`JSON001` string, `JSON002` number, `JSON003` literal).
- `Grammars.Xml` (`XmlKind`): content mode and tag mode (pushed by `<` and `</`, left by `>` and `/>`), names with `-`, `.` and `:`,
  comments, CDATA sections, `<!DOCTYPE` (with its internal subset) and `<? ?>` instructions.

They are also examples of the builder: see [Grammars.cs](Grammars/Grammars.cs).
