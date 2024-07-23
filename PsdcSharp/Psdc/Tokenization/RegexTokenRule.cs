using System.Text.RegularExpressions;

namespace Scover.Psdc.Tokenization;

sealed class RegexTokenRule(string pattern, RegexOptions flags = RegexOptions.None) : TokenRule
{
    readonly Regex _pattern = new($@"\G{pattern}", flags | RegexOptions.Compiled);

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
