namespace Scover.Psdc.Lexing;

public readonly record struct Lexeme(TokenType Type, string? Value, FixedRange CodePosition);

public readonly record struct Token(TokenType Type, string? Value, FixedRange Position)
{
    public override string ToString()
     => $"{Type} {(Value is null ? "" : $"`{Value}`")}";
}
