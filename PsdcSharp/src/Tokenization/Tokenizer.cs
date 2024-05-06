using Scover.Psdc.Messages;

using static Scover.Psdc.Tokenization.TokenType;
using static Scover.Psdc.Tokenization.TokenType.Special;
using static Scover.Psdc.Tokenization.TokenType.Valued;

namespace Scover.Psdc.Tokenization;

sealed class Tokenizer
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

    public static IEnumerable<Token> Tokenize(Messenger messenger, string code)
    {
        Tokenizer t = new(messenger, code);

        int index = 0;

        int? invalidStart = null;

        while (index < code.Length) {
            if (char.IsWhiteSpace(code[index])) {
                index++;
                continue;
            }

            Option<Token> token = t.ReadToken(index);

            if (token.HasValue) {
                if (invalidStart is not null) {
                    t.ReportUnknownToken(invalidStart.Value, index);
                    invalidStart = null;
                }
                index += token.Value.Length;
                if (!ignoredTokens.Contains(token.Value.Type)) {
                    yield return token.Value;
                }
            } else {
                invalidStart ??= index;
                index++;
            }
        }

        if (invalidStart is not null) {
            t.ReportUnknownToken(invalidStart.Value, index);
        }

        yield return new Token(Eof, null, index, 0);
    }

    void ReportUnknownToken(int invalidStart, int index)
     => _msger.Report(Message.ErrorUnknownToken(invalidStart..index));

    Option<Token> ReadToken(int offset)
     => rules.Select(t => t.TryExtract(_code, offset)).FirstOrNone(o => o.HasValue);
}
