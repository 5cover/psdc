
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{

    private static Parser<T> ParseAnyOf<T>(Parser<T> firstParser, params Parser<T>[] parsers)
     => tokens => {
        var result = firstParser(tokens);
        var enumParser = parsers.GetGenericEnumerator();

        while (enumParser.MoveNext() && !result.HasValue) {
            result = enumParser.Current(tokens);
        }

        return result;
    };

    private static Parser<T> MakeAlwaysOkParser<T>(int tokenCount, Func<SourceTokens, T> makeNode) where T : Node
     => tokens => ParseResult.Ok(makeNode(new(tokens, tokenCount)));
    private static Parser<T> MakeAlwaysOkParser<T>(Func<SourceTokens, string, T> makeNodeWithValue) where T : Node
     => tokens => ParseResult.Ok(makeNodeWithValue(new(tokens, 1), tokens.First().Value.NotNull()));

    private ParseResult<T> ParseTokenValue<T>(IEnumerable<Token> tokens, TokenType expectedType, Func<SourceTokens, string, T> resultCreator) => ParseOperation.Start(_messenger, tokens)
        .ParseTokenValue(out var value, expectedType)
    .MapResult(tokens => resultCreator(tokens, value));

    private ParseResult<T> ParseToken<T>(IEnumerable<Token> tokens, TokenType expectedType, ResultCreator<T> resultCreator) => ParseOperation.Start(_messenger, tokens)
        .ParseToken(expectedType)
    .MapResult(resultCreator);

    private static ParseResult<T> ParseByTokenType<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, Parser<T>> parserMap, Parser<T>? fallback = null)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && parserMap.TryGetValue(firstToken.Value.Type, out var parser)
            ? parser(tokens)
            : fallback?.Invoke(tokens)
            ?? ParseResult.Fail<T>(SourceTokens.Empty, ParseError.Create<T>(firstToken, parserMap.Keys));
    }

    private static ParseResult<T> GetByTokenType<T>(IEnumerable<Token> tokens, IReadOnlyDictionary<TokenType, T> map)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && map.TryGetValue(firstToken.Value.Type, out var t)
            ? ParseResult.Ok(new(tokens, 1), t)
            : ParseResult.Fail<T>(SourceTokens.Empty, ParseError.Create<T>(firstToken, map.Keys));
    }

    private static ParseResult<Token> ParseTokenOfType(
        IEnumerable<Token> tokens,
        IEnumerable<TokenType> allowedTokensTypes)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && allowedTokensTypes.Contains(firstToken.Value.Type)
            ? ParseResult.Ok(new(tokens, 1), firstToken.Value)
            : ParseResult.Fail<Token>(SourceTokens.Empty, ParseError.Create<Token>(firstToken, allowedTokensTypes));
    }
}
