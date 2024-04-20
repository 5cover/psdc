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
    #endregion Errors

    #region Warnings
    InputParameterAssignment = 1000,
    DivisionByZero = 1001,
    #endregion Warnings
}
