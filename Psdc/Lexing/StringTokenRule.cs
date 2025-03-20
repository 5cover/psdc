namespace Scover.Psdc.Lexing;

sealed class StringTokenRule(TokenType tokenType, string expected, StringComparison comparison) : TokenRule
{
    readonly StringComparison _comparison = comparison;

    public TokenType TokenType { get; } = tokenType;
    public string Expected { get; } = expected;

    public ValueOption<Token> Extract(string input, int startIndex) => input.AsSpan()[startIndex..].StartsWith(Expected, _comparison)
        ? new Token(TokenType, null, new(startIndex, Expected.Length)).Some()
        : default;
}
