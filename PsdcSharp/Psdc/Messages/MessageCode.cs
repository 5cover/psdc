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
    CharacterLiteralContainsMoreThanOneCharacter,
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
    ExcessElementInInitializer,
    UnsupportedInitializer,
    UnsupportedDesignator,
    TargetLanguageError,
    ReturnInNonReturnable,
    InvalidCast,
    AssertionFailed,
    FeatureNotAvailable,
    ReturnExpectsValue,
    SwitchDefaultIsNotLast,
    CustomError = 999,

    #endregion Errors

    #region Warnings

    DivisionByZero,
    FloatingPointEquality,
    TargetLanguageReservedKeyword,

    CustomWarning = 1999,

    #endregion Warnings

    #region Hints

    ExpressionValueUnused,
    RedundantCast,
    UnofficialFeature,
    CustomHint = 2999,

    #endregion Hint

    #region Debug

    EvaluateExpression,
    EvaluateType,

    #endregion Debug
}
