using Scover.Psdc.Messages;

using static Scover.Psdc.Tokenization.TokenType;
using static Scover.Psdc.Tokenization.TokenType.Special;
using static Scover.Psdc.Tokenization.TokenType.Valued;

namespace Scover.Psdc.Tokenization;

public sealed class Tokenizer
{
    static readonly HashSet<TokenType> ignoredTokens = [CommentMultiline, CommentSingleline];

    static readonly IReadOnlyList<Ruled> rules =
        // Variable length
        new List<Ruled> { CommentMultiline, CommentSingleline, LiteralReal, LiteralInteger, LiteralString, LiteralCharacter, }
    // Maximal munch
    .Concat(Keyword.Instances.OrderByDescending(type => type.Rule.Expected.Length))
    .Concat(Enumerable.Concat<Ruled<StringTokenRule>>(Punctuation.Instances, Operator.Instances)
            .OrderByDescending(type => type.Rule.Expected.Length))
    // Identifiers last
    .Append(Identifier)
    .ToList();

    Tokenizer(Messenger msger, string code) => (_msger, _code) = (msger, code);

    readonly Messenger _msger;
    readonly string _code;

    const int NA = -1;

    public static IEnumerable<Token> Tokenize(Messenger messenger, string code)
    {
        Tokenizer t = new(messenger, code.DiacriticsRemoved());

        int index = 0;
        int invalidStart = NA;

        while (index < t._code.Length) {
            if (char.IsWhiteSpace(t._code[index])) {
                t.ReportAnyUnknownToken(ref invalidStart, index);
                ++index;
                continue;
            }

            Option<Token> token = t.ReadToken(ref index);

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

    Option<Token> ReadToken(ref int offset)
    {
        foreach (var rule in rules) {
            var token = rule.TryExtract(_code, offset);
            if (token.HasValue) {
                offset += token.Value.Length;
                return token;
            }
        }
        return Option.None<Token>();
    }
}
