using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

/// <summary>
/// A function that parses a production rule.
/// </summary>
/// <typeparam name="T">The type of the AST node this function produces.</typeparam>
/// <param name="tokens">The input tokens.</param>
/// <returns>A result that encapsulates <typeparamref name="T"/>.</returns>
internal delegate ParseResult<T> Parser<out T>(IEnumerable<Token> tokens);

/// <summary>
/// A function that creates a node from source tokens.
/// </summary>
/// <typeparam name="T">The type of the node to create.</typeparam>
/// <param name="sourceTokens">The source tokens of the node.</param>
/// <returns>The resulting node.</returns>
internal delegate T ResultCreator<out T>(SourceTokens sourceTokens);

/// <summary>
/// A branch in a parsing operation to parse a specific type of node.
/// </summary>
/// <typeparam name="T">The type of node this branch parses.</typeparam>
/// <param name="operation">The current parsing operation.</param>
/// <returns>A result creator to use to retrieve the result.</returns>
internal delegate ResultCreator<T> Branch<out T>(ParseOperation operation);

/// <summary>
/// A fluent class to parse production rules.
/// </summary>
internal abstract class ParseOperation
{
    private int _readCount;
    private readonly IEnumerable<Token> _tokens;

    private ParseOperation(IEnumerable<Token> tokens, int readCount)
     => (_tokens, _readCount) = (tokens, readCount);

    /// <summary>
    /// Start a parsing operation.
    /// </summary>
    /// <param name="messenger">The messenger which will recieve syntax errors when multi-parsing.</param>
    /// <param name="tokens">The input tokens.</param>
    /// <returns>A new <see cref="ParseOperation"/>.</returns>
    public static ParseOperation Start(Messenger messenger, IEnumerable<Token> tokens)
     => new SuccessfulSoFarOperation(tokens, messenger);

    /// <summary>
    /// Skims <paramref name="n"/> tokens.
    /// </summary>
    /// <param name="n">The number of token to skim.</param>
    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation Skim(int n);

    /// <summary>
    /// Get the tokens that have been read so far.
    /// </summary>
    /// <value>A new <see cref="SourceTokens"/> instance containing the tokens that have been read so far.</value>
    protected SourceTokens ReadTokens => new(_tokens, _readCount);

    /// <summary>
    /// Fork the parse operation based on a previously chosen branch.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the result creator. Pass it to <see cref="MapResult"/> to retrieve the parse result.</param>
    /// <param name="branch">The branch to execute.</param>
    /// <returns></returns>
    public abstract ParseOperation Fork<T>(out ResultCreator<T> result, Branch<T> branch);
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
    /// Retrieve the final result of a parse operation.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="resultCreator">The result creator function.</param>
    /// <returns>An Ok result containing the parsed node, or a failure result with a error indicating what went wrong.</returns>
    public abstract ParseResult<T> MapResult<T>(ResultCreator<T> resultCreator);

