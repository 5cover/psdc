
using System.Collections.Immutable;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

sealed record ParseError(
    string FailedProduction,
    Option<Token> ErroneousToken,
    IImmutableSet<TokenType> ExpectedTokens,
    IImmutableSet<string> ExpectedProductions)
{
    public static ParseError ForProduction(string failedProduction, Option<Token> erroneousToken, IEnumerable<TokenType> expectedTokens, string expectedProduction)
     => new(failedProduction, erroneousToken,
            expectedTokens.ToImmutableHashSet(),
            ImmutableHashSet.Create(expectedProduction)); 
            
    public static ParseError ForProduction(string failedProduction, Option<Token> erroneousToken, IEnumerable<TokenType> expectedTokens)
     => new(failedProduction, erroneousToken,
            expectedTokens.ToImmutableHashSet(),
            ImmutableHashSet<string>.Empty);

    public static ParseError ForTerminal(string failedProduction, Option<Token> erroneousToken, TokenType expectedToken)
     => new(failedProduction, erroneousToken,
            ImmutableHashSet.Create(expectedToken),
            ImmutableHashSet<string>.Empty);

    public bool IsEquivalent(ParseError? other) => other is not null
        && other.ErroneousToken.Equals(ErroneousToken)
        && other.ExpectedTokens.SetEquals(ExpectedTokens);

    public ParseError CombineWith(ParseError other)
     => ErroneousToken.HasValue && other.ErroneousToken.HasValue
            ? ErroneousToken.Value.StartIndex > other.ErroneousToken.Value.StartIndex
                ? this
            : ErroneousToken.Value.StartIndex < other.ErroneousToken.Value.StartIndex
                ? other
            : (other with {
                ExpectedTokens = ExpectedTokens.Union(other.ExpectedTokens),
                ExpectedProductions = ExpectedProductions.Union(other.ExpectedProductions),
            })
        : ErroneousToken.HasValue ? this : other;
}
