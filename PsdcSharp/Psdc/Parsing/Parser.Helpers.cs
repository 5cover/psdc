using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

partial class Parser
{
    static ParseResult<T> GetByTokenType<T>(IEnumerable<Token> tokens, string production, IReadOnlyDictionary<TokenType, T> map)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && map.TryGetValue(firstToken.Value.Type, out var t)
            ? ParseResult.Ok(new(tokens, 1), t)
            : ParseResult.Fail<T>(SourceTokens.Empty, ParseError.ForProduction(production, firstToken, map.Keys));
    }

    static Parser<T> MakeAlwaysOkParser<T>(int tokenCount, Func<SourceTokens, T> makeNode) where T : Node
         => tokens => ParseResult.Ok(makeNode(new(tokens, tokenCount)));

    static Parser<T> MakeAlwaysOkParser<T>(Func<SourceTokens, string, T> makeNodeWithValue) where T : Node
         => tokens => ParseResult.Ok(makeNodeWithValue(new(tokens, 1), tokens.First().Value.NotNull()));

    static ParseResult<T> ParseByTokenType<T>(IEnumerable<Token> tokens, string production, IReadOnlyDictionary<TokenType, Parser<T>> parserMap, Parser<T>? fallback = null)
    {
        var firstToken = tokens.FirstOrNone();
        var error = ParseError.ForProduction(production, firstToken, parserMap.Keys);
        return firstToken.HasValue && parserMap.TryGetValue(firstToken.Value.Type, out var parser)
            ? parser(tokens)
            : fallback?.Invoke(tokens).MapError(e
                 => error.CombineWith(error) with { ExpectedProductions = error.ExpectedProductions })
            ?? ParseResult.Fail<T>(SourceTokens.Empty, error);
    }

    /// <summary>
    /// Returns the result of the first successful parser, or the combined error of all parsers.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="firstParser">The first parser.</param>
    /// <param name="parsers">The other parsers.</param>
    /// <returns>The result of the first successful parsers or the error of all parsers combined.</returns>
    /// <remarks>Parsers should be ordered by decreasing length for maximum munch.</remakrs>
    static Parser<T> ParseFirst<T>(Parser<T> firstParser, params Parser<T>[] parsers)
     => tokens => {
         var result = firstParser(tokens);
         var enumParser = parsers.GetGenericEnumerator();

         while (!result.HasValue && enumParser.MoveNext()) {
             result = enumParser.Current(tokens).MapError(result.Error.CombineWith);
         }

         return result;
     };

    static ParseResult<Token> ParseTokenOfType(
        IEnumerable<Token> tokens,
        string production,
        IEnumerable<TokenType> allowedTokensTypes)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && allowedTokensTypes.Contains(firstToken.Value.Type)
            ? ParseResult.Ok(new(tokens, 1), firstToken.Value)
            : ParseResult.Fail<Token>(SourceTokens.Empty, ParseError.ForProduction(production, firstToken, allowedTokensTypes));
    }

    ParseResult<T> ParseToken<T>(IEnumerable<Token> tokens, TokenType expectedType, ResultCreator<T> resultCreator)
     => ParseOperation.Start(_msger, tokens, expectedType.ToString())
        .ParseToken(expectedType)
    .MapResult(resultCreator);

    ParseResult<T> ParseTokenValue<T>(IEnumerable<Token> tokens, TokenType expectedType, Func<SourceTokens, string, T> resultCreator)
     => ParseOperation.Start(_msger, tokens, expectedType.ToString())
                .ParseTokenValue(out var value, expectedType)
    .MapResult(tokens => resultCreator(tokens, value));
}
