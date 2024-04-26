using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal delegate ParseResult<T> ParseMethod<out T>(IEnumerable<Token> tokens);
internal delegate T ResultCreator<T>(Partition<Token> sourceTokens);

internal abstract class ParseOperation
{
    private int _readCount;
    private readonly IEnumerable<Token> _tokens;

    private ParseOperation(IEnumerable<Token> tokens, int readCount)
     => (_tokens, _readCount) = (tokens, readCount);
    public static ParseOperation Start(MessageProvider syntaxErrorReciever, IEnumerable<Token> tokens)
     => new SuccessfulSoFarOperation(tokens, syntaxErrorReciever);

    protected Partition<Token> ReadTokens => new(_tokens, _readCount);

    public ParseResult<T> Branch<T>(Dictionary<TokenType, Func<ParseOperation, ParseResult<T>>> branches)
     => Branch((IReadOnlyDictionary<TokenType, Func<ParseOperation, ParseResult<T>>>)branches);
    public ParseOperation Branch<T>(out T result, Dictionary<TokenType, Func<ParseOperation, ParseResult<T>>> branches)
     => Branch(out result, (IReadOnlyDictionary<TokenType, Func<ParseOperation, ParseResult<T>>>)branches);

    public abstract ParseResult<T> Branch<T>(IReadOnlyDictionary<TokenType, Func<ParseOperation, ParseResult<T>>> branches);
    public abstract ParseOperation Branch<T>(out T result, IReadOnlyDictionary<TokenType, Func<ParseOperation, ParseResult<T>>> branches);

    public abstract ParseResult<T> MapResult<T>(ResultCreator<T> resultCreator);

    public abstract ParseOperation Parse<T>(out T result, ParseMethod<T> parse);
    public abstract ParseOperation ParseOptional<T>(out Option<T> result, ParseMethod<T> parse);
    public abstract ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator);
    public abstract ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens);
    public abstract ParseOperation ParseTokenValue(out string result, TokenType type);
    public abstract ParseOperation ParseToken(TokenType type);
    public abstract ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator);
    public abstract ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens);

    private sealed class SuccessfulSoFarOperation(IEnumerable<Token> tokens, MessageProvider syntaxErrorReciever) : ParseOperation(tokens, 0)
    {
        private readonly MessageProvider _syntaxErrorReciever = syntaxErrorReciever;
        private IEnumerable<Token> ParsingTokens => _tokens.Skip(_readCount);

        public override ParseResult<T> Branch<T>(IReadOnlyDictionary<TokenType, Func<ParseOperation, ParseResult<T>>> branches)
        {
            Option<Token> token = ParsingTokens.FirstOrNone();
            if (token.HasValue) {
                _readCount++;
            }

            return token.HasValue && branches.TryGetValue(token.Value.Type, out var operation)
                ? operation(this)
                : ParseResult.Fail<T>(ReadTokens, ParseError.Create<T>(token, branches.Keys));
        }

        public override ParseOperation Branch<T>(out T result, IReadOnlyDictionary<TokenType, Func<ParseOperation, ParseResult<T>>> branches)
        {
            var prResult = Branch(branches);
            if (prResult.HasValue) {
                result = prResult.Value;
                return this;
            } else {
                result = default!;
                return Fail(prResult.Error);
            }
        }

        public override ParseResult<T> MapResult<T>(ResultCreator<T> resultCreator) => MakeOkResult(resultCreator(ReadTokens));
        public override ParseOperation Parse<T>(out T result, ParseMethod<T> parse)
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

        public override ParseOperation ParseOptional<T>(out Option<T> result, ParseMethod<T> parse)
        {
            var pr = parse(ParsingTokens);
            if (pr.HasValue) {
                _readCount += pr.SourceTokens.Count;
            }
            result = pr.DiscardError();
            return this;
        }

        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator)
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

        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens)
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

        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator)
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

        private void ParseWhile<T>(ICollection<T> items, ParseMethod<T> parse, Func<bool> doKeepGoing, bool doWhile = false)
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

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens)
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
                none: error => _syntaxErrorReciever.AddMessage(Message.ErrorSyntax(item.SourceTokens, error))
            );

        private FailedOperation Fail(ParseError error) => new(_tokens, _readCount, error);

        private Option<bool> NextParsingTokenIsAny(IEnumerable<TokenType> types)
         => ParsingTokens.FirstOrNone().Map(token => types.Contains(token.Type));
        private static bool NextTokenIs(IEnumerable<Token> tokens, TokenType type)
         => tokens.FirstOrNone().Map(token => token.Type == type).ValueOr(false);
    }

    private sealed class FailedOperation(IEnumerable<Token> tokens, int readCount, ParseError error) : ParseOperation(tokens, readCount)
    {
        private readonly ParseError _error = error;

        public override ParseResult<T> Branch<T>(IReadOnlyDictionary<TokenType, Func<ParseOperation, ParseResult<T>>> branches) => ParseResult.Fail<T>(ReadTokens, _error);

        public override ParseOperation Branch<T>(out T result, IReadOnlyDictionary<TokenType, Func<ParseOperation, ParseResult<T>>> branches)
        {
            result = default!;
            return this;
        }

        public override ParseResult<T> MapResult<T>(ResultCreator<T> result) => ParseResult.Fail<T>(ReadTokens, _error);
        public override ParseOperation Parse<T>(out T result, ParseMethod<T> parse)
        {
            result = default!;
            return this;
        }
        public override ParseOperation ParseOptional<T>(out Option<T> result, ParseMethod<T> parse)
        {
            result = Option.None<T>();
            return this;
        }
        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator)
        {
            result = Array.Empty<T>();
            return this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens)
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
        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator)
        {
            result = Array.Empty<T>();
            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens)
        {
            result = Array.Empty<T>();
            return this;
        }
    }
}
