using System.Collections.Immutable;

using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

public sealed record ParseError(
    string FailedProduction,
    Option<Token> ErroneousToken,
    IImmutableSet<TokenType> ExpectedTokens,
    IImmutableSet<string> ExpectedProductions
)
{
    internal static ParseError ForProduction(
        string failedProduction,
        Option<Token> erroneousToken,
        IImmutableSet<TokenType> expectedTokens,
        string expectedProduction
    ) => new(failedProduction, erroneousToken, expectedTokens, ImmutableHashSet.Create(expectedProduction));

    internal static ParseError ForProduction(string failedProduction, Option<Token> erroneousToken, IImmutableSet<TokenType> expectedTokens) =>
        new(failedProduction, erroneousToken, expectedTokens, ImmutableHashSet<string>.Empty);

    internal static ParseError ForProduction(string failedProduction, Option<Token> erroneousToken, TokenType expectedToken) => new(failedProduction,
        erroneousToken, ImmutableHashSet.Create(expectedToken), ImmutableHashSet<string>.Empty);

    internal bool IsEquivalent(ParseError? other) => other is not null
                                                  && other.ErroneousToken.Equals(ErroneousToken)
                                                  && other.ExpectedTokens.SetEquals(ExpectedTokens);

    internal ParseError CombineWith(ParseError other) => ErroneousToken.HasValue && other.ErroneousToken.HasValue
        ? ErroneousToken.Value.Position.Start > other.ErroneousToken.Value.Position.Start ? this
        : ErroneousToken.Value.Position.Start < other.ErroneousToken.Value.Position.Start ? other : other with {
            ExpectedTokens = ExpectedTokens.Union(other.ExpectedTokens), ExpectedProductions = ExpectedProductions.Union(other.ExpectedProductions),
        } : ErroneousToken.HasValue ? this : other;
}
