using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private static ParseResult<string> ParseTokenValue(IEnumerable<Token> tokens, TokenType expectedType)
     => ParseTokenValue(tokens, expectedType, val => val);

    private static ParseResult<T> ParseTokenValue<T>(IEnumerable<Token> tokens, TokenType expectedType, Func<string, T> resultCreator)
    {
        _ = ParseOperation.Start(tokens)
            .ParseToken(expectedType, out var value);
        return value.Map(resultCreator);
    }

    private static ParseResult<T> ParseToken<T>(IEnumerable<Token> tokens, TokenType expectedType, Func<T> resultCreator) => ParseOperation.Start(tokens)
        .ParseToken(expectedType)
    .BuildResult(resultCreator);

    private static List<Token> Take(int count, IEnumerable<Token> tokens) => tokens.Take(count).ToList();

    private static ParseResult<T> ParseEither<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, ParseMethod<T>> parserMap)
     => tokens.FirstOrDefault() is { } firstToken && parserMap.TryGetValue(firstToken.Type, out var parser)
        ? parser(tokens)
        : ParseResult.Fail<T>(Take(1, tokens), ParseError.FromExpectedTokens(parserMap.Keys));

    private static ParseResult<T> ParseEither<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, T> map)
     => tokens.FirstOrDefault() is { } firstToken && map.TryGetValue(firstToken.Type, out var t)
        ? ParseResult.Ok(Take(1, tokens), t)
        : ParseResult.Fail<T>(Take(1, tokens), ParseError.FromExpectedTokens(map.Keys));

    private static ParseResult<TokenType> ParseTokenType(
        IEnumerable<Token> tokens,
        IReadOnlySet<TokenType> allowedTokensTypes)
     => tokens.FirstOrDefault() is { } firstToken && allowedTokensTypes.Contains(firstToken.Type)
        ? ParseResult.Ok(Take(1, tokens), firstToken.Type)
        : ParseResult.Fail<TokenType>(Take(1, tokens), new ParseError(allowedTokensTypes));
}