    /// <summary>
    /// Get an intermidiate result based on the tokens read so far; the parsing operation continues.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the created result.</param>
    /// <param name="resultCreator">The result creator function.</param>
    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation GetIntermediateResult<T>(out T result, ResultCreator<T> resultCreator);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation Parse<T>(out T result, Parser<T> parse);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseOptional<T>(out Option<T> result, Parser<T> parse);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyCollection<T> result, Parser<T> parse, TokenType separator);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, Parser<T> parse, params TokenType[] endTokens);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseTokenValue(out string result, TokenType type);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseToken(TokenType type);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyCollection<T> result, Parser<T> parse, TokenType separator);

    /// <returns>The current <see cref="ParseOperation">.</returns>
    public abstract ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, Parser<T> parse, params TokenType[] endTokens);

    private sealed class SuccessfulSoFarOperation(IEnumerable<Token> tokens, Messenger messenger) : ParseOperation(tokens, 0)
    {
        private IEnumerable<Token> ParsingTokens => _tokens.Skip(_readCount);

        public override ParseOperation GetIntermediateResult<T>(out T result, ResultCreator<T> resultCreator)
        {
            result = resultCreator(ReadTokens);
            return this;
        }

        public override ParseOperation ChooseBranch<T>(out Branch<T> branch, IReadOnlyDictionary<TokenType, Branch<T>> branches)
        {
            var token = ParsingTokens.FirstOrNone();
            if (token.HasValue && branches.TryGetValue(token.Value.Type, out branch!)) {
                _readCount++;
                return this;
            }
            branch = default!;
            return Fail(ParseError.Create<T>(token, branches.Keys));
        }

        public override ParseOperation Fork<T>(out ResultCreator<T> result, Branch<T> branch)
        {
            result = branch(this);
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
                return Fail(pr.Error);
            }
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
        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyCollection<T> result, Parser<T> parse, TokenType separator)
        {
            List<T> items = [];
            result = items;

            ParseResult<T> item = parse(ParsingTokens);

            _readCount += item.SourceTokens.Count;

            bool separatorPresent = CheckAndConsumeToken(ParsingTokens, separator);

            if (item.HasValue) {
                items.Add(item.Value);
            } else {
                if (!separatorPresent) {
                    // If the first item didn't have a value and wasn't followed by a separator, go back in the read tokens to where we were when we started.
                    // This is so we don't read too many tokens when there are zero elements.
                    _readCount -= item.SourceTokens.Count;
                }

                return Fail(item.Error);
            }

            if (separatorPresent) {
                ParseWhile(items, parse, () => CheckAndConsumeToken(ParsingTokens, separator), doWhile: true);
            }

            return this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, Parser<T> parse, params TokenType[] endTokens)
        {
            List<T> items = [];
            result = items;

            ParseResult<T> item = parse(ParsingTokens);
            _readCount += Math.Max(1, item.SourceTokens.Count);

            AddOrSyntaxError(items, item);

            if (!item.HasValue) {
                return Fail(item.Error);
            }

            ParseWhile(items, parse, () => !NextParsingTokenIsAny(endTokens).ValueOr(true));

            return this;
        }

        public override ParseOperation ParseToken(TokenType type)
        {
            Option<Token> token = ParsingTokens.FirstOrNone();
            if (token.HasValue && token.Value.Type == type) {
                _readCount++;
                return this;
            }
            return Fail(ParseError.Create<Token>(token, type));
        }

        public override ParseOperation ParseTokenValue(out string result, TokenType type)
        {
            Option<Token> token = ParsingTokens.FirstOrNone();
            if (token.HasValue && token.Value.Type == type) {
                _readCount++;
                result = token.Value.Value ?? throw new InvalidOperationException("Parsed token doesn't have a value");
                return this;
            }

            result = null!;
            return Fail(ParseError.Create<Token>(token, type));
        }

        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyCollection<T> result, Parser<T> parse, TokenType separator)
        {
            List<T> items = [];
            result = items;

            ParseResult<T> item = parse(ParsingTokens);

            _readCount += item.SourceTokens.Count;

            bool separatorPresent = CheckAndConsumeToken(ParsingTokens, separator);

            if (item.HasValue) {
                items.Add(item.Value);
            } else if (!separatorPresent) {
                // If the first item didn't have a value and wasn't followed by a separator, go back in the read tokens to where we were when we started.
                // This is so we don't read too many tokens when there are zero elements.
                _readCount -= item.SourceTokens.Count;
            }

            if (separatorPresent) {
                ParseWhile(items, parse, () => CheckAndConsumeToken(ParsingTokens, separator), doWhile: true);
            }

            return this;
        }

        private void ParseWhile<T>(ICollection<T> items, Parser<T> parse, Func<bool> doKeepGoing, bool doWhile = false)
        {
            ParseError? lastError = null;
            while (doWhile || doKeepGoing()) {
                doWhile = false;

                var item = parse(ParsingTokens);
                _readCount += Math.Max(1, item.SourceTokens.Count);

                if (item.HasValue || !item.Error.IsEquivalent(lastError)) {
                    AddOrSyntaxError(items, item);
                }

                if (!item.HasValue) {
                    lastError = item.Error;
                }
            }
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, Parser<T> parse, params TokenType[] endTokens)
        {
            List<T> items = [];
            result = items;

            ParseWhile(items, parse, () => !NextParsingTokenIsAny(endTokens).ValueOr(true));

            return this;
        }

        private ParseResult<T> MakeOkResult<T>(T result) => ParseResult.Ok(new(_tokens, _readCount), result);
        private bool CheckAndConsumeToken(IEnumerable<Token> tokens, TokenType type)
        {
            bool nextIsSeparator = NextTokenIs(tokens, type);
            if (nextIsSeparator) {
                _readCount++;
            }
            return nextIsSeparator;
        }

        private void AddOrSyntaxError<T>(ICollection<T> items, ParseResult<T> item)
         => item.Match(
                some: items.Add,
                none: error => messenger.Report(Message.ErrorSyntax(item.SourceTokens, error))
            );

        private FailedOperation Fail(ParseError error) => new(_tokens, _readCount, error);

        private Option<bool> NextParsingTokenIsAny(IEnumerable<TokenType> types)
         => ParsingTokens.FirstOrNone().Map(token => types.Contains(token.Type));
        private static bool NextTokenIs(IEnumerable<Token> tokens, TokenType type)
         => tokens.FirstOrNone().Map(token => token.Type == type).ValueOr(false);

        public override ParseOperation Skim(int n)
        {
            _readCount += n;
            return this;
        }
    }

    private sealed class FailedOperation(IEnumerable<Token> tokens, int readCount, ParseError error) : ParseOperation(tokens, readCount)
    {
        private readonly ParseError _error = error;

        public override ParseResult<T> MapResult<T>(ResultCreator<T> result) => ParseResult.Fail<T>(ReadTokens, _error);
        public override ParseOperation Parse<T>(out T result, Parser<T> parse)
        {
            result = default!;
            return this;
        }
        public override ParseOperation ParseOptional<T>(out Option<T> result, Parser<T> parse)
        {
            result = Option.None<T>();
            return this;
        }
        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyCollection<T> result, Parser<T> parse, TokenType separator)
        {
            result = Array.Empty<T>();
            return this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, Parser<T> parse, params TokenType[] endTokens)
        {
            result = Array.Empty<T>();
            return this;
        }
        public override ParseOperation ParseTokenValue(out string result, TokenType type)
        {
            result = null!;
            return this;
        }
        public override ParseOperation ParseToken(TokenType type) => this;
        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyCollection<T> result, Parser<T> parse, TokenType separator)
        {
            result = Array.Empty<T>();
            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, Parser<T> parse, params TokenType[] endTokens)
        {
            result = Array.Empty<T>();
            return this;
        }

        public override ParseOperation Fork<T>(out ResultCreator<T> result, Branch<T> branch)
        {
            result = null!;
            return this;
        }

        public override ParseOperation ChooseBranch<T>(out Branch<T> branch, IReadOnlyDictionary<TokenType, Branch<T>> branches)
        {
            branch = null!;
            return this;
        }

        public override ParseOperation Skim(int n) => this;
        public override ParseOperation GetIntermediateResult<T>(out T result, ResultCreator<T> resultCreator)
        {
            result = default!;
            return this;
        }
    }
}
