using System.Collections.Immutable;
using System.Data;
using System.Text.RegularExpressions;

namespace Scover.Psdc.Tokenization;

public class TokenType
{

    internal interface Ruled<out T> where T : TokenRule
    {
        public IEnumerable<T> Rules { get; }
    }

    protected static IEnumerable<T> GetRules<T>(TokenType self, IEnumerable<Func<TokenType, T>> rules) => rules.Select(r => r(self));

    private readonly string _repr;

    TokenType(string repr, bool isCode) => _repr = isCode ? $"'{repr}'" : repr;

    public static TokenType Eof { get; } = new("end of file", false);

    public override string ToString() => _repr;

    internal sealed class Keyword : TokenType, Ruled<WordTokenRule>
    {
        static readonly List<Keyword> instances = [];

        public IEnumerable<WordTokenRule> Rules { get; }

        Keyword(string mainWord, params string[] words) : base(mainWord, true)
        {
            instances.Add(this);
            Rules = GetRules(this, words.Prepend(mainWord).Select(w => (Func<TokenType, WordTokenRule>)(t => new(t, w, StringComparison.Ordinal))));
        }

        public static IReadOnlyCollection<Keyword> Instances => instances;
        public static Keyword Begin { get; } = new("début",
                                                   "debut");
        public static Keyword End { get; } = new("fin");
        public static Keyword Is { get; } = new("c'est");
        public static Keyword Program { get; } = new("programme");
        public static Keyword Structure { get; } = new("structure");
        public static Keyword Type { get; } = new("type");

        #region Data

        public static Keyword Array { get; } = new("tableau");
        public static Keyword Constant { get; } = new("constante");
        public static Keyword False { get; } = new("faux");
        public static Keyword True { get; } = new("vrai");

        #endregion Data

        #region Types

        public static Keyword Boolean { get; } = new("booléen",
                                                     "booleen");
        public static Keyword Character { get; } = new("caractère",
                                                       "caractere");
        public static Keyword File { get; } = new("nomFichierLog");
        public static Keyword Integer { get; } = new("entier");
        public static Keyword Real { get; } = new("réel",
                                                  "reel");
        public static Keyword String { get; } = new("chaîne",
                                                    "chaine");
        #endregion Types

        #region Callables

        public static Keyword Delivers { get; } = new("délivre",
                                                      "delivre");
        public static Keyword Function { get; } = new("fonction");
        public static Keyword Procedure { get; } = new("procédure",
                                                       "procedure");
        public static Keyword Return { get; } = new("retourne");

        #endregion Callables

        #region Builtins

        public static Keyword Assigner { get; } = new("assigner");
        public static Keyword Ecrire { get; } = new("écrire",
                                                    "ecrire");
        public static Keyword EcrireEcran { get; } = new("écrireEcran",
                                                         "écrireÉcran",
                                                         "ecrireÉcran",
                                                         "ecrireEcran");
        public static Keyword Fdf { get; } = new("FdF");
        public static Keyword Fermer { get; } = new("fermer");
        public static Keyword Lire { get; } = new("lire");
        public static Keyword LireClavier { get; } = new("lireClavier");
        public static Keyword OuvrirAjout { get; } = new("ouvrirAjout");
        public static Keyword OuvrirEcriture { get; } = new("ouvrirEcriture",
                                                            "ouvrirÉcriture");
        public static Keyword OuvrirLecture { get; } = new("ouvrirLecture");

        #endregion Builtins

        #region Control structures

        public static Keyword Do { get; } = new("faire");
        public static Keyword Else { get; } = new("sinon");
        public static Keyword ElseIf { get; } = new("sinonsi",
                                                    "sinonSi");
        public static Keyword EndDo { get; } = new("finfaire",
                                                   "finFaire");
        public static Keyword EndIf { get; } = new("finsi",
                                                   "finSi");
        public static Keyword EndSwitch { get; } = new("finselon",
                                                       "finSelon");
        public static Keyword For { get; } = new("pour");
        public static Keyword EndFor { get; } = new("finpour",
                                                    "finPour");
        public static Keyword If { get; } = new("si");
        public static Keyword Repeat { get; } = new("répéter",
                                                    "répeter",
                                                    "repéter",
                                                    "repeter");
        public static Keyword Switch { get; } = new("selon");
        public static Keyword Then { get; } = new("alors");
        public static Keyword Until { get; } = new("jusqu'à",
                                                   "jusqu'a");
        public static Keyword When { get; } = new("quand");
        public static Keyword While { get; } = new("tant");

        #endregion Control structures

        #region Parameters

