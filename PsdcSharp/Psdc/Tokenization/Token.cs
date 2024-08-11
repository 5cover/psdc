namespace Scover.Psdc.Tokenization;

public sealed record Token(TokenType Type, string? Value, int StartIndex, int Length)
{
    public Range InputRange => StartIndex..(StartIndex + Length);
    public override string ToString()
     => $"{Type} {(Value is null ? "" : $"`{Value}`")}";
}
