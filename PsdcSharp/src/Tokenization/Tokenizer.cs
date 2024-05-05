
using static Scover.Psdc.Tokenization.TokenType;
using static Scover.Psdc.Tokenization.TokenType.Valued;
using static Scover.Psdc.Tokenization.TokenType.Special;
using Scover.Psdc.Messages;

namespace Scover.Psdc.Tokenization;

sealed class Tokenizer(Messenger messenger, string code)
{
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

    static readonly HashSet<TokenType> ignoredTokens = [CommentMultiline, CommentSingleline];

    readonly string _code = code;

    public IEnumerable<Token> Tokenize()
    {
        int index = 0;

        int? invalidStart = null;

        while (index < _code.Length) {
            if (char.IsWhiteSpace(_code[index])) {
                index++;
                continue;
            }

            Option<Token> token = ReadToken(index);

            if (token.HasValue) {
                if (invalidStart is not null) {
                    AddUnknownTokenMessage(invalidStart.Value, index);
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
            AddUnknownTokenMessage(invalidStart.Value, index);
        }

        yield return new Token(Eof, null, index, 0);
    }

    void AddUnknownTokenMessage(int invalidStart, int index)
     => messenger.Report(Message.ErrorUnknownToken(invalidStart..index));

    Option<Token> ReadToken(int offset)
     => rules.Select(t => t.TryExtract(_code, offset)).FirstOrNone(o => o.HasValue);
}
