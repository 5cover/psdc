using System.Data;
using System.Text.RegularExpressions;

namespace Scover.Psdc.Tokenization;

public class TokenType
{
    readonly string _repr;

    TokenType(string repr, bool isStatic)
     => _repr = isStatic ? $"`{repr}`" : repr;

    internal interface Ruled
    {
        public IEnumerable<TokenRule> Rules { get; }
    }

    public override string ToString() => _repr;

    internal sealed class Keyword : Ruled<WordTokenRule>
    {
        static readonly List<Keyword> instances = [];

        Keyword(string mainWord, params string[] words) : base(mainWord, true,
            words.Prepend(mainWord).Select(w => (Func<TokenType, WordTokenRule>)(t => new WordTokenRule(t, w, StringComparison.Ordinal))))
         => instances.Add(this);

        public static IReadOnlyCollection<Keyword> Instances => instances;
        public static Keyword Begin { get; } = new("début",
                                                   "debut");
        public static Keyword End { get; } = new("fin");
        public static Keyword Is { get; } = new("c'est");
        public static Keyword Program { get; } = new("programme");
        public static Keyword Structure { get; } = new("structure");
        public static Keyword TypeAlias { get; } = new("type");

        #region Data

        public static Keyword Array { get; } = new("tableau");
        public static Keyword Constant { get; } = new("constante");
        public static Keyword False { get; } = new("faux");
        public static Keyword From { get; } = new("de");
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
        public static Keyword ElseIf { get; } = new("sinonsi");
        public static Keyword EndDo { get; } = new("finfaire");
        public static Keyword EndIf { get; } = new("finsi");
        public static Keyword EndSwitch { get; } = new("finselon");
        public static Keyword For { get; } = new("pour");
        public static Keyword If { get; } = new("si");
        public static Keyword Repeat { get; } = new("répéter",
                                                    "répeter",
                                                    "repéter",
                                                    "repeter");
        public static Keyword Step { get; } = new("pas");
        public static Keyword Switch { get; } = new("selon");
        public static Keyword Then { get; } = new("alors");
        public static Keyword To { get; } = new("à",
                                                "a");
        public static Keyword Until { get; } = new("jusqu'à",
                                                   "jusqu'a");
        public static Keyword When { get; } = new("quand");
        public static Keyword WhenOther { get; } = new("quand autre");
        public static Keyword While { get; } = new("tant que");

        #endregion Control structures

        #region Parameters

        public static Keyword EntE { get; } = new("entE");
        public static Keyword EntF { get; } = new("entF");
        public static Keyword EntSortE { get; } = new("entE/sortE");
        public static Keyword EntSortF { get; } = new("entF/sortF");
        public static Keyword SortE { get; } = new("sortE");
        public static Keyword SortF { get; } = new("sortF");

        #endregion Parameters
    }

    internal sealed class Operator : Ruled<StringTokenRule>
    {
        static readonly List<Operator> instances = [];

        Operator(string code, StringComparison comparison = StringComparison.Ordinal)
            : base(code, true, t => new StringTokenRule(t, code, comparison))
         => instances.Add(this);

        public static IReadOnlyCollection<Operator> Instances => instances;
        public static Operator ColonEqual { get; } = new(":=");
        public static Operator Dot { get; } = new(".");
        public static Operator Equal { get; } = new("=");

        #region Arithmetic

        public static Operator Plus { get; } = new("+");
        public static Operator Divide { get; } = new("/");
        public static Operator Mod { get; } = new("%");
        public static Operator Times { get; } = new("*");
        public static Operator Minus { get; } = new("-");

        #endregion Arithmetic

        #region Logical

        public static Operator And { get; } = new("ET", StringComparison.OrdinalIgnoreCase);
        public static Operator Not { get; } = new("NON", StringComparison.OrdinalIgnoreCase);
        public static Operator Or { get; } = new("OU", StringComparison.OrdinalIgnoreCase);
        public static Operator Xor { get; } = new("XOR", StringComparison.OrdinalIgnoreCase);

        #endregion Logical

        #region Comparison

        public static Operator DoubleEqual { get; } = new("==");
        public static Operator GreaterThan { get; } = new(">");
        public static Operator GreaterThanOrEqual { get; } = new(">=");
        public static Operator LessThan { get; } = new("<");
        public static Operator LessThanOrEqual { get; } = new("<=");
        public static Operator NotEqual { get; } = new("!=");

        #endregion Comparison
    }

    internal sealed class Punctuation : Ruled<StringTokenRule>
    {
        static readonly List<Punctuation> instances = [];

        Punctuation(string code) : base(code, true, t => new StringTokenRule(t, code, StringComparison.Ordinal))
         => instances.Add(this);

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
    }

    internal abstract class Ruled<T> : TokenType, Ruled where T : TokenRule
    {
        protected Ruled(string repr, bool isStatic, params Func<TokenType, T>[] rules)
         : this(repr, isStatic, (IEnumerable<Func<TokenType, T>>)rules) { }

        protected Ruled(string repr, bool isStatic, IEnumerable<Func<TokenType, T>> rules) : base(repr, isStatic)
         => Rules = rules.Select(r => r(this));

        public IEnumerable<T> Rules { get; }

        IEnumerable<TokenRule> Ruled.Rules => (IEnumerable<TokenRule>)Rules;
    }

    public static TokenType Eof { get; } = new("end of file", false);

    internal sealed class Valued : Ruled<RegexTokenRule>
    {
        static readonly List<Valued> instances = [];

        Valued(string name, string pattern, RegexOptions options = RegexOptions.None)
            : base(name, false, t => new RegexTokenRule(t, pattern, options)) => instances.Add(this);

        public static IReadOnlyCollection<Valued> Instances => instances;
        public static Valued CommentMultiline { get; } = new("multiline comment", @"/\*(.*?)\*/", RegexOptions.Singleline);
        public static Valued CommentSingleline { get; } = new("singleline comment", "//(.*)$", RegexOptions.Multiline);
        public static Valued Identifier { get; } = new("identifier", @"([\p{L}_][\p{L}_0-9]*)");
        public static Valued LiteralCharacter { get; } = new("character literal", "'(.)'");
        public static Valued LiteralInteger { get; } = new("integer literal", @"(\d+)");
        public static Valued LiteralReal { get; } = new("real literal", @"(\d*\.\d+)");
        public static Valued LiteralString { get; } = new("string literal", @"""(.*?)""");
    }
}
