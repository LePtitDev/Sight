using System;
using System.Collections.Generic;
using Sight.Tokenize.Tokens;

namespace Sight.Tokenize.Parsing
{
    /// <summary>
    /// Describe the result of a parsing operation
    /// </summary>
    public readonly struct ParseResult
    {
        private ParseResult(bool isSuccess, IReadOnlyList<string> errors, IReadOnlyList<Token> tokens)
        {
            IsSuccess = isSuccess;
            Errors = errors;
            Tokens = tokens;
        }

        /// <summary>
        /// Indicates if the parsing succeeded
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Error message when the parsing failed
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Tokens list when the parsing succeeded
        /// </summary>
        public IReadOnlyList<Token> Tokens { get; }

        /// <summary>
        /// Create a success result
        /// </summary>
        public static ParseResult Success(IReadOnlyList<Token> tokens, IReadOnlyList<string> errors)
        {
            return new ParseResult(true, errors, tokens);
        }

        /// <inheritdoc cref="Success(IReadOnlyList{Token},IReadOnlyList{string})"/>
        public static ParseResult Success(IReadOnlyList<Token> tokens, params string[] errors) => Success(tokens, (IReadOnlyList<string>)errors);

        /// <summary>
        /// Create a fail result
        /// </summary>
        public static ParseResult Fail(IReadOnlyList<Token> tokens, IReadOnlyList<string> errors)
        {
            return new ParseResult(false, errors, tokens);
        }

        /// <inheritdoc cref="Fail(IReadOnlyList{Token},IReadOnlyList{string})"/>
        public static ParseResult Fail(IReadOnlyList<Token> tokens, params string[] errors) => Fail(tokens, (IReadOnlyList<string>)errors);

        /// <inheritdoc cref="Fail(IReadOnlyList{Token},IReadOnlyList{string})"/>
        public static ParseResult Fail(IReadOnlyList<string> errors) => Fail(Array.Empty<Token>(), errors);

        /// <inheritdoc cref="Fail(IReadOnlyList{Token},IReadOnlyList{string})"/>
        public static ParseResult Fail(params string[] errors) => Fail((IReadOnlyList<string>)errors);
    }
}
