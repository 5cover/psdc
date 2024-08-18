using Scover.Psdc.Messages;

using static Scover.Psdc.Tokenization.TokenType;
using static Scover.Psdc.Tokenization.TokenType.Regular;
using static Scover.Psdc.Tokenization.TokenType.Regular.Valued;

namespace Scover.Psdc.Tokenization;

public sealed class Tokenizer
{
    static IEnumerable<T> GetRules<T>(IEnumerable<Ruled<T>> ruled) where T : TokenRule => ruled.SelectMany(r => r.Rules);
    static IEnumerable<TokenRule> GetRules(IEnumerable<Ruled<TokenRule>> ruled) => ruled.SelectMany(r => r.Rules);

    static readonly HashSet<TokenType> ignoredTokens = [CommentMultiline, CommentSingleline];

    static readonly IReadOnlyList<TokenRule> rules =
        // Variable length
        GetRules(new Ruled<TokenRule>[]{CommentMultiline, CommentSingleline, LiteralReal, LiteralInteger, LiteralString, LiteralCharacter})
        // Maximum munch
        .Concat(GetRules(Keyword.Instances).OrderByDescending(r => r.Expected.Length))
        .Concat(Enumerable.Concat(GetRules(Punctuation.Instances), GetRules(Operator.Instances))
                .OrderByDescending(r => r.Expected.Length))
        // Identifiers after keywords and punctuation/operators (just to be sure that they won't be lexed as identifiers)
        .Concat(Identifier.Rules)
        // Directives start with an unique symbol (#), so they can be wherever, but putting them last is the safest if the syntax changes.
        .Concat(PreprocessorDirective.Instance.Rules)
        .ToArray();

    Tokenizer(Messenger msger, string code) => (_msger, _code) = (msger, code);

    readonly Messenger _msger;
    readonly string _code;

    const int NA = -1;

    public static IEnumerable<Token> Tokenize(Messenger messenger, string code)
    {
        Tokenizer t = new(messenger, code);

        int index = 0;
        int invalidStart = NA;

        while (index < t._code.Length) {
            if (char.IsWhiteSpace(t._code[index])) {
                t.ReportAnyUnknownToken(ref invalidStart, index);
                ++index;
                continue;
            }

            var token = t.ReadToken(ref index);

            if (token.HasValue) {
                t.ReportAnyUnknownToken(ref invalidStart, index);
                if (!ignoredTokens.Contains(token.Value.Type)) {
                    yield return token.Value;
                }
            } else {
                if (invalidStart == NA) {
                    invalidStart = index;
                }
                index++;
            }
        }

        t.ReportAnyUnknownToken(ref invalidStart, index);

        yield return new Token(Eof, null, index, 0);

    }

    void ReportAnyUnknownToken(ref int invalidStart, int index)
    {
        if (invalidStart != NA) {
            _msger.Report(Message.ErrorUnknownToken(invalidStart..index));
            invalidStart = NA;
        }
    }

    ValueOption<Token> ReadToken(ref int offset)
    {
        foreach (var rule in rules) {
            var token = rule.Extract(_code, offset);
            if (token.HasValue) {
                offset += token.Value.Length;
                return token;
            }
        }
        return default;
    }
}
