using Scover.Psdc.Messages;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

/// <summary>
/// A branch in a parsing operation to parse a specific type of node.
/// </summary>
/// <typeparam name="T">The type of node this branch parses.</typeparam>
/// <param name="operation">The current parsing operation.</param>
/// <returns>The current <see cref="ParseOperation"> and a result creator to use to retrieve the result based on the final read tokens of the parse operation.</returns>
delegate (ParseOperation, ResultCreator<T>) Branch<T>(ParseOperation operation);
/// <summary>
/// A function that parses a production rule.
/// </summary>
/// <typeparam name="T">The type of the AST node this function produces.</typeparam>
/// <param name="tokens">The input tokens.</param>
/// <returns>A result that encapsulates <typeparamref name="T"/>.</returns>
delegate ParseResult<T> Parser<out T>(IEnumerable<Token> tokens);

/// <summary>
/// A function that creates a node from source tokens.
/// </summary>
/// <typeparam name="T">The type of the node to create.</typeparam>
/// <param name="sourceTokens">The source tokens of the node.</param>
/// <returns>The resulting node.</returns>
delegate T ResultCreator<out T>(SourceTokens sourceTokens);

/// <summary>
/// A fluent class to parse production rules.
/// </summary>
abstract class ParseOperation
{
    readonly IEnumerable<Token> _tokens;
    int _readCount;

    ParseOperation(IEnumerable<Token> tokens, int readCount)
     => (_tokens, _readCount) = (tokens, readCount);

    /// <summary>
    /// Get the tokens that have been read so far.
    /// </summary>
    /// <value>A new <see cref="SourceTokens"/> instance containing the tokens that have been read so far.</value>
    protected SourceTokens ReadTokens => new(_tokens, _readCount);

    /// <summary>
    /// Start a parsing operation.
    /// </summary>
    /// <param name="messenger">The messenger which will recieve syntax errors when multi-parsing.</param>
    /// <param name="tokens">The input tokens.</param>
    /// <param name="production">Name of the production to parse.</param>
    /// <returns>A new <see cref="ParseOperation"/>.</returns>
    public static ParseOperation Start(Messenger messenger, IEnumerable<Token> tokens, string production)
     => new SuccessfulSoFarOperation(tokens, messenger, production);

    public ParseOperation ChooseBranch<T>(
            out Branch<T> branch,
            Dictionary<TokenType, Branch<T>> branches)
         => ChooseBranch(out branch, (IReadOnlyDictionary<TokenType, Branch<T>>)branches);

    /// <summary>
    /// Choose a branch to execute based on the type of the next token.
    /// </summary>
    /// <typeparam name="T">The type of the node to parse.</typeparam>
    /// <param name="branch">Assigned to the choosen branch.</param>
    /// <param name="branches">The available branches, keyed by token type.</param>
    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ChooseBranch<T>(
        out Branch<T> branch,
        IReadOnlyDictionary<TokenType, Branch<T>> branches);

    /// <summary>
    /// Fork the parse operation based on a previously chosen branch.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the result creator. Pass it to <see cref="MapResult"/> to retrieve the parse result.</param>
    /// <param name="branch">The branch to execute.</param>
    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation Fork<T>(out ResultCreator<T> result, Branch<T> branch);

    /// <summary>
    /// Get an intermediate result based on the tokens read so far; the parsing operation continues.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the created result.</param>
    /// <param name="resultCreator">The result creator function.</param>
    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation GetIntermediateResult<T>(out T result, ResultCreator<T> resultCreator);

