using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal delegate ParseResult<T> ParseMethod<out T>(IEnumerable<Token> tokens);

internal abstract class ParseOperation
{
    private ParseOperation() { }
    public static ParseOperation Start(IEnumerable<Token> tokens) => new SuccessfulSoFarOperation(tokens);

    public abstract ParseResult<T> BuildResult<T>(Func<T> buildResult);
    public abstract ParseResult<T> FlattenResult<T>(Func<ParseResult<T>> buildResult);
    public abstract ParseOperation Parse<T>(ParseMethod<T> parse, out ParseResult<T> result);
    public abstract ParseOperation ParseOneOrMoreSeparated<T>(ParseMethod<T> parse, TokenType separator, out IReadOnlyCollection<ParseResult<T>> items);
    public abstract ParseOperation ParseOneOrMoreUntil<T>(ParseMethod<T> parse, Predicate<IEnumerable<Token>> until, out IReadOnlyCollection<ParseResult<T>> items);
    public abstract ParseOperation ParseOneOrMoreUntilToken<T>(ParseMethod<T> parse, out IReadOnlyCollection<ParseResult<T>> items, params TokenType[] endTokens);
    public abstract ParseOperation ParseToken(TokenType type, out ParseResult<string> value);
    public abstract ParseOperation ParseToken(TokenType type);
    public abstract ParseOperation ParseZeroOrMoreSeparated<T>(ParseMethod<T> parse, TokenType separator, out IReadOnlyCollection<ParseResult<T>> items);
    public abstract ParseOperation ParseZeroOrMoreUntil<T>(ParseMethod<T> parse, Predicate<IEnumerable<Token>> until, out IReadOnlyCollection<ParseResult<T>> items);
    public abstract ParseOperation ParseZeroOrMoreUntilToken<T>(ParseMethod<T> parse, out IReadOnlyCollection<ParseResult<T>> items, params TokenType[] endTokens);

    private sealed class SuccessfulSoFarOperation : ParseOperation
    {
        private readonly List<Token> _readTokens = new();
        private readonly IEnumerable<Token> _tokens;
        private IEnumerable<Token> ParsingTokens => _tokens.Skip(_readTokens.Count);
        private Token NextParsingToken => ParsingTokens.First();

        public SuccessfulSoFarOperation(IEnumerable<Token> tokens) => _tokens = tokens;

        public override ParseResult<T> BuildResult<T>(Func<T> buildResult) => MakeOkResult(buildResult());
        public override ParseResult<T> FlattenResult<T>(Func<ParseResult<T>> buildResult) => buildResult().WithSourceTokens(_readTokens);

        public override ParseOperation Parse<T>(ParseMethod<T> parse, out ParseResult<T> result)
        {
            result = parse(ParsingTokens);
            _readTokens.AddRange(result.SourceTokens);
            return this;
        }

        public override ParseOperation ParseOneOrMoreSeparated<T>(ParseMethod<T> parse, TokenType separator, out IReadOnlyCollection<ParseResult<T>> items)
        {
            List<ParseResult<T>> itemsList = new();

            ParseResult<T> first = parse(ParsingTokens);
            _readTokens.AddRange(first.SourceTokens);

            if (!first.HasValue) {
                items = Array.Empty<ParseResult<T>>();
                return new ErroneousOperation(_readTokens, first.Error);
            }

            itemsList.Add(first);

            while (NextParsingTokenIs(separator)) {
                _readTokens.Add(NextParsingToken);
                ParseResult<T> item = parse(ParsingTokens);
                _readTokens.AddRange(item.SourceTokens);
                itemsList.Add(item);
            }

            items = itemsList;
            return this;
        }

