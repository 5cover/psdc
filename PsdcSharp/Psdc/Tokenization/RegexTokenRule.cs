using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Scover.Psdc.Tokenization;

sealed class RegexTokenRule(TokenType tokenType, string pattern, RegexOptions flags = RegexOptions.None, Func<string, string>? adjustValue = null) : TokenRule
{
    readonly Regex _pattern = new($@"\G{pattern}", flags | RegexOptions.Compiled);
    readonly Func<string, string> _adjustValue = adjustValue ?? (s => s);
    public TokenType TokenType => tokenType;
    public ValueOption<Token> Extract(string code, int startIndex)
    {
        Match match = _pattern.Match(code, startIndex);
        Debug.Assert(!match.Success || match.Groups.Count == 2);
        return match.Success
            ? new Token(
            tokenType,
            _adjustValue(match.Groups[1].Value),
            match.Index,
            match.Length).Some()
            : default;
    }
}
