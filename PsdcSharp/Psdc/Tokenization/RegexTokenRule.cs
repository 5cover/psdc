using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Scover.Psdc.Tokenization;

sealed class RegexTokenRule(TokenType tokenType, string pattern, RegexOptions flags = RegexOptions.None) : TokenRule
{
    readonly Regex _pattern = new($@"\G{pattern}", flags | RegexOptions.Compiled);
    public TokenType TokenType => tokenType;
    public ValueOption<Lexeme> Extract(string code, int startIndex)
    {
        Match match = _pattern.Match(code, startIndex);
        Debug.Assert(!match.Success || match.Groups.Count == 2);
        return match.Success
            ? new Lexeme(
            tokenType,
            match.Groups[1].Value,
            new(match.Index, match.Length)).Some()
            : default;
    }
}
