
using System.Collections.Immutable;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

internal sealed record ParseError(
    Option<Token> ErroneousToken,
    IImmutableSet<TokenType> ExpectedTokens,
    IImmutableSet<string> ExpectedProductions)
{
    public static ParseError ForProduction<T>(Option<Token> erroneousToken, IEnumerable<TokenType> expectedTokens)
     => new(erroneousToken, expectedTokens.ToImmutableHashSet(),
                ImmutableHashSet.Create(typeof(T).Name.ToLower()));

    public static ParseError ForTerminal<T>(Option<Token> erroneousToken, TokenType expectedToken)
     => new(erroneousToken, ImmutableHashSet.Create(expectedToken),
            ImmutableHashSet<string>.Empty);

    public bool IsEquivalent(ParseError? other) => other is not null
        && other.ExpectedTokens.SetEquals(ExpectedTokens);

    public ParseError CombineWith(ParseError other)
     => ErroneousToken.HasValue && other.ErroneousToken.HasValue
        ? ErroneousToken.Value.StartIndex > other.ErroneousToken.Value.StartIndex
            ? this
            : (other with {
                ExpectedTokens = ExpectedTokens.Union(other.ExpectedTokens),
                ExpectedProductions = ExpectedProductions.Union(other.ExpectedProductions),
            })
        : other.ErroneousToken.HasValue ? other : this;
}