        public static Keyword EntE { get; } = new("entE");
        public static Keyword EntF { get; } = new("entF");
        public static Keyword EntSortE { get; } = new("entE/sortE");
        public static Keyword EntSortF { get; } = new("entF/sortF");
        public static Keyword SortE { get; } = new("sortE");
        public static Keyword SortF { get; } = new("sortF");

        #endregion Parameters

        #region Logical operators
        public static Keyword And { get; } = new("ET",
                                                  "et");
        public static Keyword Not { get; } = new("NON",
                                                  "non");
        public static Keyword Or { get; } = new("OU",
                                                 "ou");
        public static Keyword Xor { get; } = new("XOR",
                                                  "xor");

        #endregion Logical opetators
    }

    internal sealed class Punctuation : TokenType, Ruled<StringTokenRule>
    {
        static readonly List<Punctuation> instances = [];

        public IEnumerable<StringTokenRule> Rules { get; }

        Punctuation(string code) : base(code, true)
        {
            instances.Add(this);
            Rules = GetRules(this, [t => new StringTokenRule(t, code, StringComparison.Ordinal)]);
        }

        public static IReadOnlyCollection<Punctuation> Instances => instances;
        public static Punctuation Arrow { get; } = new("=>");
        public static Punctuation Colon { get; } = new(":");
        public static Punctuation Comma { get; } = new(",");
        public static Punctuation LBrace { get; } = new("{");
        public static Punctuation LBracket { get; } = new("[");
        public static Punctuation LParen { get; } = new("(");
        public static Punctuation RBrace { get; } = new("}");
        public static Punctuation RBracket { get; } = new("]");
        public static Punctuation RParen { get; } = new(")");
        public static Punctuation Semicolon { get; } = new(";");
        public static Punctuation NumberSign { get; } = new("#");

        #region Operators

        public static Punctuation ColonEqual { get; } = new(":=");
        public static Punctuation Dot { get; } = new(".");
        public static Punctuation Equal { get; } = new("=");

        #region Arithmetic

        public static Punctuation Plus { get; } = new("+");
        public static Punctuation Divide { get; } = new("/");
        public static Punctuation Mod { get; } = new("%");
        public static Punctuation Times { get; } = new("*");
        public static Punctuation Minus { get; } = new("-");

        #endregion Arithmetic

        #region Comparison

        public static Punctuation DoubleEqual { get; } = new("==");
        public static Punctuation GreaterThan { get; } = new(">");
        public static Punctuation GreaterThanOrEqual { get; } = new(">=");
        public static Punctuation LessThan { get; } = new("<");
        public static Punctuation LessThanOrEqual { get; } = new("<=");
        public static Punctuation NotEqual { get; } = new("!=");

        #endregion Comparison

        #endregion Operators
    }

    internal sealed class Valued : TokenType, Ruled<RegexTokenRule>
    {
        static readonly List<Valued> instances = [];

        public IEnumerable<RegexTokenRule> Rules { get; }

        Valued(string name, string pattern, RegexOptions options = RegexOptions.None) : base(name, false)
        {
            instances.Add(this);
            Rules = GetRules(this, [t => new RegexTokenRule(t, pattern, options)]);
        }

        public static IReadOnlyCollection<Valued> Instances => instances;
        public static Valued CommentMultiline { get; } = new("multiline comment", @"/\*(.*?)\*/", RegexOptions.Singleline);
        public static Valued CommentSingleline { get; } = new("singleline comment", "//(.*)$", RegexOptions.Multiline);
        public static Valued Identifier { get; } = new("identifier", @"([\p{L}_][\p{L}_0-9]*)");
        public static Valued LiteralCharacter { get; } = new("character literal", @"'((?:\\?.)*?)'");
        public static Valued LiteralInteger { get; } = new("integer literal", @"(-?\d+)");
        public static Valued LiteralReal { get; } = new("real literal", @"(-?\d*\.\d+)");
        public static Valued LiteralString { get; } = new("string literal", @"""((?:\\?.)*?)""");

        // Matches Identifier's regex second part: [\p{L}_0-9]
        internal static bool IsIdentifierChar(char c) => c == '_' || char.IsLetterOrDigit(c);
    }

    internal sealed class ContextKeyword
    {
        public IImmutableSet<string> Names { get; }
        ContextKeyword(IImmutableSet<string> names) => Names = names;

        public static ContextKeyword Assert { get; } = new(["assert"]);
        public static ContextKeyword Eval { get; } = new(["eval"]);
        public static ContextKeyword Expr { get; } = new(["expr"]);
        public static ContextKeyword From { get; } = new(["de"]);
        public static ContextKeyword Step { get; } = new(["pas"]);
        public static ContextKeyword To { get; } = new(["à", "a"]);
        public static ContextKeyword Other { get; } = new(["autre"]);
        public static ContextKeyword That { get; } = new(["que"]);
    }
}
