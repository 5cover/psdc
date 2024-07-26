using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;
using static Scover.Psdc.Parsing.Node;
using Scover.Psdc.Language;
using System.Collections.Immutable;

namespace Scover.Psdc.Messages;

public readonly struct Message
{
    Message(Range inputRange, MessageCode code, FuncOrValue<string, string> content)
     => (InputRange, Code, Content) = (inputRange, code, content);

    Message(SourceTokens sourceTokens, MessageCode code, FuncOrValue<string, string> content)
     => (Code, InputRange, Content) = (code, sourceTokens.InputRange, content);

    Message(SourceTokens sourceTokens, MessageCode code, FuncOrValue<string, string> content, IReadOnlyList<string> advicePieces)
     => (Code, InputRange, Content, AdvicePieces) = (code, sourceTokens.InputRange, content, advicePieces);

    public MessageCode Code { get; }
    public FuncOrValue<string, string> Content { get; }

    public IReadOnlyList<string> AdvicePieces { get; } = ImmutableList<string>.Empty;
    public Range InputRange { get; }
    public MessageSeverity Severity {
        get {
            var severity = (MessageSeverity)((int)Code / 1000);
            Debug.Assert(Enum.IsDefined(severity));
            return severity;
        }
    }

    private static FormattableString Quantity(int quantity, string singular, string plural)
     => $"{quantity} {(quantity == 1 ? singular : plural)}";

    internal static Message ErrorCallParameterMismatch(SourceTokens sourceTokens,
        Symbol.Callable callable, IReadOnlyList<string> problems)
     => new(sourceTokens, MessageCode.CallParameterMismatch,
        $"call to {callable.GetKind()} `{callable.Name}` does not correspond to signature",
        problems);

    internal static Message ErrorIndexWrongRank(SourceTokens sourceTokens, int faultyRank, int arrayRank)
     => new(sourceTokens, MessageCode.IndexWrongRank,
        $"index has {Quantity(faultyRank, "dimension", "dimensions")}, but {Quantity(arrayRank, "was", "were")} expected");

    internal static Message ErrorIndexOutOfBounds(SourceTokens sourceTokens, IReadOnlyList<int> faultyIndex, IReadOnlyList<int> arrayLength)
     => new(sourceTokens, MessageCode.IndexOutOfBounds,
        $"index [{string.Join(", ", faultyIndex)}] out of bounds for array [{string.Join(", ", arrayLength)}]");

    internal static Message ErrorConstantAssignment(Statement.Assignment assignment, Symbol.Constant constant)
     => new(assignment.SourceTokens, MessageCode.ConstantAssignment,
        $"reassigning constant `{constant.Name}`");

    internal static Message ErrorUnsupportedInitializer(SourceTokens sourceTokens, EvaluatedType initializerTargetType)
     => new(sourceTokens, MessageCode.UnsupportedInitializer,
        $"unsupported initializer for type `{initializerTargetType}`");

    internal static Message ErrorConstantExpressionExpected(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.ConstantExpressionExpected,
        $"constant expression expected");

    internal static Message ErrorExpressionHasWrongType(SourceTokens sourceTokens,
        EvaluatedType expected, EvaluatedType actual)
     => new(sourceTokens, MessageCode.ExpressionHasWrongType,
        $"can't convert expression of type '{actual}' to '{expected}'");

    internal static Message ErrorMissingMainProgram(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.MissingMainProgram,
        "main program missing");

    internal static Message ErrorTargetLanguageReservedKeyword(SourceTokens sourceTokens, string ident, string targetLanguageName)
     => CreateTargetLanguage(sourceTokens, MessageCode.TargetLanguageReservedKeyword,
        targetLanguageName, $"`{ident}` is a reserved {targetLanguageName} keyword and thus cannot be used as an identifier");

    internal static Message ErrorReturnInNonFunction(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.ReturnInNonFunction,
        "return in something not a function");

    internal static Message ErrorRedefinedMainProgram(Declaration.MainProgram mainProgram)
     => new(mainProgram.SourceTokens, MessageCode.RedefinedMainProgram,
        $"more than one main program");

    internal static Message ErrorRedefinedSymbol(Symbol newSymbol, Symbol existingSymbol)
     => new(newSymbol.Name.SourceTokens, MessageCode.RedefinedSymbol,
        $"{newSymbol.GetKind()} `{existingSymbol.Name}` redefines a {existingSymbol.GetKind()} of the same name");

    internal static Message ErrorSignatureMismatch<TSymbol>(TSymbol newSig, TSymbol expectedSig) where TSymbol : Symbol
     => new(newSig.SourceTokens, MessageCode.SignatureMismatch,
        new(input => $"this signature of {newSig.GetKind()} `{newSig.Name}` differs from previous signature (`{input[expectedSig.SourceTokens.InputRange]}`)"));

    internal static Message ErrorStructureComponentDoesntExist(Expression.Lvalue.ComponentAccess compAccess,
        Identifier? structureName)
     => new(compAccess.ComponentName.SourceTokens, MessageCode.StructureComponentDoesntExist,
            structureName is null
                ? $"no component named `{compAccess.ComponentName}` in structure"
                : $"`{structureName}` has no component named `{compAccess.ComponentName}`");

    internal static Message ErrorStructureDuplicateComponent(SourceTokens sourceTokens, Identifier componentName)
     => new(sourceTokens, MessageCode.StructureDuplicateComponent,
        $"duplicate component `{componentName}` in structure");

    internal static Message ErrorNonIntegerIndex(SourceTokens sourceTokens, EvaluatedType actualIndexType)
     => new(sourceTokens, MessageCode.NonIntegerIndex,
        $"non integer ('{actualIndexType}') array index");

    internal static Message ErrorSubscriptOfNonArray(Expression.Lvalue.ArraySubscript arrSub, EvaluatedType actualArrayType)
     => new(arrSub.SourceTokens, MessageCode.SubscriptOfNonArray,
        $"subscripted value ('{actualArrayType}') is not an array");

    internal static Message ErrorSyntax(SourceTokens sourceTokens, ParseError error)
    {
        StringBuilder msgContent = new("syntax: ");

        if (error.ExpectedProductions.Count > 0) {
            msgContent.Append($"on {error.FailedProduction}: expected ").AppendJoin(" or ", error.ExpectedProductions);
        } else if (sourceTokens.Count > 0) {
            // show expected tokens only if failure token isn't the first, or if we successfully read at least 1 token.
            msgContent.Append($"on {error.FailedProduction}: expected ").AppendJoin(", ", error.ExpectedTokens);
        } else {
            msgContent.Append($"expected {error.FailedProduction}");
        }

        error.ErroneousToken.MatchSome(token => msgContent.Append($", got {token}"));

        return new(error.ErroneousToken
            .Map(t => t.InputRange)
            .ValueOr(sourceTokens.InputRange),
            MessageCode.SyntaxError, msgContent.ToString());
    }

    internal static Message ErrorTargetLanguage(SourceTokens sourceTokens, string targetLanguageName, string content)
     => CreateTargetLanguage(sourceTokens, MessageCode.TargetLanguageError, targetLanguageName, content);

    internal static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier) where TSymbol : Symbol
     => new(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        $"undefined {SymbolExtensions.GetKind<TSymbol>()} `{identifier}`");

    internal static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier, Symbol existingSymbol) where TSymbol : Symbol
     => new(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        $"`{identifier}` is a {existingSymbol.GetKind()}, {SymbolExtensions.GetKind<TSymbol>()} expected");

    internal static Message ErrorUnknownToken(Range inputRange)
     => new(inputRange, MessageCode.UnknownToken,
        new(input => $"stray `{input[inputRange]}` in program"));

    internal static Message ErrorUnsupportedOperation(Expression.BinaryOperation opBin, EvaluatedType leftType, EvaluatedType rightType)
     => new(opBin.SourceTokens, MessageCode.UnsupportedOperation,
        $"unsupported operand types for {opBin.Operator.GetRepresentation()}: '{leftType}' and '{rightType}'");

    internal static Message ErrorUnsupportedOperation(Expression.UnaryOperation opUn, EvaluatedType operandType)
     => new(opUn.SourceTokens, MessageCode.UnsupportedOperation,
        $"unsupported operand type for {opUn.Operator.GetRepresentation()}: '{operandType}'");

    internal static Message ErrrorComponentAccessOfNonStruct(Expression.Lvalue.ComponentAccess compAccess, EvaluatedType actualStructType)
     => new(compAccess.SourceTokens, MessageCode.ComponentAccessOfNonStruct,
        $"request for component `{compAccess.ComponentName}` in something ('{actualStructType}') not a structure");

    internal static Message ErrorExcessElementInArrayInitializer(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.ExcessElementInArrayInitializer,
        $"excess element in array initializer");

    internal static string ProblemWrongArgumentMode(Identifier name, string expected, string actual)
     => $"wrong mode for `{name}`: expected '{expected}', got '{actual}'";

    internal static string ProblemWrongArgumentType(Identifier name, EvaluatedType expected, EvaluatedType actual)
     => $"wrong type for `{name}`: expected '{expected}', got '{actual}'";

    internal static string ProblemWrongNumberOfArguments(int expected, int actual)
     => $"expected {Quantity(expected, "argument", "arguments")}, got {actual}";

    internal static Message WarningDivisionByZero(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.DivisionByZero,
        "division by zero will cause runtime error");

    internal static Message WarningFloatingPointEquality(SourceTokens sourceTokens)
     => new(sourceTokens, MessageCode.FloatingPointEquality,
        "floating point equality may be inaccurate",
        ["consider comparing absolute difference to an epsilon value instead"]);

    private static Message CreateTargetLanguage(SourceTokens sourceTokens, MessageCode code,
        string targetLanguageName, string content)
     => new(sourceTokens, code, $"{targetLanguageName}: {content}");
}
