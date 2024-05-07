using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;
using static Scover.Psdc.Parsing.Node;
using Scover.Psdc.Language;

namespace Scover.Psdc.Messages;

readonly struct Message
{
    Message(MessageCode code, Option<Range> inputRange, string content)
     => (Code, InputRange, Content) = (code, inputRange, content);

    public MessageCode Code { get; }
    public string Content { get; }
    public Option<Range> InputRange { get; }

    public MessageSeverity Severity {
        get {
            var severity = (MessageSeverity)((int)Code / 1000);
            Debug.Assert(Enum.IsDefined(severity));
            return severity;
        }
    }

    public static Message ErrorCallParameterMismatch(SourceTokens sourceTokens,
        CallableSymbol callable, IReadOnlyCollection<string> problems)
    {
        Debug.Assert(problems.Count > 0);
        StringBuilder msgContent = new();
        msgContent.Append($"call to {callable.GetKind()} `{callable.Name}` does not correspond to signature:");

        if (problems.Count == 1) {
            msgContent.Append($" {problems.Single()}");
        } else {
            foreach (var problem in problems) {
                msgContent.AppendLine().Append($"  - {problem}");
            }
        }

        return Create(sourceTokens, MessageCode.CallParameterMismatch, msgContent.ToString());
    }

    public static Message ErrorCantInferTypeOfExpression(SourceTokens sourceTokens)
     => Create(sourceTokens, MessageCode.CantInferType,
        "can't infer type of expression");

    public static Message ErrorConstantAssignment(Statement.Assignment assignment, Symbol.Constant constant)
     => Create(assignment.SourceTokens, MessageCode.ConstantAssignment,
        $"reassigning constant `{constant.Name}`");

    public static Message ErrorConstantExpressionExpected(SourceTokens sourceTokens)
     => Create(sourceTokens, MessageCode.ConstantExpressionExpected,
        $"constant expression expected");

    public static Message ErrorExpressionHasWrongType(SourceTokens sourceTokens, EvaluatedType expected, EvaluatedType actual)
     => Create(sourceTokens, MessageCode.ExpressionHasWrongType,
        $"wrong type for expression: expected '{expected}', got '{actual}'");

    public static Message ErrorMissingMainProgram(SourceTokens sourceTokens)
     => Create(sourceTokens, MessageCode.MissingMainProgram,
        "main program missing");

    public static Message ErrorRedefinedMainProgram(Declaration.MainProgram mainProgram)
     => Create(mainProgram.SourceTokens, MessageCode.RedefinedMainProgram,
        $"more than one main program");

    public static Message ErrorRedefinedSymbol(Symbol newSymbol, Symbol existingSymbol)
     => Create(newSymbol.Name.SourceTokens, MessageCode.RedefinedSymbol,
        $"{newSymbol.GetKind()} `{existingSymbol.Name}` is a redefinition (a {existingSymbol.GetKind()} already exists)");

    public static Message ErrorSignatureMismatch<TSymbol>(TSymbol newSig, TSymbol expectedSig) where TSymbol : Symbol
     => Create(newSig.SourceTokens, MessageCode.SignatureMismatch,
        $"this signature of {newSig.GetKind()} `{newSig.Name}` differs from previous signature (`{Globals.Input[expectedSig.SourceTokens.InputRange]}`)");

    public static Message ErrorStructureComponentDoesntExist(Expression.Lvalue.ComponentAccess compAccess,
        Identifier? structureName)
     => Create(compAccess.ComponentName, MessageCode.StructureComponentDoesntExist,
            structureName is null
                ? $"no component named `{compAccess.ComponentName}` in structure"
                : $"`{structureName}` has no component named `{compAccess.ComponentName}`");

    public static Message ErrorStructureDuplicateComponent(SourceTokens sourceTokens, Identifier componentName)
     => Create(sourceTokens, MessageCode.StructureDuplicateComponent,
        $"duplicate component `{componentName}` in structure");

    public static Message ErrorNonIntegerIndex(SourceTokens sourceTokens, EvaluatedType actualIndexType)
     => Create(sourceTokens, MessageCode.NonIntegerIndex,
        $"non integer ('{actualIndexType}') array index");

    public static Message ErrorSubscriptOfNonArray(Expression.Lvalue.ArraySubscript arrSub, EvaluatedType actualArrayType)
     => Create(arrSub, MessageCode.SubscriptOfNonArray,
        $"subscripted value ('{actualArrayType}') is not an array");

    public static Message ErrorSyntax(SourceTokens sourceTokens, ParseError error)
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

        return Create(error.ErroneousToken
            .Map(t => t.InputRange)
            .ValueOr(sourceTokens.InputRange),
            MessageCode.SyntaxError, msgContent.ToString());
    }

    public static Message ErrorTargetLanguage(string targetLanguageName, SourceTokens sourceTokens, string content)
     => Create(sourceTokens, MessageCode.TargetLanguageError, $"{targetLanguageName}: {content}");

    public static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier) where TSymbol : Symbol
     => Create(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        $"undefined {SymbolExtensions.GetKind<TSymbol>()} `{identifier}`");

    public static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier, Symbol existingSymbol) where TSymbol : Symbol
     => Create(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        $"`{identifier}` is a {existingSymbol.GetKind()}, {SymbolExtensions.GetKind<TSymbol>()} expected");

    public static Message ErrorUnknownToken(Range inputRange)
                                                                         => new(MessageCode.UnknownToken,
            inputRange.Some(),
            $"stray `{Globals.Input[inputRange]}` in program");

    public static Message ErrorUnsupportedOperation(Expression.BinaryOperation opBin, EvaluatedType leftType, EvaluatedType rightType)
     => Create(opBin, MessageCode.UnsupportedOperation,
        $"unsupported operand types for {opBin.Operator.GetRepresentation()}: '{leftType}' and '{rightType}'");

    public static Message ErrorUnsupportedOperation(Expression.UnaryOperation opUn, EvaluatedType operandType)
     => Create(opUn, MessageCode.UnsupportedOperation,
        $"unsupported operand type for {opUn.Operator.GetRepresentation()}: '{operandType}'");

    public static Message ErrrorComponentAccessOfNonStruct(Expression.Lvalue.ComponentAccess compAccess, EvaluatedType actualStructType)
     => Create(compAccess, MessageCode.ComponentAccessOfNonStruct,
        $"request for component `{compAccess.ComponentName}` in something ('{actualStructType}') not a structure");

    public static string ProblemWrongArgumentMode(Identifier name, string expected, string actual)
     => $"wrong mode for `{name}`: expected '{expected}', got '{actual}'";

    public static string ProblemWrongArgumentType(Identifier name, EvaluatedType expected, EvaluatedType actual)
     => $"wrong type for `{name}`: expected '{expected}', got '{actual}'";

    public static string ProblemWrongNumberOfArguments(int expected, int actual)
     => $"wrong number of arguments: expected {expected}, got {actual}";

    public static Message WarningDivisionByZero(SourceTokens sourceTokens)
     => Create(sourceTokens, MessageCode.DivisionByZero,
        "division by zero will cause runtime error");

    public static Message WarningFloatingPointEquality(SourceTokens sourceTokens)
     => Create(sourceTokens, MessageCode.FloatingPointEquality,
        "floating point equality may be inaccurate - consider comparing absolute difference to an epsilon value instead");

    static Message Create(Range inputRange, MessageCode code, string content)
     => new(code,
            inputRange.Some(),
            content);

    static Message Create(SourceTokens sourceTokens, MessageCode code, string content)
     => Create(sourceTokens.InputRange, code, content);

    static Message Create(Node sourceNode, MessageCode code, string content)
 => Create(sourceNode.SourceTokens.InputRange, code, content);
}
