using static Scover.Psdc.TokenType;

namespace Scover.Psdc;

internal static class TokenTypeExtensions
{
    public static string? DisplayName(this TokenType type) => type switch {
        CommentMultiline => "multiline comment",
        CommentSingleline => "singleline comment",
        Identifier => "identifier",
        LiteralCharacter => "character literal",
        LiteralInteger => "integer literal",
        LiteralReal => "real literal",
        LiteralString => "string literal",
        Eof => "end of file",
        _ => null
    };

    public static string? Humanize(this TokenType type) => type.DisplayName() ?? type.ToString();
}

internal enum TokenType
{
    CommentMultiline,
    CommentSingleline,
    Identifier,
    LiteralCharacter,
    LiteralInteger,
    LiteralReal,
    LiteralString,

    #region Punctuations

    CloseBrace,
    CloseBracket,
    CloseSquareBracket,
    OpenBrace,
    OpenBracket,
    OpenSquareBracket,
    PunctuationCase,
    PunctuationColon,
    PunctuationComma,
    PunctuationSemicolon,

    #endregion Punctuations

    #region Operators

    OperatorAssignment,
    OperatorMemberAccess,
    OperatorTypeAssignment,

    #region Arithmetic

    OperatorDivide,
    OperatorMinus,
    OperatorModulus,
    OperatorMultiply,
    OperatorPlus,

    #endregion Arithmetic

    #region Logical

    OperatorAnd,
    OperatorNot,
    OperatorOr,

    #endregion Logical

    #region Comparison

    OperatorEqual,
    OperatorGreaterThan,
    OperatorGreaterThanOrEqual,
    OperatorLessThan,
    OperatorLessThanOrEqual,
    OperatorNotEqual,

    #endregion Comparison

    #endregion Operators

    #region Keywords

    KeywordBegin,
    KeywordEnd,
    KeywordIs,
    KeywordProgram,
    KeywordStructure,
    KeywordTypeAlias,

    #region Data

    KeywordArray,
    KeywordConstant,
    KeywordFalse,
    KeywordFrom,
    KeywordTrue,

    #endregion Data

    #region Types

    KeywordInteger,
    KeywordReal,
    KeywordCharacter,
    KeywordString,
    KeywordBoolean,

    #endregion Types

    #region Callables

    KeywordDelivers,
    KeywordFunction,
    KeywordProcedure,
    KeywordReturn,

    #endregion Callables

    #region Builtins

    KeywordLireClavier,
    KeywordEcrireEcran,
    KeywordAssigner,
    KeywordOuvrirAjout,
    KeywordOuvrirEcriture,
    KeywordOuvrirLecture,
    KeywordLire,
    KeywordEcrire,
    KeywordFermer,
    KeywordFdf,

    #endregion Builtins

    #region Control structures

    KeywordDo,
    KeywordElse,
    KeywordElseIf,
    KeywordEndDo,
    KeywordEndIf,
    KeywordEndSwitch,
    KeywordFor,
    KeywordIf,
    KeywordRepeat,
    KeywordStep,
    KeywordSwitch,
    KeywordThen,
    KeywordTo,
    KeywordUntil,
    KeywordWhen,
    KeywordWhile,

    #endregion Control structures

    #region Parameters

    KeywordEntE,
    KeywordEntSortE,
    KeywordEntF,
    KeywordEntSortF,
    KeywordSortE,
    KeywordSortF,

    #endregion Parameters

    #endregion Keywords

    /// <summary>Special token that indicates the end of the token sequence.</summary>
    Eof,
}
