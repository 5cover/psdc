namespace Scover.Psdc.Lexing;

public enum TokenType
{
    Eof,

    /// <summary>
    /// Value is a <see cref="string"/>.
    /// </summary>
    Ident,

    #region Contextual tokens

    // Those tokens are specially treated identifiers by the lexer
    // Naming convention : PrecedingTokenType . ActualName

    HashAssert,
    HashEval,
    EvalExpr,

    #endregion

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

    /// <summary>
    /// Value is a <see cref="char"/>.
    /// </summary>
    LiteralChar,
    /// <summary>
    /// Value is a <see cref="long"/>.
    /// </summary>
    LiteralInt,
    /// <summary>
    /// Value is a <see cref="decimal"/>.
    /// </summary>
    LiteralReal,
    /// <summary>
    /// Value is a <see cref="string"/>.
    /// </summary>
    LiteralString,
}
