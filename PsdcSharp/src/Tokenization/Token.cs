namespace Scover.Psdc.Tokenization;

readonly record struct Token(TokenType Type, string? Value, int StartIndex, int Length)
{
    public Range InputRange => StartIndex..(StartIndex + Length);
    public override string ToString()
     => $"{Type} {(Value is null ? "" : $"`{Value}`")}";
}
