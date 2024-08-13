namespace Scover.Psdc.Tokenization;

sealed class WordTokenRule(TokenType tokenType, string word, StringComparison comparison) : TokenRule
{
    readonly StringTokenRule _stringRule = new(tokenType, word, comparison);

    public string Expected => _stringRule.Expected;
    public TokenType TokenType => tokenType;

    public ValueOption<Token> Extract(string code, int startIndex)
    {
        int indexAfter = startIndex + Expected.Length;
        return indexAfter < code.Length && (char.IsLetterOrDigit(code, indexAfter) || code[indexAfter] == '_')
            ? default
            : _stringRule.Extract(code, startIndex);
    }
}
