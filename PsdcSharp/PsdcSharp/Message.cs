using System.Diagnostics;
using System.Text;

using Scover.Psdc.Parsing;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

internal delegate Message ErrorProvider(IReadOnlyCollection<Token> sourceTokens);

internal sealed record Message(
    MessageCode Code,
    MessageSeverity Type,
    int StartIndex,
    int EndIndex,
    string Contents)
{
    public static Message UnknownToken(int startIndex, string unknownChars)
     => new(MessageCode.UnknownToken,
            MessageSeverity.Error,
            unknownChars.Length, startIndex,
            $"Unknown token '{unknownChars}'");

    public static Message SyntaxError<T>(IReadOnlyCollection<Token> sourceTokens, ParseError error)
    {
        StringBuilder msgContents = new($"Syntax error trying to generate {typeof(T).Name}");

        _ = error.ExpectedTokens.Count switch {
            1 => msgContents.Append($" (expected {error.ExpectedTokens.Single()}"),
            > 1 => msgContents.Append(" (expected either (").AppendJoin(", ", error.ExpectedTokens).Append(')'),
            _ => throw new UnreachableException("expected tokens set is empty"),
        };

        if (sourceTokens.FirstOrDefault() is { } firstToken) {
            msgContents.Append($", got {firstToken.Type})");
        }

        return Create(sourceTokens, MessageCode.SyntaxError, MessageSeverity.Error, msgContents.ToString());
    }

    public static Message CantInferType(IReadOnlyCollection<Token> sourceTokens)
     => Create(sourceTokens, MessageCode.CantInferType, MessageSeverity.Error,
            "Can't infer type of expression");

    public static Message UndefinedSymbol<T>(IReadOnlyCollection<Token> sourceTokens, string symbolName)
     => Create(sourceTokens, MessageCode.UndefinedSymbol, MessageSeverity.Error,
            $"Symbol '{symbolName}' ({typeof(T).Name}) is undefined in current scope");

    public static Message RedefinedSymbol<T>(IReadOnlyCollection<Token> sourceTokens, string symbolName)
     => Create(sourceTokens, MessageCode.RedefinedSymbol, MessageSeverity.Error,
            $"Symbol '{symbolName}' ({typeof(T).Name}) is already defined in current scope");

    private static Message Create(IReadOnlyCollection<Token> involvedTokens, MessageCode code, MessageSeverity severity, string content)
     => new(code, severity,
            involvedTokens.First().StartIndex,
            involvedTokens.Last().StartIndex + involvedTokens.Last().Length,
            content);
}

internal enum MessageSeverity
{
    Error,
    Warning,
    Suggestion,
}

internal enum MessageCode
{
    UnknownToken,
    SyntaxError,
    CantInferType,
    UndefinedSymbol,
    RedefinedSymbol,
}
