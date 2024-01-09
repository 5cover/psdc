namespace Scover.Psdc;

internal enum TokenType
{
    CommentMultiline,
    CommentSingleline,
    Identifier,
    LiteralCharacter,
    LiteralInteger,
    LiteralReal,
    LiteralString,

    #region Delimiters

    CloseBrace,
    CloseBracket,
    CloseSquareBracket,
    DelimiterCase,
    DelimiterColon,
    DelimiterSeparator,
    DelimiterTerminator,
    OpenBrace,
    OpenBracket,
    OpenSquareBracket,

    #endregion Delimiters

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
    KeywordOf,
    KeywordTrue,

    #endregion Data

    #region Types

    KeywordInteger,
    KeywordReal,
    KeywordCharacter,
    KeywordString,
    KeywordBoolean,

    #endregion Types

    #region Subroutines

    KeywordDelivers,
    KeywordFunction,
    KeywordProcedure,
    KeywordReturn,

    #endregion Subroutines

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
    KeywordSwitch,
    KeywordThen,
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

    #region Contextual

    KeywordStep,
    KeywordTo,

    #endregion Contextual

    #endregion Keywords
}
