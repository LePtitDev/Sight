namespace Sight.Tokenize;

/// <summary>
/// Kinds of the tokens of <see cref="Grammars.Xml"/>
/// </summary>
public enum XmlKind
{
    /// <summary>
    /// Whitespace in a tag (trivia)
    /// </summary>
    Whitespace,

    /// <summary>
    /// Comment <c>&lt;!-- --&gt;</c> (trivia)
    /// </summary>
    Comment,

    /// <summary>
    /// Processing instruction or XML declaration <c>&lt;? ?&gt;</c>
    /// </summary>
    ProcessingInstruction,

    /// <summary>
    /// Document type declaration <c>&lt;!DOCTYPE ...&gt;</c>
    /// </summary>
    DocType,

    /// <summary>
    /// CDATA section <c>&lt;![CDATA[ ]]&gt;</c>
    /// </summary>
    CData,

    /// <summary>
    /// Text content
    /// </summary>
    Text,

    /// <summary>
    /// '&lt;' (start of an element tag)
    /// </summary>
    TagOpen,

    /// <summary>
    /// '&lt;/' (start of an end tag)
    /// </summary>
    EndTagOpen,

    /// <summary>
    /// '&gt;'
    /// </summary>
    TagClose,

    /// <summary>
    /// '/&gt;'
    /// </summary>
    SelfClose,

    /// <summary>
    /// Element or attribute name
    /// </summary>
    Name,

    /// <summary>
    /// '='
    /// </summary>
    Equals,

    /// <summary>
    /// Attribute value with its quotes
    /// </summary>
    String,

    /// <summary>
    /// Invalid text
    /// </summary>
    Error
}
