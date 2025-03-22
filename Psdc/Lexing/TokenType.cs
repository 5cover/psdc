namespace Scover.Psdc.Lexing;

public enum TokenType
{
    Eof,

    Ident,

    Array,
    Begin,
    Boolean,
    Character,
    Constant,
    Do,
    Else,
    ElseIf,
    End,
    EndFor,
    EndIf,
    EndSwitch,
    EndWhile,
    False,
    For,
    Function,
    If,
    Integer,
    Out,
    Procedure,
    Program,
    Read,
    Real,
    Return,
    Returns,
    String,
    Hash,
    Structure,
    Switch,
    Then,
    True,
    Trunc,
    Type,
    When,
    WhenOther,
    While,
    Write,

    And,
    Not,
    Or,
    Xor,

    LBrace,
    LBracket,
    LParen,
    RBrace,
    RBracket,
    RParen,

    Arrow,
    Colon,
    ColonEqual,
    Comma,
    Div,
    Dot,
    Semi,
    Eq,
    Equal,
    Ge,
    Gt,
    Le,
    Lt,
    Minus,
    Mod,
    Mul,
    Neq,
    Plus,

    LiteralChar,
    LiteralInt,
    LiteralReal,
    LiteralString,
}
