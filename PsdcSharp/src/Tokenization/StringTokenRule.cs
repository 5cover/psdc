using System.Diagnostics;

namespace Scover.Psdc.Tokenization;

sealed class StringTokenRule : TokenRule
{
    readonly StringComparison _comparison;
    readonly string _normalizedExpected;

    public StringTokenRule(string expected, StringComparison comparison)
    {
        (Expected, _normalizedExpected, _comparison) = (expected, expected.DiacriticsRemoved(), comparison);
        // Without diacritics should be the same length
        Debug.Assert(_normalizedExpected.Length == Expected.Length);
    }

    public string Expected { get; }

    public Option<Token> TryExtract(TokenType tokenType, string input, int startIndex)
    {
        // Not >= as startIndex is already the index of the first character
        if (startIndex + Expected.Length > input.Length) {
            return Option.None<Token>();
        }

        // Assume that the input has already been normalized.
        var target = input.AsSpan().Slice(startIndex, Expected.Length);

        return target.Equals(_normalizedExpected, _comparison)
            ? new Token(
            tokenType,
            null,
            startIndex,
            Expected.Length).Some()
            : Option.None<Token>();
    }
}
