
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal partial class Parser
{
    private static Parser<T> ParseFirst<T>(Parser<T> firstParser, params Parser<T>[] parsers)
     => tokens => {
        var result = firstParser(tokens);
        var enumParser = parsers.GetGenericEnumerator();

        while (!result.HasValue && enumParser.MoveNext()) {
            result = enumParser.Current(tokens).MapError(result.Error.CombineWith);
        }

        return result;
    };

    private static Parser<T> MakeAlwaysOkParser<T>(int tokenCount, Func<SourceTokens, T> makeNode) where T : Node
     => tokens => ParseResult.Ok(makeNode(new(tokens, tokenCount)));
    private static Parser<T> MakeAlwaysOkParser<T>(Func<SourceTokens, string, T> makeNodeWithValue) where T : Node
     => tokens => ParseResult.Ok(makeNodeWithValue(new(tokens, 1), tokens.First().Value.NotNull()));

    private ParseResult<T> ParseTokenValue<T>(IEnumerable<Token> tokens, TokenType expectedType, Func<SourceTokens, string, T> resultCreator) => ParseOperation.Start(_messenger, tokens, expectedType.ToString())
        .ParseTokenValue(out var value, expectedType)
    .MapResult(tokens => resultCreator(tokens, value));

    private ParseResult<T> ParseToken<T>(IEnumerable<Token> tokens, TokenType expectedType, ResultCreator<T> resultCreator) => ParseOperation.Start(_messenger, tokens, expectedType.ToString())
        .ParseToken(expectedType)
    .MapResult(resultCreator);

    private static ParseResult<T> ParseByTokenType<T>(IEnumerable<Token> tokens, string production, IReadOnlyDictionary<TokenType, Parser<T>> parserMap, Parser<T>? fallback = null)
    {
        var firstToken = tokens.FirstOrNone();
        var error = ParseError.ForProduction(production, firstToken, parserMap.Keys);
        return firstToken.HasValue && parserMap.TryGetValue(firstToken.Value.Type, out var parser)
            ? parser(tokens)
            : fallback?.Invoke(tokens).MapError(e
                 => error.CombineWith(error) with { ExpectedProductions = error.ExpectedProductions })
            ?? ParseResult.Fail<T>(SourceTokens.Empty, error);
    }

    private static ParseResult<T> GetByTokenType<T>(IEnumerable<Token> tokens, string production, IReadOnlyDictionary<TokenType, T> map)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && map.TryGetValue(firstToken.Value.Type, out var t)
            ? ParseResult.Ok(new(tokens, 1), t)
            : ParseResult.Fail<T>(SourceTokens.Empty, ParseError.ForProduction(production, firstToken, map.Keys));
    }

    private static ParseResult<Token> ParseTokenOfType(
        IEnumerable<Token> tokens,
        string production,
        IEnumerable<TokenType> allowedTokensTypes)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && allowedTokensTypes.Contains(firstToken.Value.Type)
            ? ParseResult.Ok(new(tokens, 1), firstToken.Value)
            : ParseResult.Fail<Token>(SourceTokens.Empty, ParseError.ForProduction(production, firstToken, allowedTokensTypes));
    }
}
