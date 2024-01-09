using System.Diagnostics;

namespace Scover.Psdc.Tokenization;

internal sealed class StringTokenRule : TokenRule
{
    private readonly StringComparison _comparison;
    private readonly string _normalizedExpected;
    private readonly TokenType _tokenType;
    public StringTokenRule(TokenType tokenType, string expected) : this(StringComparison.OrdinalIgnoreCase, tokenType, expected)
    {
    }

    public StringTokenRule(StringComparison comparison, TokenType tokenType, string expected)
    {
        (_tokenType, Expected, _normalizedExpected, _comparison) = (tokenType, expected, expected.RemoveDiacritics(), comparison);
        // Without diacritics should be the same length
        Debug.Assert(_normalizedExpected.Length == Expected.Length);
    }
    public string Expected { get; }

    public Option<Token> TryExtract(string input, int startIndex)
    {
        // Not >= as startIndex is already the index of the first character
        if (startIndex + Expected.Length > input.Length) {
            return Option.None<Token>();
        }

        string target = input.Substring(startIndex, Expected.Length).RemoveDiacritics();

        return target.Equals(_normalizedExpected, _comparison)
            ? new Token(
            _tokenType,
            null,
            startIndex,
            Expected.Length).Some()
            : Option.None<Token>();
    }
}
