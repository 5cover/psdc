using static Scover.Psdc.TokenType;

namespace Scover.Psdc.Tokenization;

internal sealed record Token(TokenType Type, string? Value, int StartIndex, int Length)
{
    public Option<string> ErrorMessagePart => Type switch {
        Eof => $"reached {Eof.DisplayName()}".Some(),
        var t when t.DisplayName() is { } displayName
         => (Value is null
            ? $"got {displayName}"
            : $"got {displayName} `{Value}`").Some(),
        _ => Option.None<string>()
    };
}
