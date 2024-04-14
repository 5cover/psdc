using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal delegate ParseResult<T> ParseMethod<out T>(IEnumerable<Token> tokens);

internal abstract class ParseOperation
{
    private int _readCount;
    private readonly IEnumerable<Token> _tokens;
    private ParseOperation(IEnumerable<Token> tokens, int readCount)
     => (_tokens, _readCount) = (tokens, readCount);
    public static ParseOperation Start(MessageProvider syntaxErrorReciever, IEnumerable<Token> tokens)
     => new SuccessfulSoFarOperation(tokens, syntaxErrorReciever);

    protected Partition<Token> ReadTokens => new(_tokens, _readCount);    

    public abstract ParseResult<T> MapResult<T>(Func<T> result);

    public abstract ParseOperation Parse<T>(out T result, ParseMethod<T> parse);
    public abstract ParseOperation ParseOptional<T>(out Option<T> result, ParseMethod<T> parse);
    public abstract ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator);
    public abstract ParseOperation ParseOneOrMoreUntil<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, Predicate<IEnumerable<Token>> until);
    public abstract ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens);
    public abstract ParseOperation ParseTokenValue(out string result, TokenType type);
    public abstract ParseOperation ParseToken(TokenType type);
    public abstract ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator);
    public abstract ParseOperation ParseZeroOrMoreUntil<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, Predicate<IEnumerable<Token>> until);
    public abstract ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens);

    private sealed class SuccessfulSoFarOperation : ParseOperation
    {
        private readonly MessageProvider _syntaxErrorReciever;
        private IEnumerable<Token> ParsingTokens => _tokens.Skip(_readCount);

        public SuccessfulSoFarOperation(IEnumerable<Token> tokens, MessageProvider syntaxErrorReciever) : base(tokens, 0)
         => _syntaxErrorReciever = syntaxErrorReciever;

        public override ParseResult<T> MapResult<T>(Func<T> result) => MakeOkResult(result());
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

        public override ParseOperation ParseOptional<T>(out Option<T> result, ParseMethod<T> parse) {
            var pr = parse(ParsingTokens);
            if (pr.HasValue) {
                _readCount += pr.SourceTokens.Count;
            }
            result = pr.DiscardError();
            return this;
        }

        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator)
         => ParseOneOrMoreUntil(out result, parse, tokens => !CheckAndConsumeToken(tokens, separator));

        public override ParseOperation ParseOneOrMoreUntil<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, Predicate<IEnumerable<Token>> until)
        {
            List<T> items = new();
            result = items;

            ParseResult<T> item = parse(ParsingTokens);
            _readCount += item.SourceTokens.Count;

            AddOrSyntaxError(items, item);
            bool endReached = until(ParsingTokens);

            if (endReached && !item.HasValue) {
                return Fail(item.Error);
            } else if (!endReached) {
                ParseZeroOrMoreUntil(out var otherItems, parse, until);
                items.AddRange(otherItems);
            }

            return this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens)
         => ParseOneOrMoreUntil(out result, parse, tokens => NextTokenIsAny(tokens, endTokens));

        public override ParseOperation ParseToken(TokenType type)
        {
            Option<Token> token = ParsingTokens.FirstOrNone();
            if (token.HasValue) {
                _readCount++;
            }

            return token.HasValue && token.Value.Type == type
                ? this
                : Fail(ParseError.FromExpectedTokens(type));
        }

        public override ParseOperation ParseTokenValue(out string result, TokenType type)
        {
            Option<Token> token = ParsingTokens.FirstOrNone();
            if (token.HasValue) {
                _readCount++;
            }

            if (token.HasValue && token.Value.Type == type) {
                result = token.Value.Value ?? throw new InvalidOperationException("Parsed token doesn't have a value");
                return this;
            } else {
                result = null!;
                return Fail(ParseError.FromExpectedTokens(type));
            }
        }

        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator)
         => ParseZeroOrMoreUntil(out result, parse, tokens => !CheckAndConsumeToken(tokens, separator));

        public override ParseOperation ParseZeroOrMoreUntil<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, Predicate<IEnumerable<Token>> until)
        {
            List<T> items = new();
            result = items;

            do {
                var item = parse(ParsingTokens);
                _readCount += item.SourceTokens.Count;
                AddOrSyntaxError(items, item);
            } while (!until(ParsingTokens) && ParsingTokens.Any());

            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens)
         => ParseZeroOrMoreUntil(out result, parse, tokens => NextTokenIsAny(tokens, endTokens));

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
                none: error => _syntaxErrorReciever.AddMessage(Message.SyntaxError<T>(item.SourceTokens, error))
            );

        private FailedOperation Fail(ParseError error) => new(_tokens, _readCount, error);

        private static bool NextTokenIsAny(IEnumerable<Token> tokens, IEnumerable<TokenType> types)
         => tokens.FirstOrNone().Map(token => types.Contains(token.Type)).ValueOr(false);
        private static bool NextTokenIs(IEnumerable<Token> tokens, TokenType type)
         => tokens.FirstOrNone().Map(token => token.Type == type).ValueOr(false);
    }

    private sealed class FailedOperation : ParseOperation
    {
        private readonly ParseError _error;

        public FailedOperation(IEnumerable<Token> tokens, int readCount, ParseError error) : base(tokens, readCount)
         => _error = error;

        public override ParseResult<T> MapResult<T>(Func<T> result) => ParseResult.Fail<T>(ReadTokens, _error);
        public override ParseOperation Parse<T>(out T result, ParseMethod<T> parse) {
            result = default!;
            return this;
        }
        public override ParseOperation ParseOptional<T>(out Option<T> result, ParseMethod<T> parse) {
            result = Option.None<T>();
            return this;
        }
        public override ParseOperation ParseOneOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator) {
            result = Array.Empty<T>();
            return this;
        }
        public override ParseOperation ParseOneOrMoreUntil<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, Predicate<IEnumerable<Token>> until) {
            result = Array.Empty<T>();
            return this;
        }
        public override ParseOperation ParseOneOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens) {
            result = Array.Empty<T>();
            return this;
        }
        public override ParseOperation ParseTokenValue(out string result, TokenType type) {
            result = null!;
            return this;
        }
        public override ParseOperation ParseToken(TokenType type) => this;
        public override ParseOperation ParseZeroOrMoreSeparated<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, TokenType separator) {
            result = Array.Empty<T>();
            return this;
        }
        public override ParseOperation ParseZeroOrMoreUntil<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, Predicate<IEnumerable<Token>> until) {
            result = Array.Empty<T>();
            return this;
        }
        public override ParseOperation ParseZeroOrMoreUntilToken<T>(out IReadOnlyCollection<T> result, ParseMethod<T> parse, params TokenType[] endTokens) {
            result = Array.Empty<T>();
            return this;
        }
    }
}
