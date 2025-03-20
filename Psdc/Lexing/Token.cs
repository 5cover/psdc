namespace Scover.Psdc.Lexing;

public readonly record struct Token(TokenType Type, string? Value, LengthRange Position)
{
    public override string ToString() => $"{Type} {(Value is null ? "" : $"`{Value}`")}";
}
