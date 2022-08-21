# Sight.Tokenize

[![](https://img.shields.io/nuget/v/Sight.Tokenize.svg)](https://www.nuget.org/packages/Sight.Tokenize/)

Utilities to parse a document into structured tokens.

## How to use?

A tokenizer implement the interface `ITokenizer`.

```csharp
var result = await tokenizer.ReadAsync(text);
// Result:
//   IsSuccess: boolean
//   Tokens: array of Tokens
//   Errors: array of strings
```

The class `Token` is abstract and in standalone library was implemented by `SymbolToken` and `InvalidToken`.

## JSON

```csharp
var tokenizer = new JsonTokenizer();
var result = await tokenizer.ReadAsync(text);
```

## XML

```csharp
var tokenizer = new XmlTokenizer();
var result = await tokenizer.ReadAsync(text);
```

## Dynamic tokenizer

Dynamic tokenizer allow to create a specific tokenizer without a complex implementation. In this sample...

```csharp
var tokenizer = new DynamicTokenizer()
    .IgnoreSymbols(new [] { ' ', '\t', '\r', '\n' })
    .AddSymbol("delimiter", '[')
    .AddSymbol("delimiter", ']')
    .AddSymbol("delimiter", '=')
    .AddSymbol("delimiter", ';')
    .AddLiteral("symbol", "NULL")
    .AddLiteral("symbol", "TRUE")
    .AddLiteral("symbol", "FALSE")
    .AddNumeric("number", s => double.TryParse(s, out _))
    .AddBlock("identifier", '<', '>')
    .AddBlock("string", '\'', '\'', '\\');

var result = await tokenizer.ReadAsync(text);
```

... the tokenizer support JSON like document with differents symbols:

```
[
  <firstName> = 'John';
  <lastName> = 'Smith';
  <isAlive> = TRUE;
  <age> = 27;
  <address> = [
    <streetAddress> = '21 2nd Street';
    <city> = 'New York';
    <state> = 'NY';
    <postalCode> = '10021-3100'
  ];
  <phoneNumbers> = [
    [
      <type> = 'home';
      <number> = '212 555-1234'
    ];
    [
      <type> = 'office';
      <number> = '646 555-4567'
    ]
  ];
  <children> = [
    'Catherine';
    'Thomas';
    'Trevor'
  ];
  <spouse> = NULL
]
```
