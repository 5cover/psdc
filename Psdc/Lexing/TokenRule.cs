namespace Scover.Psdc.Lexing;

interface TokenRule
{
    TokenType TokenType { get; }

    /// <summary>Attempts to extract a token out of a string.</summary>
    /// <param name="input">The input code.</param>
    /// <param name="startIndex">The index at which the token must start.</param>
    /// <returns>An option wrapping the token that could be extracted.</returns>
    ValueOption<Token> Extract(string input, int startIndex);
}
