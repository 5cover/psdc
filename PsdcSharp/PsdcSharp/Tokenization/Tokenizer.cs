using System.Text;
using System.Text.RegularExpressions;

using static Scover.Psdc.TokenType;

namespace Scover.Psdc.Tokenization;

internal sealed class Tokenizer : CompilationStep
{
    private static readonly IReadOnlyList<TokenRule> rules =
        RulesBeforeKeywords
        .Concat(KeywordRules.OrderByDescending(rule => rule.Expected.Length))
        .Concat(PunctuationRules.OrderByDescending(rule => rule.Expected.Length))
        .Concat(RulesAfterKeywords)
    .ToList();

    private static readonly IReadOnlySet<TokenType> ignoredTokens = new HashSet<TokenType> { CommentMultiline, CommentSingleline };

    private readonly string _input;

    public Tokenizer(string input) => _input = input;

    private static IEnumerable<TokenRule> RulesBeforeKeywords => new List<RegexTokenRule> {
        new(CommentMultiline, @"/\*(.*?)\*/", RegexOptions.Singleline),
        new(CommentSingleline, "//(.*)$", RegexOptions.Multiline),
        new(LiteralReal, @"(\d*\.\d+)"),
        new(LiteralInteger, @"(\d+)"),
        new(LiteralString, @"""(.*?)"""),
        new(LiteralCharacter, "'(.)'"),
    };

    private static IEnumerable<StringTokenRule> PunctuationRules => new List<StringTokenRule> {
        // Delimiters
        new(OpenBracket, "("),
        new(CloseBracket, ")"),
        new(OpenSquareBracket, "["),
        new(CloseSquareBracket, "]"),
        new(OpenBrace, "{"),
        new(CloseBrace, "}"),
        new(PunctuationComma, ","),
        new(PunctuationSemicolon, ";"),
        new(PunctuationCase, "=>"),
        new(PunctuationColon, ":"),

        // Arithmetic operators
        new(OperatorPlus, "+"),
        new(OperatorMinus, "-"),
        new(OperatorMultiply, "*"),
        new(OperatorDivide, "/"),
        new(OperatorModulus, "%"),

        // Comparison operators
        new(OperatorEqual, "=="),
        new(OperatorNotEqual, "!="),
        new(OperatorLessThan, "<"),
        new(OperatorLessThanOrEqual, "<="),
        new(OperatorGreaterThan, ">"),
        new(OperatorGreaterThanOrEqual, ">="),

        // Other operators
        new(OperatorAssignment, ":="),
        new(OperatorTypeAssignment, "="),
        new(OperatorMemberAccess, "."),
    };

    private static IEnumerable<WordTokenRule> KeywordRules => new List<WordTokenRule> {
        // Logical operators
        new(OperatorAnd, "ET"),
        new(OperatorOr, "OU"),
        new(OperatorNot, "NON"),

        // General keywords
        new(KeywordBegin, "début"),
        new(KeywordEnd, "fin"),
        new(KeywordIs, "c'est"),
        new(KeywordProgram, "programme"),
        new(KeywordStructure, "structure"),
        new(KeywordTypeAlias, "type"),

        // Data keywords
        new(KeywordArray, "tableau"),
        new(KeywordConstant, "constante"),
        new(KeywordFalse, "faux"),
        new(KeywordFrom, "de"),
        new(KeywordTrue, "vrai"),

        // Type keywords
        new(KeywordCharacter, "caractère"),
        new(KeywordInteger, "entier"),
        new(KeywordReal, "réel"),
        new(KeywordString, "chaîne"),
        new(KeywordBoolean, "booléen"),

        // Subroutine keywords
        new(KeywordDelivers, "délivre"),
        new(KeywordFunction, "fonction"),
        new(KeywordProcedure, "procédure"),
        new(KeywordReturn, "retourne"),

        // Builtins keywords
        // Builtins are case-sensitive.
        new(StringComparison.Ordinal, KeywordAssigner, "assigner"),
        new(StringComparison.Ordinal, KeywordEcrire, "écrire"),
        new(StringComparison.Ordinal, KeywordEcrireEcran, "écrireÉcran"),
        new(StringComparison.Ordinal, KeywordFdf, "FdF"),
        new(StringComparison.Ordinal, KeywordFermer, "fermer"),
        new(StringComparison.Ordinal, KeywordLire, "lire"),
        new(StringComparison.Ordinal, KeywordLireClavier, "lireClavier"),
        new(StringComparison.Ordinal, KeywordOuvrirAjout, "ouvrirAjout"),
        new(StringComparison.Ordinal, KeywordOuvrirEcriture, "ouvrirÉcriture"),
        new(StringComparison.Ordinal, KeywordOuvrirLecture, "ouvrirLecture"),

        // Control structures keywords
        new(KeywordDo, "faire"),
        new(KeywordElse, "sinon"),
        new(KeywordElseIf, "sinonsi"),
        new(KeywordEndDo, "finfaire"),
        new(KeywordEndIf, "finsi"),
        new(KeywordEndSwitch, "finselon"),
        new(KeywordFor, "pour"),
        new(KeywordIf, "si"),
        new(KeywordRepeat, "répéter"),
        new(KeywordStep, "pas"),
        new(KeywordSwitch, "selon"),
        new(KeywordThen, "alors"),
        new(KeywordTo, "à"),
        new(KeywordUntil, "jusqu'à"),
        new(KeywordWhen, "quand"),
        new(KeywordWhile, "tant que"),

        // Parameter keywords
        new(KeywordEntE, "entE"),
        new(KeywordEntSortE, "entE/sortE"),
        new(KeywordEntF, "entF"),
        new(KeywordEntSortF, "entF/sortF"),
        new(KeywordSortE, "sortE"),
        new(KeywordSortF, "sortF"),
    };

    private static IEnumerable<TokenRule> RulesAfterKeywords => new List<RegexTokenRule> { new(Identifier, @"([\p{L}_][\p{L}_0-9]*)") };

    public IEnumerable<Token> Tokenize()
    {
        int offset = 0;

        do {
            if (char.IsWhiteSpace(_input[offset])) {
                offset++;
                continue;
            }

            Option<Token> token = ReadNextValidToken(offset);

            // If we didn't find any valid tokens then we reached the end of input.
            // then set offset to stop the loop
            offset = token.Map(t => t.StartIndex + t.Length).ValueOr(_input.Length);

            if (token.HasValue && !ignoredTokens.Contains(token.Value.Type)) {
                yield return token.Value;
            }
        } while (offset < _input.Length);

        yield return new Token(Eof, null, offset, 0);
    }

    private Option<Token> ReadNextValidToken(int startIndex)
    {
        StringBuilder unknownTokenContents = new();
        Option<Token> validToken = Option.None<Token>();
        int offset = startIndex;

        while (!validToken.HasValue && offset < _input.Length) {
            if (char.IsWhiteSpace(_input[offset])) {
                ++offset;
                continue;
            }

            validToken = ReadToken(offset);

            _ = unknownTokenContents.Append(_input[offset]);
            ++offset;
        }

        if (--unknownTokenContents.Length > 0) {
            AddMessage(Message.UnknownToken(startIndex, unknownTokenContents.ToString()));
        }

        return validToken;
    }

    private Option<Token> ReadToken(int offset)
     => rules.Select(r => r.TryExtract(_input, offset)).FirstOrNone(t => t.HasValue);
}
