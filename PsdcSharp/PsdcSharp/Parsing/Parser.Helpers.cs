using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private ParseResult<string> ParseTokenValue(IEnumerable<Token> tokens, TokenType expectedType)
     => ParseOperation.Start(this, tokens)
        .ParseTokenValue(out var value, expectedType)
    .MapResult(tokens => value);

    private ParseResult<T> ParseTokenValue<T>(IEnumerable<Token> tokens, TokenType expectedType, Func<Partition<Token>, string, T> resultCreator) => ParseOperation.Start(this, tokens)
        .ParseTokenValue(out var value, expectedType)
    .MapResult(tokens => resultCreator(tokens, value));

    private ParseResult<T> ParseToken<T>(IEnumerable<Token> tokens, TokenType expectedType, ResultCreator<T> resultCreator) => ParseOperation.Start(this, tokens)
        .ParseToken(expectedType)
    .MapResult(resultCreator);

    private static ParseResult<T> ParseByTokenType<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, ParseMethod<T>> parserMap)
     => tokens.FirstOrDefault() is { } firstToken && parserMap.TryGetValue(firstToken.Type, out var parser)
        ? parser(tokens)
        : ParseResult.Fail<T>(new(tokens, 1), ParseError.FromExpectedTokens(parserMap.Keys));

    private static ParseResult<T> GetByTokenType<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, T> map)
     => tokens.FirstOrDefault() is { } firstToken && map.TryGetValue(firstToken.Type, out var t)
        ? ParseResult.Ok(new(tokens, 1), t)
        : ParseResult.Fail<T>(new(tokens, 1), ParseError.FromExpectedTokens(map.Keys));

    private static ParseResult<Token> ParseTokenOfType(
        IEnumerable<Token> tokens,
        IEnumerable<TokenType> allowedTokensTypes)
     => tokens.FirstOrDefault() is { } firstToken && allowedTokensTypes.Contains(firstToken.Type)
        ? ParseResult.Ok(new(tokens, 1), firstToken)
        : ParseResult.Fail<Token>(new(tokens, 1), new ParseError(allowedTokensTypes.ToHashSet()));
}
