using System.Collections.Immutable;
using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

partial class Parser
{
    static ParseResult<T> GetByTokenType<T>(IEnumerable<Token> tokens, string production, IReadOnlyDictionary<TokenType, T> map)
    {
        var firstToken = tokens.FirstOrNone();
        return firstToken.HasValue && map.TryGetValue(firstToken.Value.Type, out var t)
            ? ParseResult.Ok(new(tokens, 1), t)
            : ParseResult.Fail<T>(firstToken.Map(t => new SourceTokens(t)).ValueOr(SourceTokens.Empty), ParseError.ForProduction(production, firstToken, map.Keys.ToImmutableHashSet()));
    }

    static Parser<T> ParserReturn<T>(int tokenCount, ResultCreator<T> makeNode) where T : Node => tokens => {
        SourceTokens sourceTokens = new(tokens, tokenCount);
        return ParseResult.Ok(sourceTokens, makeNode(sourceTokens.Location));
    };

    static Parser<T> ParserReturn1<T>(ValuedResultCreator<T> makeNodeWithValue) where T : Node => tokens => {
        SourceTokens sourceTokens = new(tokens, 1);
        return ParseResult.Ok(sourceTokens, makeNodeWithValue(sourceTokens.Location, tokens.First().Value.NotNull()));
    };

    static ParseResult<T> ParseByTokenType<T>(IEnumerable<Token> tokens, string production, IReadOnlyDictionary<TokenType, Parser<T>> parserMap, int index = 0, Parser<T>? fallback = null)
    {
        var keyToken = tokens.ElementAtOrNone(index);
        return keyToken.HasValue && parserMap.TryGetValue(keyToken.Value.Type, out var parser)
            ? parser(tokens)
            : fallback?.Invoke(tokens).MapError(Error().CombineWith)
                ?? ParseResult.Fail<T>(keyToken.Map(t => new SourceTokens(t)).ValueOr(SourceTokens.Empty), Error());

        ParseError Error() => ParseError.ForProduction(production, keyToken, parserMap.Keys.ToImmutableHashSet());
    }

    static ParseResult<T> ParseByIdentifierValue<T>(IEnumerable<Token> tokens, string production, Dictionary<string, Parser<T>> parserMap, int index = 0, Parser<T>? fallback = null)
    {
        var keyToken = tokens.ElementAtOrNone(index);
        return keyToken.HasValue
            && keyToken.Value.Type == TokenType.Valued.Identifier
            && parserMap.TryGetValue(keyToken.Value.Value.NotNull(), out var parser)
                ? parser(tokens)
                : fallback?.Invoke(tokens).MapError(Error().CombineWith)
                    ?? ParseResult.Fail<T>(SourceTokens.Empty, Error());

        ParseError Error() => ParseError.ForContextKeyword(production, keyToken, ImmutableHashSet.CreateRange(parserMap.Keys));
    }

    /// <summary>
    /// Returns the result of the first successful parser, or the combined error of all parsers.
    /// </summary>
    /// <typeparam name="T">The type of node to parse.</typeparam>
    /// <param name="firstParser">The first parser.</param>
    /// <param name="parsers">The other parsers.</param>
    /// <returns>The result of the first successful parsers or the error of all parsers combined.</returns>
    /// <remarks>Parsers should be ordered by decreasing length for maximum munch.</remarks>
    static Parser<T> ParserFirst<T>(Parser<T> firstParser, params Parser<T>[] parsers)
     => tokens => {
         var result = firstParser(tokens);
         var enumParser = parsers.GetGenericEnumerator();

         while (!result.HasValue && enumParser.MoveNext()) {
             result = enumParser.Current(tokens).MapError(result.Error.CombineWith);
         }

         return result;
     };

    static ParseResult<T> ParseToken<T>(IEnumerable<Token> tokens, TokenType expectedType, ResultCreator<T> resultCreator)
     => ParseOperation.Start(tokens, expectedType.ToString())
        .ParseToken(expectedType)
    .MapResult(resultCreator);

    static ParseResult<T> ParseTokenValue<T>(IEnumerable<Token> tokens, TokenType expectedType, ValuedResultCreator<T> resultCreator)
     => ParseOperation.Start(tokens, expectedType.ToString())
                .ParseTokenValue(out var value, expectedType)
    .MapResult(tokens => resultCreator(tokens, value));

    static void AddContextKeyword<T>(Dictionary<string, Parser<T>> parsers, TokenType.ContextKeyword contextKeyword, Parser<T> parser)
    {
        foreach (var name in contextKeyword.Names) {
            parsers.Add(name, parser);
        }
    }
}
