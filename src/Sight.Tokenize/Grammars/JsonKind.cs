namespace Sight.Tokenize;

/// <summary>
/// Kinds of the tokens of <see cref="Grammars.Json"/>
/// </summary>
public enum JsonKind
{
    /// <summary>
    /// Whitespace (trivia)
    /// </summary>
    Whitespace,

    /// <summary>
    /// Comment (trivia, only with <see cref="Grammars.CreateJson"/> and <c>allowComments</c>)
    /// </summary>
    Comment,

    /// <summary>
    /// '{'
    /// </summary>
    BeginObject,

    /// <summary>
    /// '}'
    /// </summary>
    EndObject,

    /// <summary>
    /// '['
    /// </summary>
    BeginArray,

    /// <summary>
    /// ']'
    /// </summary>
    EndArray,

    /// <summary>
    /// ':'
    /// </summary>
    Colon,

    /// <summary>
    /// ','
    /// </summary>
    Comma,

    /// <summary>
    /// String with its quotes
    /// </summary>
    String,

    /// <summary>
    /// Number
    /// </summary>
    Number,

    /// <summary>
    /// true, false or null
    /// </summary>
    Keyword,

    /// <summary>
    /// Invalid text
    /// </summary>
    Error
}
