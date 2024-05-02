namespace Scover.Psdc;

internal enum MessageCode
{
    #region Errors
    UnknownToken = 0,
    SyntaxError,
    CantInferType,
    UndefinedSymbol,
    RedefinedSymbol,
    RedefinedMainProgram,
    MissingMainProgram,
    SignatureMismatch,
    ConstantAssignment,
    CallParameterMismatch,
    DeclaredInferredTypeMismatch,
    ConstantExpressionExpected,
    StructureDuplicateComponent,
    StructureComponentDoesntExist,
    ComponentAccessOfNonStruct,
    SubscriptOfNonArray,
    UnsupportedOperation,
    ConstantExpressionWrongType,
    TargetLanguageError,

    CustomError = 999,

    #endregion Errors

    #region Warnings

    DivisionByZero = 1000,

    CustomWarning = 1999,
    
    #endregion Warnings

    #region Suggestions

    CustomSuggestion = 2999,

    #endregion Suggestions
}