    /// <summary>
    /// Retrieve the final result of a parse operation.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="resultCreator">The result creator function.</param>
    /// <returns>An Ok result containing the parsed node, or a failure result with a error indicating what went wrong.</returns>
    public abstract ParseResult<T> MapResult<T>(ResultCreator<T> resultCreator);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation Parse<T>(out T result, Parser<T> parse);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyList<T> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken = true, bool allowTrailingSeparator = false);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyList<T> result, Parser<T> parse, params TokenType[] endTokens);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseOptional<T>(out Option<T> result, Parser<T> parse);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseOptionalToken(TokenType type);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseToken(TokenType type);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseTokenValue(out string result, TokenType type);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyList<T> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken = true, bool allowTrailingSeparator = false);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyList<T> result, Parser<T> parse, params TokenType[] endTokens);

    /// <summary>
    /// Skims <paramref name="n"/> tokens.
    /// </summary>
    /// <param name="n">The number of token to skim.</param>
    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation Skim(int n);

    sealed class FailedOperation(IEnumerable<Token> tokens, int readCount, ParseError error) : ParseOperation(tokens, readCount)
    {
        readonly ParseError _error = error;

        public override ParseOperation ChooseBranch<T>(out Branch<T> branch, IReadOnlyDictionary<TokenType, Branch<T>> branches)
        {
            branch = null!;
            return this;
        }

        public override ParseOperation Fork<T>(out ResultCreator<T> result, Branch<T> branch)
        {
            result = null!;
            return this;
        }

        public override ParseOperation GetIntermediateResult<T>(out T result, ResultCreator<T> resultCreator)
        {
            result = default!;
            return this;
        }

        public override ParseResult<T> MapResult<T>(ResultCreator<T> result) => ParseResult.Fail<T>(ReadTokens, _error);

        public override ParseOperation Parse<T>(out T result, Parser<T> parse)
        {
            result = default!;
            return this;
        }

        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyList<T> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken, bool allowTrailingSeparator)
        {
            result = Array.Empty<T>();
            return this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyList<T> result, Parser<T> parse, params TokenType[] endTokens)
        {
            result = Array.Empty<T>();
            return this;
        }

        public override ParseOperation ParseOptional<T>(out Option<T> result, Parser<T> parse)
        {
            result = Option.None<T>();
            return this;
        }

        public override ParseOperation ParseOptionalToken(TokenType type) => this;
        public override ParseOperation ParseToken(TokenType type) => this;

        public override ParseOperation ParseTokenValue(out string result, TokenType type)
        {
            result = null!;
            return this;
        }

        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyList<T> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken, bool allowTrailingSeparator)
        {
            result = Array.Empty<T>();
            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyList<T> result, Parser<T> parse, params TokenType[] endTokens)
        {
            result = Array.Empty<T>();
            return this;
        }

        public override ParseOperation Skim(int n) => this;
    }

    sealed class SuccessfulSoFarOperation(IEnumerable<Token> tokens, Messenger messenger, string production) : ParseOperation(tokens, 0)
    {
        IEnumerable<Token> ParsingTokens => _tokens.Skip(_readCount);

        public override ParseOperation ChooseBranch<T>(out Branch<T> branch, IReadOnlyDictionary<TokenType, Branch<T>> branches)
        {
            var token = ParsingTokens.FirstOrNone();
            if (token.HasValue && branches.TryGetValue(token.Value.Type, out branch!)) {
                _readCount++;
                return this;
            }
            branch = default!;
            return Fail(ParseError.ForProduction(production, token, branches.Keys));
        }

        public override ParseOperation Fork<T>(out ResultCreator<T> result, Branch<T> branch)
        {
            (ParseOperation p, result) = branch(this);
            return p;
        }

        public override ParseOperation GetIntermediateResult<T>(out T result, ResultCreator<T> resultCreator)
        {
            result = resultCreator(ReadTokens);
            return this;
        }

        public override ParseResult<T> MapResult<T>(ResultCreator<T> resultCreator) => MakeOkResult(resultCreator(ReadTokens));

        public override ParseOperation Parse<T>(out T result, Parser<T> parse)
        {
            var pr = parse(ParsingTokens);
            _readCount += pr.SourceTokens.Count;
            if (pr.HasValue) {
                result = pr.Value;
                return this;
            } else {
                result = default!;
                return Fail(MakeOurs(pr.Error));
            }
        }

        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyList<T> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken, bool allowTrailingSeparator)
        {
            List<T> items = [];
            result = items;

            if (Peek(end).ValueOr(true)) {
                // try to parse to get an appropriate error, it should fail since we're on the end token. 
                return Fail(parse(ParsingTokens).Error.NotNull());
            } else {
                do {
                    var item = parse(ParsingTokens);
                    _readCount += item.SourceTokens.Count;
                    AddOrSyntaxError(items, item);
                } while (Match(ParsingTokens, separator) && (!allowTrailingSeparator || !Peek(end).ValueOr(true)));
            }

            return readEndToken ? ParseToken(end) : this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyList<T> result, Parser<T> parse, params TokenType[] endTokens)
        {
            List<T> items = [];
            result = items;

            ParseResult<T> item = parse(ParsingTokens);
            _readCount += Math.Max(1, item.SourceTokens.Count);

            AddOrSyntaxError(items, item);

            if (!item.HasValue) {
                return Fail(MakeOurs(item.Error));
            }

            ParseWhile(items, parse, () => !Peek(endTokens).ValueOr(true));

            return this;
        }


        public override ParseOperation ParseOptional<T>(out Option<T> result, Parser<T> parse)
        {
            var pr = parse(ParsingTokens);
            if (pr.HasValue) {
                _readCount += pr.SourceTokens.Count;
            }
            result = pr.DiscardError();
            return this;
        }

        public override ParseOperation ParseOptionalToken(TokenType type)
        {
            var token = ParsingTokens.FirstOrNone();
            if (token.HasValue && token.Value.Type == type) {
                _readCount++;
            }
            return this;
        }

        public override ParseOperation ParseToken(TokenType type)
        {
            var token = ParsingTokens.FirstOrNone();
            if (token.HasValue && token.Value.Type == type) {
                _readCount++;
                return this;
            }
            return Fail(ParseError.ForTerminal(production, token, type));
        }

        public override ParseOperation ParseTokenValue(out string result, TokenType type)
        {
            var token = ParsingTokens.FirstOrNone();
            if (token.HasValue && token.Value.Type == type) {
                _readCount++;
                result = token.Value.Value ?? throw new InvalidOperationException("Parsed token doesn't have a value");
                return this;
            }

            result = null!;
            return Fail(ParseError.ForTerminal(production, token, type));
        }

        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyList<T> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken, bool allowTrailingSeparator)
        {
            List<T> items = [];
            result = items;

            if (!Peek(end).ValueOr(true)) {
                do {
                    var item = parse(ParsingTokens);
                    _readCount += item.SourceTokens.Count;
                    AddOrSyntaxError(items, item);
                } while (Match(ParsingTokens, separator) && (!allowTrailingSeparator || !Peek(end).ValueOr(true)));
            }

            if (allowTrailingSeparator) {
                ParseOptionalToken(separator);
            }

            return readEndToken ? ParseToken(end) : this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyList<T> result, Parser<T> parse, params TokenType[] endTokens)
        {
            List<T> items = [];
            result = items;

            ParseWhile(items, parse, () => !Peek(endTokens).ValueOr(true));

            return this;
        }

        public override ParseOperation Skim(int n)
        {
            _readCount += n;
            return this;
        }

        void AddOrSyntaxError<T>(ICollection<T> items, ParseResult<T> item)
         => item.Match(
                some: items.Add,
                none: error => messenger.Report(Message.ErrorSyntax(item.SourceTokens, error))
            );

        bool Match(IEnumerable<Token> tokens, params TokenType[] types)
        {
            bool nextIsSeparator = tokens.NextIsOfType(types).ValueOr(false);
            if (nextIsSeparator) {
                _readCount++;
            }
            return nextIsSeparator;
        }

        FailedOperation Fail(ParseError error) => new(_tokens, _readCount, error);

        ParseResult<T> MakeOkResult<T>(T result) => ParseResult.Ok(new(_tokens, _readCount), result);

        ParseError MakeOurs(ParseError error)
         => production == error.FailedProduction
            ? error
            : ParseError.ForProduction(production,
                error.ErroneousToken, error.ExpectedTokens, error.FailedProduction);

        Option<bool> Peek(params TokenType[] types)
         => ParsingTokens.NextIsOfType(types);

        void ParseWhile<T>(ICollection<T> items, Parser<T> parse, Func<bool> keepGoingWhile, Func<bool>? skimWhile = null)
        {
            while (skimWhile?.Invoke() ?? false) {
                _readCount++;
            }

            ParseError? lastError = null;
            while (keepGoingWhile()) {
                var item = parse(ParsingTokens);
                _readCount += Math.Max(1, item.SourceTokens.Count);

                if (item.HasValue || !item.Error.IsEquivalent(lastError)) {
                    AddOrSyntaxError(items, item);
                }

                if (!item.HasValue) {
                    lastError = item.Error;
                    while (skimWhile?.Invoke() ?? false) {
                        _readCount++;
                    }
                }
            }
        }
    }
}
