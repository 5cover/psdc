namespace Scover.Psdc.Messages;

public enum MessageCode
{
    #region Errors
    UnknownToken = 0,
    SyntaxError,
    UndefinedSymbol,
    RedefinedSymbol,
    RedefinedMainProgram,
    MissingMainProgram,
    SignatureMismatch,
    ConstantAssignment,
    CallParameterMismatch,
    ConstantExpressionExpected,
    StructureDuplicateComponent,
    StructureComponentDoesntExist,
    ComponentAccessOfNonStruct,
    SubscriptOfNonArray,
    UnsupportedOperation,
    ExpressionHasWrongType,
    NonIntegerIndex,
    TargetLanguageError,
    ReturnInNonFunction,
    TargetLanguageReservedKeyword,

    CustomError = 999,

    #endregion Errors

    #region Warnings

    DivisionByZero = 1000,
    FloatingPointEquality,

    CustomWarning = 1999,

    #endregion Warnings

    #region Suggestions

    CustomSuggestion = 2999,

    #endregion Suggestions
}
