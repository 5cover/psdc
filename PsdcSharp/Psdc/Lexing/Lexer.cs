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
        (_code, _sortedLineContinutationIndexes) = PreprocessLineContinuations(input);
    }

    readonly Messenger _msger;
    readonly List<int> _sortedLineContinutationIndexes;
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

            var token = t.Lex(i);

            if (token.HasValue) {
                t.ReportInvalidToken(ref iInvalidStart, i);
                if (!ignoredTokens.Contains(token.Value.Type)) {
                    yield return token.Value with { Position = t.AdjustLocationForLineContinuations(token.Value.Position.Start, token.Value.Position.Length) };
                }
                i += token.Value.Position.Length;
            } else {
                if (iInvalidStart == NA_INDEX) {
                    iInvalidStart = i;
                }
                i++;
            }
        }

        t.ReportInvalidToken(ref iInvalidStart, i);

        yield return new Token(Eof, null, t.AdjustLocationForLineContinuations(i, 0));
    }

    void ReportInvalidToken(ref int iInvalidStart, int i)
    {
        if (iInvalidStart != NA_INDEX) {
            _msger.Report(Message.ErrorUnknownToken(AdjustLocationForLineContinuations(iInvalidStart, i - iInvalidStart)));
            iInvalidStart = NA_INDEX;
        }
    }

    ValueOption<Token> Lex(int offset)
    {
        foreach (var rule in rules) {
            if (rule.Extract(_code, offset) is { HasValue: true } token) {
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

    LengthRange AdjustLocationForLineContinuations(int start, int length)
    {
        start += MeasureLineContinuations(0, start);
        length += MeasureLineContinuations(start, start + length);
        return new(start, length);
    }

    int MeasureLineContinuations(int start, int end)
    {
        const int LineContinuationLen = 2;

        int iFirstLC = _sortedLineContinutationIndexes.BinarySearch(start);
        // If there's no line continuation after start
        if (~iFirstLC == _sortedLineContinutationIndexes.Count) {
            return 0;
        }
        if (iFirstLC < 0) {
            iFirstLC = ~iFirstLC;
        }
        int iLastLC = iFirstLC;
        for (int i = _sortedLineContinutationIndexes[iFirstLC]; iLastLC < _sortedLineContinutationIndexes.Count && i < end; ++i) {
            if (i == _sortedLineContinutationIndexes[iLastLC]) {
                ++iLastLC;
                end += LineContinuationLen;
            }
        }

        return LineContinuationLen * (iLastLC - iFirstLC);
    }
}
