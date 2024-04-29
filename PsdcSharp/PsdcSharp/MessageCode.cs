namespace Scover.Psdc;

internal enum MessageCode
{
    #region Errors
    UnknownToken = 1,
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
    UnsupportedOperandTypesForBinaryOperation,
    LiteralWrongType,

    #endregion Errors

    #region Warnings
    DivisionByZero,
    #endregion Warnings
}
