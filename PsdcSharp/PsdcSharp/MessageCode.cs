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
    ExpectedConstantExpression,

    #endregion Errors

    #region Warnings
    InputParameterAssignment = 1000,
    DivisionByZero,
    #endregion Warnings
}
