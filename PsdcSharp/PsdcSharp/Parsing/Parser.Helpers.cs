
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private static ParseResult<T> ParseAnyOf<T>(IEnumerable<Token> tokens,
        ParseMethod<T> firstParser,
        params ParseMethod<T>[] parsers)
    {
        var result = firstParser(tokens);
        var enumParser = parsers.GetGenericEnumerator();

        while (enumParser.MoveNext() && !result.HasValue) {
            result = enumParser.Current(tokens);
        }

        return result;
    }

    private static ParseMethod<T> MakeAlwaysOkParser<T>(int tokenCount, Func<Partition<Token>, T> makeNode) where T : Node
     => tokens => ParseResult.Ok(makeNode(new(tokens, tokenCount)));
    private static ParseMethod<T> MakeAlwaysOkParser<T>(Func<Partition<Token>, string, T> makeNodeWithValue) where T : Node
     => tokens => ParseResult.Ok(makeNodeWithValue(new(tokens, 1), tokens.First().Value.NotNull()));

    private ParseResult<string> ParseTokenValue(IEnumerable<Token> tokens, TokenType expectedType)
     => ParseOperation.Start(_messenger, tokens)
        .ParseTokenValue(out var value, expectedType)
    .MapResult(tokens => value);

    private ParseResult<T> ParseTokenValue<T>(IEnumerable<Token> tokens, TokenType expectedType, Func<Partition<Token>, string, T> resultCreator) => ParseOperation.Start(_messenger, tokens)
        .ParseTokenValue(out var value, expectedType)
    .MapResult(tokens => resultCreator(tokens, value));

    private ParseResult<T> ParseToken<T>(IEnumerable<Token> tokens, TokenType expectedType, ResultCreator<T> resultCreator) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(expectedType)
    .MapResult(resultCreator);

    private static ParseResult<T> ParseByTokenType<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, ParseMethod<T>> parserMap)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && parserMap.TryGetValue(firstToken.Value.Type, out var parser)
            ? parser(tokens)
            : ParseResult.Fail<T>(Partition.Empty(tokens), ParseError.Create<T>(firstToken, parserMap.Keys));
    }

    private static ParseResult<T> GetByTokenType<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, T> map)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && map.TryGetValue(firstToken.Value.Type, out var t)
            ? ParseResult.Ok(new(tokens, 1), t)
            : ParseResult.Fail<T>(Partition.Empty(tokens), ParseError.Create<T>(firstToken, map.Keys));
    }

    private static ParseResult<Token> ParseTokenOfType(
        IEnumerable<Token> tokens,
        IEnumerable<TokenType> allowedTokensTypes)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && allowedTokensTypes.Contains(firstToken.Value.Type)
            ? ParseResult.Ok(new(tokens, 1), firstToken.Value)
            : ParseResult.Fail<Token>(Partition.Empty(tokens), ParseError.Create<Token>(firstToken, allowedTokensTypes));
    }
}
