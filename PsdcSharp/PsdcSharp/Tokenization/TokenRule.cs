namespace Scover.Psdc.Tokenization;

internal interface TokenRule
{
    /// <summary>Attempts to extract a token out of a string.</summary>
    /// <param name="input">The string to tokenize</param>
    /// <param name="startIndex">The index at which the token must start</param>
    /// <returns>An option wrapping the token that could be extracted.</returns>
    Option<Token> TryExtract(string input, int startIndex);
}
