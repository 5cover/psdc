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
    OutputParameterNeverAssigned,
    StructureComponentDoesntExist,
    ComponentAccessOfNonStruct,
    SubscriptOfNonArray,
    UnsupportedOperation,
    ConstantExpressionWrongType,

    #endregion Errors

    #region Warnings
    DivisionByZero = 1000,
    #endregion Warnings
}
