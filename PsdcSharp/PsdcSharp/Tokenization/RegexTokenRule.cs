using System.Text.RegularExpressions;

namespace Scover.Psdc.Tokenization;

internal sealed class RegexTokenRule : TokenRule
{
    private readonly Regex _pattern;

    public RegexTokenRule(string pattern, RegexOptions flags = RegexOptions.None)
     => _pattern = new($@"\G{pattern}", flags);

    public Option<Token> TryExtract(TokenType tokenType, string code, int startIndex)
    {
        Match match = _pattern.Match(code, startIndex);
        return match.Success
            ? new Token(
            tokenType,
            match.Groups[1].Value,
            match.Index,
            match.Length).Some()
            : Option.None<Token>();
    }
}
