using Scover.Psdc.Messages;

using static Scover.Psdc.Lexing.TokenType;
using static Scover.Psdc.Lexing.TokenType.Valued;

namespace Scover.Psdc.Lexing;

public sealed class Lexer
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

    Lexer(Messenger msger, string input)
    {
        _msger = msger;
        (_code, _sortedlineContinutationIndexes) = PreprocessLineContinuations(input);
    }

    readonly Messenger _msger;
    readonly List<int> _sortedlineContinutationIndexes;
    readonly string _code;
    const int NA_INDEX = -1;

    public static IEnumerable<Token> Lex(Messenger messenger, string input)
    {
        Lexer t = new(messenger, input);

        int i = 0;
        int iInvalidStart = NA_INDEX;

        while (i < t._code.Length) {
            if (char.IsWhiteSpace(t._code[i])) {
                t.ReportInvalidToken(ref iInvalidStart, i++);
                continue;
            }

            var Token = t.Lex(ref i);

            if (Token.HasValue) {
                t.ReportInvalidToken(ref iInvalidStart, i);
                if (!ignoredTokens.Contains(Token.Value.Type)) {
                    yield return Token.Value with { Position = t.GetInputRange(Token.Value.Position.Start, Token.Value.Position.End) };
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

    ValueOption<Token> Lex(ref int offset)
    {
        foreach (var rule in rules) {
            var token = rule.Extract(_code, offset);
            if (token.HasValue) {
                offset += token.Value.Position.Length;
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
        List<int> sortedLineContinuationsIndexes = [];

        int i = 0;
        for (int j = 0; j < input.Length; ++j) {
            if (input[j] == '\\' && j + 1 < input.Length && input[j + 1] == '\n') {
                sortedLineContinuationsIndexes.Add(j++);
            } else {
                preprocessedCode[i++] = input[j];
            }
        }
        return (new(preprocessedCode.AsSpan()[..i]), sortedLineContinuationsIndexes);
    }

    FixedRange GetInputRange(int start, int length)
    {
        const int LineContinuationLen = 2;
        return new(
            start + LineContinuationLen
                * _sortedlineContinutationIndexes.Count(lco => lco < start),
            length == 0 ? 0 : length + LineContinuationLen
                * _sortedlineContinutationIndexes.Count(lco => lco > start && lco < start + length)
        );
    }
}
