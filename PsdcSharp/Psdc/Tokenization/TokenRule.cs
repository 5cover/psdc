namespace Scover.Psdc.Tokenization;

interface TokenRule
{
    /// <summary>Attempts to extract a token out of a string.</summary>
    /// <param name="tokenType">The type of token to extract</param>
    /// <param name="code">The string to tokenize</param>
    /// <param name="startIndex">The index at which the token must start</param>
    /// <returns>An option wrapping the token that could be extracted.</returns>
    Option<Token> TryExtract(TokenType tokenType, string code, int startIndex);
}
