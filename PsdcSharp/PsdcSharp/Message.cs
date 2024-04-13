using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using Scover.Psdc.Parsing;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

internal delegate Message ErrorProvider(IReadOnlyCollection<Token> sourceTokens);

internal enum MessageSeverity {
    Error = 0,
    Warning = 1,
    Suggestion = 2,
}

internal sealed record Message(
    MessageCode Code,
    int StartIndex,
    int EndIndex,
    // Content based on input
    Func<string, string> Content)
{
    public MessageSeverity Type {
        get {
            var type = (MessageSeverity)((int)Code / 1000);
            Debug.Assert(Enum.IsDefined(type));
            return type;
        }
    }

    public static Message UnknownToken(int startIndex, string unknownChars)
     => new(MessageCode.UnknownToken,
            unknownChars.Length, startIndex,
            _ => $"Unknown token '{unknownChars}'");

    public static Message SyntaxError<T>(IReadOnlyCollection<Token> sourceTokens, ParseError error)
     => Create(sourceTokens, MessageCode.SyntaxError, input => {
         StringBuilder msgContent = new();
         msgContent.Append($"Syntax error trying to generate {typeof(T).Name}: ");

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

    public static Message CantInferType(IReadOnlyCollection<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.CantInferType,
        _ => "Can't infer type of expression");

    public static Message UndefinedSymbol<T>(IReadOnlyCollection<Token> sourceTokens, string symbolName)
     => Create(sourceTokens, MessageCode.UndefinedSymbol,
        _ => $"Symbol '{symbolName}' ({typeof(T).Name}) is undefined in current scope");

    public static Message RedefinedSymbol<T>(IReadOnlyCollection<Token> sourceTokens, string symbolName)
     => Create(sourceTokens, MessageCode.RedefinedSymbol,
        _ => $"Symbol '{symbolName}' ({typeof(T).Name}) is already defined in current scope");

    private static Message Create(IReadOnlyCollection<Token> involvedTokens, MessageCode code, Func<string, string> content)
     => new(code,
            involvedTokens.First().StartIndex,
            involvedTokens.Last().StartIndex + involvedTokens.Last().Length,
            content);
}
