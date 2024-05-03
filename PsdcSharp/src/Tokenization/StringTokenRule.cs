using System.Diagnostics;

namespace Scover.Psdc.Tokenization;

internal sealed class StringTokenRule : TokenRule
{
    private readonly StringComparison _comparison;
    private readonly string _normalizedExpected;

    public StringTokenRule(string expected, StringComparison comparison)
    {
        (Expected, _normalizedExpected, _comparison) = (expected, expected.RemoveDiacritics(), comparison);
        // Without diacritics should be the same length
        Debug.Assert(_normalizedExpected.Length == Expected.Length);
    }

    public string Expected { get; }

    public Option<Token> TryExtract(TokenType tokenType, string code, int startIndex)
    {
        // Not >= as startIndex is already the index of the first character
        if (startIndex + Expected.Length > code.Length) {
            return Option.None<Token>();
        }

        string target = code.Substring(startIndex, Expected.Length).RemoveDiacritics();

        return target.Equals(_normalizedExpected, _comparison)
            ? new Token(
            tokenType,
            null,
            startIndex,
            Expected.Length).Some()
            : Option.None<Token>();
    }
}
