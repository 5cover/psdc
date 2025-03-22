namespace Scover.Psdc.Lexing;

public readonly record struct Token(FixedRange Position, TokenType Type, object? Value = null);
