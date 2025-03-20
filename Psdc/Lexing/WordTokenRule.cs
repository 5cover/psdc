namespace Scover.Psdc.Lexing;

sealed class WordTokenRule(TokenType tokenType, string word, StringComparison comparison) : TokenRule
{
    readonly StringTokenRule _stringRule = new(tokenType, word, comparison);

    public string Expected => _stringRule.Expected;
    public TokenType TokenType { get; } = tokenType;

    public ValueOption<Token> Extract(string code, int startIndex)
    {
        int indexAfter = startIndex + Expected.Length;
        return indexAfter > code.Length || indexAfter < code.Length && TokenType.Valued.IsIdentifierChar(code[indexAfter])
            ? default
            : _stringRule.Extract(code, startIndex);
    }
}
