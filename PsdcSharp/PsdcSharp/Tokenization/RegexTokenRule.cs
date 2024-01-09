using System.Text.RegularExpressions;

namespace Scover.Psdc.Tokenization;

internal sealed class RegexTokenRule : TokenRule
{
    private readonly Regex _pattern;
    private readonly TokenType _tokenType;

    public RegexTokenRule(TokenType tokenType, string pattern, RegexOptions flags = RegexOptions.None)
     => (_tokenType, _pattern) = (tokenType, new($@"\G{pattern}", flags));

    public Option<Token> TryExtract(string input, int startIndex)
    {
        Match match = _pattern.Match(input, startIndex);
        return match.Success
            ? new Token(
            _tokenType,
            match.Groups[1].Value,
            match.Index,
            match.Length).Some()
            : Option.None<Token>();
    }
}