        public override ParseOperation ParseOneOrMoreUntil<T>(ParseMethod<T> parse, Predicate<IEnumerable<Token>> until, out IReadOnlyCollection<ParseResult<T>> items)
        {
            List<ParseResult<T>> itemsList = new();

            ParseResult<T> first = parse(ParsingTokens);
            _readTokens.AddRange(first.SourceTokens);

            if (!first.HasValue) {
                items = Array.Empty<ParseResult<T>>();
                return new ErroneousOperation(_readTokens, first.Error);
            }

            itemsList.Add(first);
            itemsList.AddRange(ParseZeroOrMoreUntilImpl(parse, until));

            // An empty collection may still be returned without an error if the until condition is reached before parsing the first item.
            // That's not a huge problem since semantically, if the until condition is reached, the parsing should also fail, so we'll get an error.
            items = itemsList;
            return this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(ParseMethod<T> parse, out IReadOnlyCollection<ParseResult<T>> items, params TokenType[] endTokens)
            => ParseOneOrMoreUntil(parse, tokens => FirstTokenIsAny(tokens, endTokens), out items);

        public override ParseOperation ParseToken(TokenType type)
        {
            Option<Token> token = ParsingTokens.FirstOrNone();
            token.MatchSome(_readTokens.Add);

            return token.HasValue && token.Value.Type == type
                ? this
                : new ErroneousOperation(_readTokens, ParseError.FromExpectedTokens(type));
        }

        public override ParseOperation ParseToken(TokenType type, out ParseResult<string> value)
        {
            Option<Token> token = ParsingTokens.FirstOrNone();
            token.MatchSome(_readTokens.Add);

            value = token.HasValue && token.Value.Type == type
                ? MakeOkResult(token.Value.Value.NotNull())
                : ParseResult.Fail<string>(_readTokens, ParseError.FromExpectedTokens(type));

            return this;

        }

        public override ParseOperation ParseZeroOrMoreSeparated<T>(ParseMethod<T> parse, TokenType separator, out IReadOnlyCollection<ParseResult<T>> items)
        {
            List<ParseResult<T>> itemsList = new();
            items = itemsList;

            ParseResult<T> first = parse(ParsingTokens);
            _readTokens.AddRange(first.SourceTokens);

            if (!first.HasValue) {
                return this;
            }

            itemsList.Add(first);

            while (NextParsingTokenIs(separator)) {
                _readTokens.Add(NextParsingToken);
                ParseResult<T> item = parse(ParsingTokens);
                _readTokens.AddRange(item.SourceTokens);
                itemsList.Add(item);
            }
            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntil<T>(ParseMethod<T> parse, Predicate<IEnumerable<Token>> until, out IReadOnlyCollection<ParseResult<T>> items)
        {
            items = ParseZeroOrMoreUntilImpl(parse, until).ToList();
            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(ParseMethod<T> parse, out IReadOnlyCollection<ParseResult<T>> items, params TokenType[] endTokens)
         => ParseZeroOrMoreUntil(parse, tokens => FirstTokenIsAny(tokens, endTokens), out items);

        private IEnumerable<ParseResult<T>> ParseZeroOrMoreUntilImpl<T>(ParseMethod<T> parse, Predicate<IEnumerable<Token>> until)
        {
            while (!NextParsingTokenIs(TokenType.Eof)
             && !until(ParsingTokens)
            // We stop when SourceTokens is empty to guard against infinite loops.
            // Semantically if SourceTokens is an empty collection it means that we're out of tokens to parse.
             && parse(ParsingTokens) is { SourceTokens.Count: > 0 } item
            // But still check it as a failsafe against infinite loops.
             && ParsingTokens.Any()) {
                _readTokens.AddRange(item.SourceTokens);
                yield return item;
            }
        }

        private static bool FirstTokenIsAny(IEnumerable<Token> tokens, IEnumerable<TokenType> types)
         => tokens.FirstOrNone().Map(token => types.Contains(token.Type)).ValueOr(false);

        private ParseResult<T> MakeOkResult<T>(T result) => ParseResult.Ok(_readTokens, result);
        private bool NextParsingTokenIs(TokenType type) => ParsingTokens.FirstOrDefault() is { } token && token.Type == type;
    }

    private sealed class ErroneousOperation : ParseOperation
    {
        private readonly ParseError _error;
        private readonly IReadOnlyCollection<Token> _readTokens;

        public ErroneousOperation(IReadOnlyCollection<Token> readTokens, ParseError error)
         => (_readTokens, _error) = (readTokens, error);

        public override ParseOperation Parse<T>(ParseMethod<T> parse, out ParseResult<T> result)
        {
            result = ParseResult.Fail<T>(_readTokens, _error);
            return this;
        }

        public override ParseResult<T> BuildResult<T>(Func<T> buildResult)
         => ParseResult.Fail<T>(_readTokens, _error);

        public override ParseResult<T> FlattenResult<T>(Func<ParseResult<T>> buildResult)
         => ParseResult.Fail<T>(_readTokens, _error);

        public override ParseOperation ParseOneOrMoreSeparated<T>(ParseMethod<T> parse, TokenType separator, out IReadOnlyCollection<ParseResult<T>> items)
        {
            items = Array.Empty<ParseResult<T>>();
            return this;
        }

        public override ParseOperation ParseOneOrMoreUntil<T>(ParseMethod<T> parse, Predicate<IEnumerable<Token>> until, out IReadOnlyCollection<ParseResult<T>> items)
        {
            items = Array.Empty<ParseResult<T>>();
            return this;
        }

        public override ParseOperation ParseOneOrMoreUntilToken<T>(ParseMethod<T> parse, out IReadOnlyCollection<ParseResult<T>> items, params TokenType[] endTokens)
        {
            items = Array.Empty<ParseResult<T>>();
            return this;
        }

        public override ParseOperation ParseToken(TokenType type) => this;

        public override ParseOperation ParseToken(TokenType type, out ParseResult<string> value)
        {
            value = ParseResult.Fail<string>(_readTokens, _error);
            return this;
        }

        public override ParseOperation ParseZeroOrMoreSeparated<T>(ParseMethod<T> parse, TokenType separator, out IReadOnlyCollection<ParseResult<T>> items) {
            items = Array.Empty<ParseResult<T>>();
            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntil<T>(ParseMethod<T> parse, Predicate<IEnumerable<Token>> until, out IReadOnlyCollection<ParseResult<T>> items)
        {
            items = Array.Empty<ParseResult<T>>();
            return this;
        }

        public override ParseOperation ParseZeroOrMoreUntilToken<T>(ParseMethod<T> parse, out IReadOnlyCollection<ParseResult<T>> items, params TokenType[] endTokens)
        {
            items = Array.Empty<ParseResult<T>>();
            return this;
        }
    }
}
