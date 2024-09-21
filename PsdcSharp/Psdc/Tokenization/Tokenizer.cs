using Scover.Psdc.Messages;

using static Scover.Psdc.Tokenization.TokenType;
using static Scover.Psdc.Tokenization.TokenType.Valued;

namespace Scover.Psdc.Tokenization;

public sealed class Tokenizer
{
    static IEnumerable<T> GetRules<T>(IEnumerable<Ruled<T>> ruled) where T : TokenRule => ruled.SelectMany(r => r.Rules);
    static IEnumerable<TokenRule> GetRules(IEnumerable<Ruled<TokenRule>> ruled) => ruled.SelectMany(r => r.Rules);

    static readonly HashSet<TokenType> ignoredTokens = [CommentMultiline, CommentSingleline];

    static readonly IReadOnlyList<TokenRule> rules =
        // Variable length
        GetRules(new Ruled<TokenRule>[] { CommentMultiline, CommentSingleline, LiteralReal, LiteralInteger, LiteralString, LiteralCharacter })
        // Maximum munch
        .Concat(GetRules(Keyword.Instances).OrderByDescending(r => r.Expected.Length))
        .Concat(GetRules(Punctuation.Instances).OrderByDescending(r => r.Expected.Length))
        // Identifiers after keywords and punctuation/operators (just to be sure that they won't be lexed as identifiers)
        .Concat(Identifier.Rules)
        .ToArray();

    Tokenizer(Messenger msger, string input)
    {
        _msger = msger;
        (_code, _lineContIndexes) = PreprocessLineContinuations(input);
    }

    readonly Messenger _msger;
    readonly List<int> _lineContIndexes;
    readonly string _code;
    const int NA_INDEX = -1;

    public static IEnumerable<Token> Tokenize(Messenger messenger, string input)
    {
        Tokenizer t = new(messenger, input);

        int i = 0;
        int iInvalidStart = NA_INDEX;

        while (i < t._code.Length) {
            if (char.IsWhiteSpace(t._code[i])) {
                t.ReportInvalidToken(ref iInvalidStart, i);
                ++i;
                continue;
            }

            var lexeme = t.Lex(ref i);

            if (lexeme.HasValue) {
                t.ReportInvalidToken(ref iInvalidStart, i);
                if (!ignoredTokens.Contains(lexeme.Value.Type)) {
                    yield return new Token(lexeme.Value.Type, lexeme.Value.Value, t.GetInputRange(lexeme.Value.CodePosition));
                }
            } else {
                if (iInvalidStart == NA_INDEX) {
                    iInvalidStart = i;
                }
                i++;
            }
        }

        t.ReportInvalidToken(ref iInvalidStart, i);

        yield return new Token(Eof, null, t.GetInputRange(i, 0));
    }

    void ReportInvalidToken(ref int iInvalidStart, int index)
    {
        if (iInvalidStart != NA_INDEX) {
            _msger.Report(Message.ErrorUnknownToken(GetInputRange(iInvalidStart, index - iInvalidStart)));
            iInvalidStart = NA_INDEX;
        }
    }

    ValueOption<Lexeme> Lex(ref int offset)
    {
        foreach (var rule in rules) {
            var token = rule.Extract(_code, offset);
            if (token.HasValue) {
                offset += token.Value.CodePosition.Length;
                return token;
            }
        }
        return default;
    }

    static (string, List<int>) PreprocessLineContinuations(string input)
    {
        if (input.Length == 0) {
            return ("", []);
        }

        var preprocessedCode = new char[input.Length];
        List<int> lineContinuationsIndexes = [];

        int i = 0;
        for (int j = 0; j < input.Length; ++j) {
            if (input[j] == '\\' && j + 1 < input.Length && input[j + 1] == '\n') {
                lineContinuationsIndexes.Add(j++);
            } else {
                preprocessedCode[i++] = input[j];
            }
        }
        return (new(preprocessedCode.AsSpan()[..i]), lineContinuationsIndexes);
    }

    const int LineContLen = 2;

    FixedRange GetInputRange(FixedRange range) => GetInputRange(range.Start, range.Length);
    FixedRange GetInputRange(int start, int length) => new(
        start + LineContLen * _lineContIndexes.Count(lco => lco < start),
        length == 0 ? 0 : length + LineContLen * _lineContIndexes.Count(lco => lco > start && lco < start + length)
    );
}
