using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;
using static Scover.Psdc.Parsing.Node;

using Scover.Psdc.StaticAnalysis;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

internal enum MessageSeverity
{
    Error = 0,
    Warning = 1,
    Suggestion = 2,
}

internal sealed record Message(
    MessageCode Code,
    Option<Range> SourceCodeRange,
    string Content)
{
    public MessageSeverity Type {
        get {
            var type = (MessageSeverity)((int)Code / 1000);
            Debug.Assert(Enum.IsDefined(type));
            return type;
        }
    }

    public static Message ErrorUnknownToken(Range range)
     => new(MessageCode.UnknownToken,
            range.Some(),
            $"stray `{Globals.Input[range]}` in program");

    public static Message ErrorSyntax(SourceTokens sourceTokens, ParseError error)
    {
        StringBuilder msgContent = new("expected ");

        _ = error.ExpectedTokens.Count switch {
            0 => throw new UnreachableException("tokens set is empty"),
            <= 3 => msgContent.AppendJoin(", ", error.ExpectedTokens),
            _ => msgContent.Append(error.ExpectedProductionName),
        };

        error.ErroneousToken.MatchSome(token => msgContent.Append($", got {token}"));

        return Create(error.ErroneousToken.Map(Extensions.Yield).ValueOr(sourceTokens), MessageCode.SyntaxError, msgContent.ToString());
    }

    public static Message ErrorCantInferTypeOfExpression(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.CantInferType,
        "can't infer type of expression");

    public static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier) where TSymbol : Symbol
     => Create(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        $"undefined {SymbolExtensions.GetKind<TSymbol>()} `{identifier}`");

    public static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier, Symbol existingSymbol) where TSymbol : Symbol
     => Create(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        $"`{identifier}` is a {existingSymbol.GetKind()}, {SymbolExtensions.GetKind<TSymbol>()} expected");

    public static Message ErrorRedefinedSymbol(Symbol newSymbol, Symbol existingSymbol)
     => Create(newSymbol.Name.SourceTokens, MessageCode.RedefinedSymbol,
        $"{newSymbol.GetKind()} `{existingSymbol.Name}` is a redefinition (a {existingSymbol.GetKind()} already exists)");

    public static Message ErrorRedefinedMainProgram(Declaration.MainProgram mainProgram)
     => Create(mainProgram.SourceTokens, MessageCode.RedefinedMainProgram,
        $"more than one main program");

    public static Message ErrorMissingMainProgram(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.MissingMainProgram,
        "main program missing");

    public static Message ErrorSignatureMismatch<TSymbol>(TSymbol newSig, TSymbol expectedSig) where TSymbol : Symbol
     => Create(newSig.SourceTokens, MessageCode.SignatureMismatch,
        $"this signature of {newSig.GetKind()} `{newSig.Name}` differs from previous signature (`{expectedSig.SourceTokens.SourceCode}`)");

    public static Message ErrorCallParameterMismatch(IEnumerable<Token> sourceTokens,
        CallableSymbol callable, IReadOnlyCollection<string> problems)
    {
        Debug.Assert(problems.Count > 0);
        StringBuilder msgContent = new();
        msgContent.Append($"call to {callable.GetKind()} `{callable.Name}` does not correspond to signature:");

        if (problems.Count == 1) {
            msgContent.AppendLine($" {problems.Single()}");
        } else {
            foreach (var problem in problems) {
                msgContent.AppendLine($"  - {problem}");
            }
        }

        return Create(sourceTokens, MessageCode.CallParameterMismatch, msgContent.ToString());
    }

    public static string ProblemWrongNumberOfArguments(int expected, int actual)
     => $"wrong number of arguments: expected {expected}, got {actual}";
    public static string ProblemWrongArgumentMode(Identifier name, string expected, string actual)
     => $"wrong mode for `{name}`: expected {expected}, got {actual}";
    public static string ProblemWrongArgumentType(Identifier name, EvaluatedType expected, EvaluatedType actual)
     => $"wrong type for `{name}`: expected {expected}, got {actual}";

    public static Message ErrorConstantAssignment(Statement.Assignment assignment, Symbol.Constant constant)
     => Create(assignment.SourceTokens, MessageCode.ConstantAssignment,
        $"reassigning constant `{constant.Name}`");

    public static Message ErrorDeclaredInferredTypeMismatch(IEnumerable<Token> sourceTokens,
        EvaluatedType declaredType, EvaluatedType inferredType)
     => Create(sourceTokens, MessageCode.DeclaredInferredTypeMismatch,
        $"declared type '{declaredType}' differs from inferred type '{inferredType}'");

    public static Message ErrorConstantExpressionExpected(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.ConstantExpressionExpected,
        "constant expression expected");

    public static Message ErrorStructureDuplicateComponent(IEnumerable<Token> sourceTokens, Identifier componentName)
     => Create(sourceTokens, MessageCode.StructureDuplicateComponent,
        $"duplicate component `{componentName}` in structure");

    public static Message ErrorOutputParameterNeverAssigned(Identifier parameterName)
     => Create(parameterName.SourceTokens, MessageCode.OutputParameterNeverAssigned,
        $"output parameter `{parameterName}` never assigned");

    public static Message ErrorStructureComponentDoesntExist(Expression.Lvalue.ComponentAccess compAccess,
        Option<Identifier> structureName)
     => Create(compAccess.ComponentName.SourceTokens, MessageCode.StructureComponentDoesntExist,
            structureName.Match(
                some: structName => $"`{structName}` has no component named `{compAccess.ComponentName}`",
                none: () => $"no component named `{compAccess.ComponentName}` in structure"));

    public static Message ErrrorComponentAccessOfNonStruct(Expression.Lvalue.ComponentAccess compAccess)
     => Create(compAccess.SourceTokens, MessageCode.ComponentAccessOfNonStruct,
        $"request for component `{compAccess.ComponentName}` in something not a structure");

    public static Message ErrorSubscriptOfNonArray(Expression.Lvalue.ArraySubscript arraySub)
     => Create(arraySub.SourceTokens, MessageCode.SubscriptOfNonArray,
        $"subscripted value is not an array");

    public static Message ErrorUnsupportedOperation(Expression.OperationBinary opBin, EvaluatedType operand1Type, EvaluatedType operand2Type)
     => Create(opBin.SourceTokens, MessageCode.UnsupportedOperation,
        $"unsupported operand types for {opBin.Operator.GetRepresentation()}: '{operand1Type}' and '{operand2Type}'");    
    
    public static Message ErrorUnsupportedOperation(Expression.OperationUnary opUn, EvaluatedType operandType)
     => Create(opUn.SourceTokens, MessageCode.UnsupportedOperation,
        $"unsupported operand type for {opUn.Operator.GetRepresentation()}: '{operandType}'");

    public static Message ErrorConstantExpressionWrongType(Expression expr, EvaluatedType expected, EvaluatedType actual)
     => Create(expr.SourceTokens, MessageCode.ConstantExpressionWrongType,
        $"wrong type for literal: expected '{expected}', got '{actual}'");

    public static Message WarningDivisionByZero(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.DivisionByZero,
        "division by zero will cause runtime error");

    private static Message Create(IEnumerable<Token> involvedTokens, MessageCode code, string content)
     => new(code,
            involvedTokens.Any()
            ? (involvedTokens.First().StartIndex..(involvedTokens.Last().StartIndex + involvedTokens.Last().Length)).Some()
            : Option.None<Range>(),
            content);
}
