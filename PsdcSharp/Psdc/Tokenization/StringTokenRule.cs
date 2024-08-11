namespace Scover.Psdc.Tokenization;

sealed class StringTokenRule(TokenType tokenType, string expected, StringComparison comparison) : TokenRule
{
    readonly StringComparison _comparison = comparison;
    
    public TokenType TokenType => tokenType;
    public string Expected => expected;

    public Option<Token> Extract(string input, int startIndex)
     => input.AsSpan()[startIndex..].StartsWith(Expected, _comparison)
            ? new Token(tokenType, null, startIndex, Expected.Length).Some()
            : Option.None<Token>();
}
