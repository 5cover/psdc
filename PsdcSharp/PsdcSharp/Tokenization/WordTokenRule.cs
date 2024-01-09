namespace Scover.Psdc.Tokenization;

internal sealed class WordTokenRule : TokenRule
{
    private readonly StringTokenRule _stringRule;
    public WordTokenRule(TokenType tokenType, string word) : this(StringComparison.OrdinalIgnoreCase, tokenType, word)
    {
    }
    public WordTokenRule(StringComparison comparison, TokenType tokenType, string word)
     => _stringRule = new StringTokenRule(comparison, tokenType, word);

    public string Expected => _stringRule.Expected;

    public Option<Token> TryExtract(string input, int startIndex)
    {
        int indexAfter = startIndex + Expected.Length;
        return indexAfter < input.Length && (char.IsLetterOrDigit(input, indexAfter) || input[indexAfter] == '_')
            ? Option.None<Token>()
            : _stringRule.TryExtract(input, startIndex);
    }
}
