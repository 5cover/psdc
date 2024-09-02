using System.Collections.Immutable;

using Scover.Psdc.Tokenization;

namespace Scover.Psdc.Parsing;

public sealed record ParseError(
    string FailedProduction,
    Option<Token> ErroneousToken,
    IImmutableSet<TokenType> ExpectedTokens,
    IImmutableSet<string> ExpectedProductions)
{
    internal static ParseError ForProduction(string failedProduction, Option<Token> erroneousToken, IEnumerable<TokenType> expectedTokens, string expectedProduction)
     => new(failedProduction, erroneousToken,
            expectedTokens.ToImmutableHashSet(),
            ImmutableHashSet.Create(expectedProduction));

    internal static ParseError ForProduction(string failedProduction, Option<Token> erroneousToken, IEnumerable<TokenType> expectedTokens)
     => new(failedProduction, erroneousToken,
            expectedTokens.ToImmutableHashSet(),
            ImmutableHashSet<string>.Empty);

    internal static ParseError ForProduction(string failedProduction, Option<Token> erroneousToken, TokenType expectedToken)
     => new(failedProduction, erroneousToken,
            ImmutableHashSet.Create(expectedToken),
            ImmutableHashSet<string>.Empty);

    internal static ParseError ForContextKeyword(string failedProduction, Option<Token> erroneousToken, IImmutableSet<string> identifierValues)
     => new(failedProduction, erroneousToken,
            ImmutableHashSet.Create<TokenType>(TokenType.Valued.Identifier),
            identifierValues);

    internal bool IsEquivalent(ParseError? other) => other is not null
        && other.ErroneousToken.Equals(ErroneousToken)
        && other.ExpectedTokens.SetEquals(ExpectedTokens);

    internal ParseError CombineWith(ParseError other)
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
