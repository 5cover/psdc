namespace Scover.Psdc.Messages;

public enum MessageCode
{
    #region Errors
    UnknownToken,
    SyntaxError,
    UndefinedSymbol,
    RedefinedSymbol,
    RedefinedMainProgram,
    CallableNotDefined,
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
    CannotSwitchOnString,
    NonIntegerIndex,
    IndexOutOfBounds,
    IndexWrongRank,
    ExcessElementInInitializer,
    UnsupportedInitializer,
    UnsupportedDesignator,
    TargetLanguageError,
    ReturnInNonFunction,
    InvalidCast,
    AssertionFailed,
    CustomError = 999,

    #endregion Errors

    #region Warnings

    DivisionByZero,
    FloatingPointEquality,
    TargetLanguageReservedKeyword,

    CustomWarning = 1999,

    #endregion Warnings

    #region Suggestions

    RedundantCast,

    CustomSuggestion = 2999,

    #endregion Suggestions

    #region Debug

    EvaluateExpression,
    EvaluateType,

    #endregion Debug
}
