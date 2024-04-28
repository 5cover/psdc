using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;

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
    // Content based on original input code
    Func<string, string> Content)
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
            input => $"stray `{input[range]}` in program");

    public static Message ErrorSyntax(Partition<Token> sourceTokens, ParseError error)
     => Create(error.ErroneousToken.Map(Extensions.Yield).ValueOr(sourceTokens), MessageCode.SyntaxError, input => {
         StringBuilder msgContent = new("expected ");

         _ = error.ExpectedTokens.Count switch {
             0 => throw new UnreachableException("tokens set is empty"),
             <= 3 => msgContent.AppendJoin(", ", error.ExpectedTokens),
             _ => msgContent.Append(error.ExpectedProductionName),
         };

         error.ErroneousToken.MatchSome(token => msgContent.Append($", got {token}"));

         return msgContent.ToString();
     });

    public static Message ErrorCantInferTypeOfExpression(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.CantInferType,
        _ => "can't infer type of expression");

    public static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier) where TSymbol : Symbol
     => Create(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        _ => $"undefined {SymbolExtensions.GetKind<TSymbol>()} `{identifier}`");

    public static Message ErrorUndefinedSymbol<TSymbol>(Identifier identifier, Symbol existingSymbol) where TSymbol : Symbol
     => Create(identifier.SourceTokens, MessageCode.UndefinedSymbol,
        _ => $"`{identifier}` is a {existingSymbol.GetKind()}, {SymbolExtensions.GetKind<TSymbol>()} expected");

    public static Message ErrorRedefinedSymbol(Symbol newSymbol, Symbol existingSymbol)
     => Create(newSymbol.Name.SourceTokens, MessageCode.RedefinedSymbol,
        _ => $"{newSymbol.GetKind()} `{existingSymbol.Name}` is a redefinition (a {existingSymbol.GetKind()} already exists)");

    public static Message ErrorRedefinedMainProgram(Node.Declaration.MainProgram mainProgram)
     => Create(mainProgram.SourceTokens, MessageCode.RedefinedMainProgram,
        _ => $"more than one main program");

    public static Message ErrorMissingMainProgram(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.MissingMainProgram,
        _ => "main program missing");

    public static Message ErrorSignatureMismatch<TSymbol>(TSymbol newSig, TSymbol expectedSig) where TSymbol : Symbol
     => Create(newSig.SourceTokens, MessageCode.SignatureMismatch,
        input => $"this signature of {SymbolExtensions.GetKind<TSymbol>()} {newSig.Name} differs from previous signature (`{expectedSig.SourceTokens.GetSourceCode(input)}`)");

    public static Message ErrorCallParameterMismatch(CallNode callNode)
     => Create(callNode.SourceTokens, MessageCode.CallParameterMismatch,
        _ => $"the actual parameters of the call to {SymbolExtensions.GetKind<Symbol.Function>()} {callNode.Name} do not correspond to the formal parameters of the function's signature");

    public static Message ErrorConstantAssignment(Node.Statement.Assignment assignment, Symbol.Constant constant)
     => Create(assignment.SourceTokens, MessageCode.ConstantAssignment,
        _ => $"reassigning constant `{constant.Name}`");

    public static Message ErrorDeclaredInferredTypeMismatch(IEnumerable<Token> sourceTokens,
        EvaluatedType declaredType, EvaluatedType inferredType)
     => Create(sourceTokens, MessageCode.DeclaredInferredTypeMismatch,
        input => $"declared type ({declaredType.GetRepresentation(input)}) differs from inferred type ({inferredType.GetRepresentation(input)})");

    public static Message ErrorExpectedConstantExpression(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.ExpectedConstantExpression,
        _ => "expected constant expression");

    public static Message ErrorStructureDuplicateComponent(IEnumerable<Token> sourceTokens, Identifier componentName)
     => Create(sourceTokens, MessageCode.StructureDuplicateComponent,
        _ => $"duplicate component `{componentName}` in structure");

    public static Message ErrorOutputParameterNeverAssigned(Identifier parameterName)
     => Create(parameterName.SourceTokens, MessageCode.OutputParameterNeverAssigned,
        _ => $"output parameter `{parameterName}` never assigned");

    public static Message ErrorStructureComponentDoesntExist(Node.Expression.Lvalue.ComponentAccess compAccess,
        Option<Identifier> structureName)
     => Create(compAccess.ComponentName.SourceTokens, MessageCode.StructureComponentDoesntExist,
            structureName.Match<Identifier, Func<string, string>>(
                some: structName => _ => $"`{structName}` has no component named `{compAccess.ComponentName}`",
                none: () => _ => $"no component named `{compAccess.ComponentName}` in structure"));

    public static Message ErrrorComponentAccessOfNonStruct(Node.Expression.Lvalue.ComponentAccess compAccess)
     => Create(compAccess.SourceTokens, MessageCode.ComponentAccessOfNonStruct,
        _ => $"request for component `{compAccess.ComponentName}` in something not a structure");

    public static Message ErrorSubscriptOfNonArray(Node.Expression.Lvalue.ArraySubscript arraySub)
     => Create(arraySub.SourceTokens, MessageCode.SubscriptOfNonArray,
        _ => $"subscripted value is not an array");

    public static Message UnsupportedOperandTypesForBinaryOperation(IEnumerable<Token> sourceTokens,
        EvaluatedType operand1Type, EvaluatedType operand2Type)
     => Create(sourceTokens, MessageCode.UnsupportedOperandTypesForBinaryOperation,
        input => $"unsupported operand types for binary operation: `{operand1Type.GetRepresentation(input)}` and `{operand2Type.GetRepresentation(input)}`");
    public static Message WarningDivisionByZero(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.DivisionByZero,
        _ => "division by zero (will cause runtime error)");

    private static Message Create(IEnumerable<Token> involvedTokens, MessageCode code, Func<string, string> content)
     => new(code,
            involvedTokens.Any()
            ? (involvedTokens.First().StartIndex..(involvedTokens.Last().StartIndex + involvedTokens.Last().Length)).Some()
            : Option.None<Range>(),
            content);
}
