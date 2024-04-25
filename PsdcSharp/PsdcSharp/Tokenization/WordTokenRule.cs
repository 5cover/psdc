namespace Scover.Psdc.Tokenization;

internal sealed class WordTokenRule(string word, StringComparison comparison) : TokenRule
{
    private readonly StringTokenRule _stringRule = new(word, comparison);

    public string Expected => _stringRule.Expected;

    public Option<Token> TryExtract(TokenType tokenType, string input, int startIndex)
    {
        int indexAfter = startIndex + Expected.Length;
        return indexAfter < input.Length && (char.IsLetterOrDigit(input, indexAfter) || input[indexAfter] == '_')
            ? Option.None<Token>()
            : _stringRule.TryExtract(tokenType, input, startIndex);
    }
}
