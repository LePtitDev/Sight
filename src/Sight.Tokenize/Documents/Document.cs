namespace Sight.Tokenize.Documents
{
    /// <summary>
    /// Static methods can construct documents
    /// </summary>
    public static class Document
    {
        /// <summary>
        /// Create document from text
        /// </summary>
        public static IDocument FromText(string text)
        {
            return new StringDocument(text);
        }
    }
}
