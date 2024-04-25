using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;
using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.SemanticAnalysis;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

internal delegate Message ErrorProvider(IEnumerable<Token> sourceTokens);

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

    public static Message ErrorUndefinedSymbol<TSymbol>(IEnumerable<Token> sourceTokens, string symbolName) where TSymbol : Symbol
     => Create(sourceTokens, MessageCode.UndefinedSymbol,
        _ => $"{SymbolExtensions.GetKind<TSymbol>()} `{symbolName}` undefined in current scope");

    public static Message ErrorRedefinedSymbol(Symbol newSymbol, Symbol existingSymbol)
     => Create(newSymbol.SourceTokens, MessageCode.RedefinedSymbol,
        _ => $"{newSymbol.GetKind()} `{existingSymbol.Name}` is a redefinition (a {existingSymbol.GetKind()} already exists)");

    public static Message ErrorRedefinedMainProgram(Node.Declaration.MainProgram mainProgram)
     => Create(mainProgram.SourceTokens, MessageCode.RedefinedMainProgram,
        _ => $"more than one main program");

    public static Message ErrorMissingMainProgram(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.MissingMainProgram,
        _ => "main program missing");

    public static Message ErrorSignatureMismatch<TSymbol>(TSymbol actualSig, TSymbol expectedSig) where TSymbol : Symbol
     => Create(actualSig.SourceTokens, MessageCode.SignatureMismatch,
        input => $"this signature of {SymbolExtensions.GetKind<TSymbol>()} {actualSig.Name} differs from previous signature (`{expectedSig.SourceTokens.GetSourceCode(input)}`)");

    public static Message ErrorCallParameterMismatch(CallNode callNode)
     => Create(callNode.SourceTokens, MessageCode.CallParameterMismatch,
        _ => $"the effective parameters of the call to {SymbolExtensions.GetKind<Symbol.Function>()} {callNode.Name} do not correspond to the formal parameters of the function's signature");

    public static Message ErrorConstantAssignment(Node.Statement.Assignment assignment, Symbol.Constant constant)
     => Create(assignment.SourceTokens, MessageCode.ConstantAssignment,
        _ => $"reassigning constant `{constant.Name}`");

    public static Message ErrorDeclaredInferredTypeMismatch(IEnumerable<Token> sourceTokens, EvaluatedType declaredType, EvaluatedType inferredType)
     => Create(sourceTokens, MessageCode.DeclaredInferredTypeMismatch,
        input => $"declared type ({declaredType.GetRepresentation(input)}) differs from inferred type ({inferredType.GetRepresentation(input)})");

    public static Message ErrorExpectedConstantExpression(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.ExpectedConstantExpression,
        _ => "expected constant expression");

    public static Message ErrorStructureDuplicateComponent(IEnumerable<Token> sourceTokens, string componentName)
     => Create(sourceTokens, MessageCode.StructureDuplicateComponent,
        _ => $"duplicate component `{componentName}` in structure");

    public static Message WarningInputParameterAssignment(Node.Statement.Assignment assignment)
     => Create(assignment.SourceTokens, MessageCode.InputParameterAssignment,
        _ => $"reassinging input parameter `{assignment.Target}`");
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
