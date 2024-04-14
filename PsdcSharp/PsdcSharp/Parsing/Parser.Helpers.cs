using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private ParseResult<string> ParseTokenValue(IEnumerable<Token> tokens, TokenType expectedType)
     => ParseTokenValue(tokens, expectedType, val => val);

    private ParseResult<T> ParseTokenValue<T>(IEnumerable<Token> tokens, TokenType expectedType, Func<string, T> resultCreator) => ParseOperation.Start(this, tokens)
        .ParseTokenValue(out var value, expectedType)
    .MapResult(() => resultCreator(value));

    private ParseResult<T> ParseToken<T>(IEnumerable<Token> tokens, TokenType expectedType, Func<T> resultCreator) => ParseOperation.Start(this, tokens)
        .ParseToken(expectedType)
    .MapResult(resultCreator);

    private static ParseResult<T> ParseEither<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, ParseMethod<T>> parserMap)
     => tokens.FirstOrDefault() is { } firstToken && parserMap.TryGetValue(firstToken.Type, out var parser)
        ? parser(tokens)
        : ParseResult.Fail<T>(new(tokens, 1), ParseError.FromExpectedTokens(parserMap.Keys));

    private static ParseResult<T> ParseEither<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, T> map)
     => tokens.FirstOrDefault() is { } firstToken && map.TryGetValue(firstToken.Type, out var t)
        ? ParseResult.Ok(new(tokens, 1), t)
        : ParseResult.Fail<T>(new(tokens, 1), ParseError.FromExpectedTokens(map.Keys));

    private static ParseResult<TokenType> ParseTokenOfType(
        IEnumerable<Token> tokens,
        IReadOnlySet<TokenType> allowedTokensTypes)
     => tokens.FirstOrDefault() is { } firstToken && allowedTokensTypes.Contains(firstToken.Type)
        ? ParseResult.Ok(new(tokens, 1), firstToken.Type)
        : ParseResult.Fail<TokenType>(new(tokens, 1), new ParseError(allowedTokensTypes));
}
