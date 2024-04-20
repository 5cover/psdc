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
    int StartIndex,
    int EndIndex,
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

    public static Message ErrorUnknownToken(int startIndex, string unknownChars)
     => new(MessageCode.UnknownToken,
            unknownChars.Length, startIndex,
            _ => $"unknown token: '{unknownChars}'");

    public static Message ErrorSyntax<T>(IEnumerable<Token> sourceTokens, ParseError error)
     => Create(sourceTokens, MessageCode.SyntaxError, input => {
         StringBuilder msgContent = new();
         msgContent.Append($"syntax error trying on {typeof(T).Name}: ");

         _ = error.ExpectedTokens.Count switch {
             1 => msgContent.Append($"expected {error.ExpectedTokens.Single().Humanize()}"),
             > 1 => msgContent.Append("expected ").AppendJoin(", ", error.ExpectedTokens.Select(t => t.Humanize())),
             _ => throw new UnreachableException("expected tokens set is empty"),
         };

         msgContent.Append("; ").Append(sourceTokens.LastOrNone().Match(
             token => $"{token.ErrorMessagePart.ValueOr($"got `{input.Substring(token.StartIndex, token.Length)}`")}",
             none: () => "got nothing"));

         return msgContent.ToString();
     });

    public static Message ErrorCantInferTypeOfExpression(IEnumerable<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.CantInferType,
        _ => "can't infer type of expression");

    public static Message ErrorUndefinedSymbol<TSymbol>(IEnumerable<Token> sourceTokens, string symbolName) where TSymbol : Symbol
     => Create(sourceTokens, MessageCode.UndefinedSymbol,
        _ => $"undefined {SymbolExtensions.GetKind<TSymbol>()} in current scope: '{symbolName}'");

    public static Message ErrorRedefinedSymbol(Symbol newSymbol, Symbol existingSymbol)
     => Create(newSymbol.SourceTokens, MessageCode.RedefinedSymbol,
        _ => $"defining {newSymbol.GetKind()} '{existingSymbol.Name}' but a {existingSymbol.GetKind()} of the same name is already defined in the current scope");

    public static Message ErrorRedefinedMainProgram(Node.Declaration.MainProgram mainProgram)
     => Create(mainProgram.SourceTokens, MessageCode.RedefinedMainProgram,
        _ => $"more than one main program ({mainProgram.ProgramName})");

    public static Message ErrorMissingMainProgram()
     => Create(Enumerable.Empty<Token>(), MessageCode.MissingMainProgram,
        _ => "main program missing");

    public static Message ErrorSignatureMismatch<TSymbol>(TSymbol actualSig, TSymbol expectedSig) where TSymbol : Symbol
     => Create(actualSig.SourceTokens, MessageCode.SignatureMismatch,
        input => {
            var lastSourceToken = expectedSig.SourceTokens.Last();
            string expectedSigCode = input[expectedSig.SourceTokens.First().StartIndex..(lastSourceToken.StartIndex + lastSourceToken.Length)];
            return $"this signature of {SymbolExtensions.GetKind<TSymbol>()} {actualSig.Name} differs from previous signature ('{expectedSigCode}')";
        });

    public static Message ErrorCallParameterMismatch(Node.Expression.Call call, Symbol.Function func)
     => Create(call.SourceTokens, MessageCode.CallParameterMismatch,
        input => {
            var lastSourceToken = func.SourceTokens.Last();
            string expectedSigCode = input[func.SourceTokens.First().StartIndex..(lastSourceToken.StartIndex + lastSourceToken.Length)];
            return $"the effective parameters of the call to {SymbolExtensions.GetKind<Symbol.Function>()} {func.Name} do not correspond to the formal parameters of the function's signature ('{expectedSigCode}')";
        });

    public static Message ErrorConstantAssignment(Node.Statement.Assignment assignment, Symbol.Constant constant)
     => Create(assignment.SourceTokens, MessageCode.ConstantAssignment,
        _ => $"reassigning constant '{constant.Name}'");

    public static Message WarningInputParameterAssignment(Node.Statement.Assignment assignment)
     => Create(assignment.SourceTokens, MessageCode.InputParameterAssignment,
        _ => $"reassinging input parameter '{assignment.Target}'");
    public static Message WarningDivisionByZero(Partition<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.DivisionByZero,
        _ => "division by zero (will cause runtime error)");

    private static Message Create(IEnumerable<Token> involvedTokens, MessageCode code, Func<string, string> content)
     => new(code,
            involvedTokens.First().StartIndex,
            involvedTokens.Last().StartIndex + involvedTokens.Last().Length,
            content);
}
