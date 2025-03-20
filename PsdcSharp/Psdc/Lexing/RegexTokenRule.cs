using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Scover.Psdc.Lexing;

sealed class RegexTokenRule(TokenType tokenType, string pattern, RegexOptions flags = RegexOptions.None) : TokenRule
{
    readonly Regex _pattern = new($@"\G{pattern}", flags | RegexOptions.Compiled);
    public TokenType TokenType { get; } = tokenType;
    public ValueOption<Token> Extract(string code, int startIndex)
    {
        Match match = _pattern.Match(code, startIndex);
        Debug.Assert(!match.Success || match.Groups.Count == 2);
        return match.Success
            ? new Token(
            TokenType,
            match.Groups[1].Value,
            new(match.Index, match.Length)).Some()
            : default;
    }
}
