namespace Scover.Psdc.Lexing;

sealed class StringTokenRule(TokenType tokenType, string expected, StringComparison comparison) : TokenRule
{
    readonly StringComparison _comparison = comparison;

    public TokenType TokenType => tokenType;
    public string Expected => expected;

    public ValueOption<Token> Extract(string input, int startIndex)
     => input.AsSpan()[startIndex..].StartsWith(Expected, _comparison)
            ? new Token(tokenType, null, new(startIndex, Expected.Length)).Some()
            : default;
}
