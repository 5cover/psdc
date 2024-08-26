using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

using TokenTypeSet = IReadOnlySet<TokenType>;

/// <summary>
/// A branch in a parsing operation to parse a specific type of node.
/// </summary>
/// <typeparam name="T">The type of node parsed.</typeparam>
/// <param name="operation">The current parsing operation.</param>
/// <returns>The current <see cref="ParseOperation"/> and a result creator to use to retrieve the result based on the final read tokens of the parsing operation.</returns>
delegate (ParseOperation, ResultCreator<T>) Branch<T>(ParseOperation operation);

/// <summary>
/// A function that parses a production rule.
/// </summary>
/// <typeparam name="T">The type of node parsed.</typeparam>
/// <param name="tokens">The input tokens.</param>
/// <returns>A result that encapsulates <typeparamref name="T"/>.</returns>
delegate ParseResult<T> Parser<out T>(IEnumerable<Token> tokens);

/// <summary>
/// A parsing block.
/// </summary>
/// <typeparam name="T">The type of node parsed.</typeparam>
/// <param name="operation">The enclosing parsing operation.</param>
/// <returns>A result that encapsulates <typeparamref name="T"/>.</returns>
delegate ParseResult<T> ParseBlock<out T>(ParseOperation operation);

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
    /// <param name="tokens">The input tokens.</param>
    /// <param name="production">Name of the production to parse.</param>
    /// <returns>A new <see cref="ParseOperation"/>.</returns>
    public static ParseOperation Start(IEnumerable<Token> tokens, string production)
     => new SuccessfulSoFarOperation(tokens, production);

    /// <inheritdoc cref="Switch{T}(out Branch{T}, IReadOnlyDictionary{TokenType, Branch{T}}, Branch{T}?)"/>
    public ParseOperation Switch<T>(
            out Branch<T> branch,
            Dictionary<TokenType, Branch<T>> cases,
            Branch<T>? @default = null)
     => Switch(out branch, (IReadOnlyDictionary<TokenType, Branch<T>>)cases, @default);

    /// <summary>
    /// Choose a branch to execute based on the type of the next token.
    /// </summary>
    /// <typeparam name="T">The type of the node to parse.</typeparam>
    /// <param name="branch">Assigned to the choosen branch.</param>
    /// <param name="cases">The available branches, keyed by token type.</param>
    /// <param name="default">The default branch if the next token's type isn't a key in <paramref name="cases"/>. If the value <see langword="null"/> when this happens, this parse operation fails.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation Switch<T>(
        out Branch<T> branch,
        IReadOnlyDictionary<TokenType, Branch<T>> cases,
        Branch<T>? @default = null);

    /// <summary>
    /// Fork based on a previously chosen branch.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the result creator. Pass it to <see cref="MapResult"/> to retrieve the parse result.</param>
    /// <param name="branch">The branch to execute.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation Fork<T>(out ResultCreator<T> result, Branch<T> branch);

    /// <summary>
    /// Get a node based on the tokens read so far; the parsing operation can continue.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the created result.</param>
    /// <param name="resultCreator">The result creator function.</param>
    /// <remarks>This method is the equivalent of <see cref="Parse{T}(out T, Parser{T})"/>, but without a parser.</remarks>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation Get<T>(out T result, ResultCreator<T> resultCreator);

    /// <summary>
    /// Retrieve the final result.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="resultCreator">The result creator function.</param>
    /// <returns>An Ok result containing the parsed node, or a failure result with a error indicating what went wrong.</returns>
    public abstract ParseResult<T> MapResult<T>(ResultCreator<T> resultCreator);

    /// <summary>Parse a single node.</summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the parsed result.</param>
    /// <param name="parse">The node parser.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    /// <remarks><paramref name="parse"/>'s failure fails this parsing operation.</remarks>
    public abstract ParseOperation Parse<T>(out T result, Parser<T> parse);

    /// <summary>Parse one or more separated node.</summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the list of parsed nodes.</param>
    /// <param name="parse">The node parser.</param>
    /// <param name="separator">The separator token.</param>
    /// <param name="end">The end token.</param>
    /// <param name="readEndToken">To read the end token.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken = true, bool allowTrailingSeparator = false);

    /// <summary>Parse one or more nodes until an end token is encountered.</summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the list of parsed nodes.</param>
    /// <param name="parse">The node parser.</param>
    /// <param name="endTokens">The set of end tokens.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenTypeSet endTokens);

    /// <summary>Parse a node, optionally.</summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the parsed node option.</param>
    /// <param name="parse">The node parser.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseOptional<T>(out Option<T> result, Parser<T> parse);

    /// <summary>Parse a node, optionally.</summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the parsed node option.</param>
    /// <param name="block">The block to execute to get each node.</param>
    /// <param name="parse">The node parser.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseOptional<T>(out Option<T> result, ParseBlock<T> parse);

    /// <summary>Parse a token, optionally.</summary>
    /// <param name="type">The type of token to parse.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseOptionalToken(TokenType type);

    /// <summary>Parse a token.</summary>
    /// <param name="type">The type of token to parse.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseToken(TokenType type);

    /// <summary>Parse a contextual keyword, by expecting an identifier of the defined name.</summary>
    /// <param name="contextKeyword">The contextual keyword to parse.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseContextKeyword(TokenType.ContextKeyword contextKeyword);

    /// <summary>Parse a token's value.</summary>
    /// <param name="result">Assigned to the parsed token's value.</param>
    /// <param name="type">The type of token to parse.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseTokenValue(out string result, TokenType type);

    /// <summary>Parse zero or more separated nodes.</summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="allowTrailingSeparator">To allow a trailing separator.</param>
    /// <param name="separator">The separator token.</param>
    /// <param name="end">The end token.</param>
    /// <param name="readEndToken">To read the end token.</param>
    /// <param name="allowTrailingSeparator">To allow a trailing separator.</param>
    /// <param name="parse">The node parser.</param>
    /// <param name="result">Assigned to the list of parsed nodes.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken = true, bool allowTrailingSeparator = false);

    /// <summary>Parse zero or more nodes until an end token is encountered.</summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the list of parsed nodes.</param>
    /// <param name="parse">The node parser.</param>
    /// <param name="endTokens">The set of end tokens.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenTypeSet endTokens);

    /// <summary>Parse zero ore more nodes until an end token is encountered.</summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="result">Assigned to the list of parsed nodes.</param>
    /// <param name="block">The block to execute to get each node.</param>
    /// <param name="endTokens">The set of end tokens.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyList<ParseResult<T>> result, ParseBlock<T> block, TokenTypeSet endTokens);

    /// <summary>
    /// Skims <paramref name="n"/> tokens.
    /// </summary>
    /// <param name="n">The number of token to skim.</param>
    /// <returns>The current <see cref="ParseOperation"/>.</returns>
    public abstract ParseOperation Skim(int n);

    sealed class FailedOperation(IEnumerable<Token> tokens, int readCount, ParseError error) : ParseOperation(tokens, readCount)
    {
        readonly ParseError _error = error;

        public override ParseOperation Switch<T>(out Branch<T> branch, IReadOnlyDictionary<TokenType, Branch<T>> cases, Branch<T>? @default)
        {
            branch = default!;
            return this;
        }

        public override ParseOperation Fork<T>(out ResultCreator<T> resultCreator, Branch<T> branch)
        {
            resultCreator = default!;
            return this;
        }

        public override ParseOperation Get<T>(out T result, ResultCreator<T> resultCreator)
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

        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken, bool allowTrailingSeparator)
        {
            result = default!;
            return this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenTypeSet endTokens)
        {
            result = default!;
            return this;
        }

        public override ParseOperation ParseOptional<T>(out Option<T> result, Parser<T> parse)
        {
            result = default!;
            return this;
        }

        public override ParseOperation ParseOptionalToken(TokenType type) => this;
        public override ParseOperation ParseToken(TokenType type) => this;

        public override ParseOperation ParseTokenValue(out string result, TokenType type)
        {
            result = default!;
            return this;
        }

        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken, bool allowTrailingSeparator)
        {
            result = default!;
            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenTypeSet endTokens)
        {
            result = default!;
            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyList<ParseResult<T>> result, ParseBlock<T> block, TokenTypeSet endTokens)
        {
            result = default!;
            return this;
        }

        public override ParseOperation Skim(int n) => this;
        public override ParseOperation ParseOptional<T>(out Option<T> result, ParseBlock<T> parse)
        {
            result = default!;
            return this;
        }

        public override ParseOperation ParseContextKeyword(TokenType.ContextKeyword contextKeyword) => this;
    }

    sealed class SuccessfulSoFarOperation(IEnumerable<Token> tokens, string production) : ParseOperation(tokens, 0)
    {
        readonly string _prod = production;

        ValueOption<Token> Next() => _tokens.ElementAtOrNone(_readCount);

        ParseResult<T> CallParser<T>(Parser<T> parser) => parser(_tokens.Skip(_readCount));

        public override ParseOperation Switch<T>(out Branch<T> branch, IReadOnlyDictionary<TokenType, Branch<T>> cases, Branch<T>? @default)
        {
            var token = Next();
            if (token.HasValue && cases.TryGetValue(token.Value.Type, out branch!)) {
                _readCount++;
                return this;
            } else if (@default is not null) {
                branch = @default;
                return this;
            }
            branch = default!;
            return Fail(ParseError.ForProduction(_prod, token, cases.Keys));
        }

        public override ParseOperation Fork<T>(out ResultCreator<T> result, Branch<T> branch)
        {
            (ParseOperation p, result) = branch(this);
            return p;
        }

        public override ParseOperation Get<T>(out T result, ResultCreator<T> resultCreator)
        {
            result = resultCreator(ReadTokens);
            return this;
        }

        public override ParseResult<T> MapResult<T>(ResultCreator<T> resultCreator) => MakeOkResult(resultCreator(ReadTokens));

        public override ParseOperation Parse<T>(out T result, Parser<T> parse)
        {
            var pr = CallParser(parse);
            _readCount += pr.SourceTokens.Count;
            if (pr.HasValue) {
                result = pr.Value;
                return this;
            } else {
                result = default!;
                return Fail(MakeOurs(pr.Error));
            }
        }

        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken, bool allowTrailingSeparator)
        {
            List<ParseResult<T>> items = [];
            result = items;

            if (Peek(end).ValueOr(true)) {
                // try to parse to get an appropriate error, it should fail since we're on the end token. 
                return Fail(CallParser(parse).Error.NotNull());
            } else {
                do {
                    var item = CallParser(parse);
                    _readCount += item.SourceTokens.Count;
                    items.Add(item);
                } while (Match(separator) && (!allowTrailingSeparator || !Peek(end).ValueOr(true)));
            }

            return readEndToken ? ParseToken(end) : this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenTypeSet endTokens)
        {
            List<ParseResult<T>> items = [];
            result = items;

            ParseResult<T> item = CallParser(parse);
            _readCount += item.SourceTokens.Count;

            items.Add(item);

            if (!item.HasValue) {
                return Fail(MakeOurs(item.Error));
            }

            ParseWhile(items, parse, () => !Peek(endTokens).ValueOr(true));

            return this;
        }

        public override ParseOperation ParseOptional<T>(out Option<T> result, Parser<T> parse)
        {
            var pr = CallParser(parse);
            if (pr.HasValue) {
                _readCount += pr.SourceTokens.Count;
            }
            result = pr.DropError();
            return this;
        }

        public override ParseOperation ParseOptional<T>(out Option<T> result, ParseBlock<T> parse)
        {
            var pr = parse(this);

            if (pr.HasValue) {
                _readCount += pr.SourceTokens.Count;
            }
            result = pr.DropError();
            return this;
        }

        public override ParseOperation ParseOptionalToken(TokenType type)
        {
            var token = Next();
            if (token.HasValue && token.Value.Type == type) {
                _readCount++;
            }
            return this;
        }

        public override ParseOperation ParseToken(TokenType type)
        {
            var token = Next();
            if (token.HasValue && token.Value.Type == type) {
                _readCount++;
                return this;
            }
            return Fail(ParseError.ForTerminal(_prod, token, type));
        }

        public override ParseOperation ParseContextKeyword(TokenType.ContextKeyword contextKeyword)
        {
            var token = Next();
            if (token.HasValue
             && token.Value.Type == TokenType.Valued.Identifier
             && contextKeyword.Names.Contains(token.Value.Value.NotNull())) {
                _readCount++;
                return this;
            }
            return Fail(ParseError.ForContextKeyword(_prod, token, contextKeyword.Names));
        }

        public override ParseOperation ParseTokenValue(out string result, TokenType type)
        {
            var token = Next();
            if (token.HasValue && token.Value.Type == type) {
                _readCount++;
                result = token.Value.Value ?? throw new InvalidOperationException("Parsed token doesn't have a value");
                return this;
            }

            result = null!;
            return Fail(ParseError.ForTerminal(_prod, token, type));
        }

        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenType separator, TokenType end, bool readEndToken, bool allowTrailingSeparator)
        {
            List<ParseResult<T>> items = [];
            result = items;

            if (!Peek(end).ValueOr(true)) {
                do {
                    var item = CallParser(parse);
                    _readCount += item.SourceTokens.Count;
                    items.Add(item);
                } while (Match(separator) && (!allowTrailingSeparator || !Peek(end).ValueOr(true)));
            }

            if (allowTrailingSeparator) {
                ParseOptionalToken(separator);
            }

            return readEndToken ? ParseToken(end) : this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyList<ParseResult<T>> result, Parser<T> parse, TokenTypeSet endTokens)
        {
            List<ParseResult<T>> items = [];
            result = items;

            ParseWhile(items, parse, () => !Peek(endTokens).ValueOr(true));

            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyList<ParseResult<T>> result, ParseBlock<T> block, TokenTypeSet endTokens)
        {
            List<ParseResult<T>> items = [];
            result = items;

            ParseWhile(items, block, () => !Peek(endTokens).ValueOr(true));

            return this;
        }

        public override ParseOperation Skim(int n)
        {
            _readCount += n;
            return this;
        }

        bool Match(TokenType type)
        {
            bool nextIsSeparator = Peek(type).ValueOr(false);
            if (nextIsSeparator) {
                _readCount++;
            }
            return nextIsSeparator;
        }

        FailedOperation Fail(ParseError error) => new(_tokens, _readCount, error);

        ParseResult<T> MakeOkResult<T>(T result) => ParseResult.Ok(new(_tokens, _readCount), result);

        ParseError MakeOurs(ParseError error)
         => _prod == error.FailedProduction
            ? error
            : ParseError.ForProduction(_prod,
                error.ErroneousToken, error.ExpectedTokens, error.FailedProduction);

        ValueOption<bool> Peek(TokenTypeSet types)
         => Next().Map(token => types.Contains(token.Type));

        ValueOption<bool> Peek(TokenType type)
         => Next().Map(token => type.Equals(token.Type));

        void ParseWhile<T>(ICollection<ParseResult<T>> items, Parser<T> parse, Func<bool> keepGoingWhile)
        {
            bool prevFailedWith0SrcTokens = false;
            while (keepGoingWhile()) {
                var prItem = CallParser(parse);
                _readCount += prItem.SourceTokens.Count;

                {
                    var thisFailedWith0SrcTokens = !prItem.HasValue && prItem.SourceTokens.Count == 0;
                    if (prevFailedWith0SrcTokens && thisFailedWith0SrcTokens) {
                        break;
                    }
                    prevFailedWith0SrcTokens = thisFailedWith0SrcTokens;
                }

                items.Add(prItem);
            }
        }

        void ParseWhile<T>(ICollection<ParseResult<T>> items, ParseBlock<T> block, Func<bool> keepGoingWhile)
        {
            if (!keepGoingWhile()) {
                return;
            }

            var prItem = block(this);

            while (keepGoingWhile()) {
                items.Add(prItem);
                prItem = block(this);
            }
        }
    }
}
