using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

public enum BinaryOperator
{
    Plus = TokenType.Plus,
    And = TokenType.And,
    Div = TokenType.Div,
    Eq = TokenType.Eq,
    Gt = TokenType.Gt,
    Ge = TokenType.Ge,
    Lt = TokenType.Lt,
    Le = TokenType.Le,
    Mod = TokenType.Mod,
    Mul = TokenType.Mul,
    Neq = TokenType.Neq,
    Or = TokenType.Or,
    Minus = TokenType.Minus,
    Xor = TokenType.Xor,
}
