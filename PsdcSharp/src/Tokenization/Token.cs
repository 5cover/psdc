namespace Scover.Psdc.Tokenization;

internal readonly record struct Token(TokenType Type, string? Value, int StartIndex, int Length)
{
    public override string ToString()
     => $"{Type} {(Value is null ? "" : $"`{Value}`")}";
}
