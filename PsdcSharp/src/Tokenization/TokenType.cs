using System.Text.RegularExpressions;

namespace Scover.Psdc.Tokenization;

public abstract class TokenType
{
    readonly string _repr;

    TokenType(string repr, bool isStatic)
     => _repr = isStatic ? $"`{repr}`" : repr;

    internal interface Ruled
    {
        public TokenRule Rule { get; }

        public Option<Token> TryExtract(string code, int startIndex);
    }

    public override string ToString() => _repr;

    internal sealed class Keyword : Ruled<WordTokenRule>
    {
        static readonly List<Keyword> instances = [];

        Keyword(string word)
                : base(word, true, new WordTokenRule(word, StringComparison.Ordinal))
         => instances.Add(this);

        public static Keyword Begin { get; } = new("début");
        public static Keyword End { get; } = new("fin");
        public static IReadOnlyCollection<Keyword> Instances => instances;
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

        public static Keyword Boolean { get; } = new("booléen");
        public static Keyword Character { get; } = new("caractère");
        public static Keyword File { get; } = new("nomFichierLog");
        public static Keyword Integer { get; } = new("entier");
        public static Keyword Real { get; } = new("réel");
        public static Keyword String { get; } = new("chaîne");
        #endregion Types

        #region Callables

        public static Keyword Delivers { get; } = new("délivre");
        public static Keyword Function { get; } = new("fonction");
        public static Keyword Procedure { get; } = new("procédure");
        public static Keyword Return { get; } = new("retourne");

        #endregion Callables

        #region Builtins

        public static Keyword Assigner { get; } = new("assigner");
        public static Keyword Ecrire { get; } = new("écrire");
        public static Keyword EcrireEcran { get; } = new("écrireEcran");
        public static Keyword Fdf { get; } = new("FdF");
        public static Keyword Fermer { get; } = new("fermer");
        public static Keyword Lire { get; } = new("lire");
        public static Keyword LireClavier { get; } = new("lireClavier");
        public static Keyword OuvrirAjout { get; } = new("ouvrirAjout");
        public static Keyword OuvrirEcriture { get; } = new("ouvrirEcriture");
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
        public static Keyword Repeat { get; } = new("répéter");
        public static Keyword Step { get; } = new("pas");
        public static Keyword Switch { get; } = new("selon");
        public static Keyword Then { get; } = new("alors");
        public static Keyword To { get; } = new("à");
        public static Keyword Until { get; } = new("jusqu'à");
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

        Operator(string code)
                : base(code, true, new StringTokenRule(code, StringComparison.Ordinal))
         => instances.Add(this);

        public static Operator Assignment { get; } = new(":=");
        public static Operator ComponentAccess { get; } = new(".");
        public static IReadOnlyCollection<Operator> Instances => instances;
        public static Operator TypeAssignment { get; } = new("=");

        #region Arithmetic

        public static Operator Add { get; } = new("+");
        public static Operator Divide { get; } = new("/");
        public static Operator Mod { get; } = new("%");
        public static Operator Multiply { get; } = new("*");
        public static Operator Subtract { get; } = new("-");
        #endregion Arithmetic

        #region Logical

        public static Operator And { get; } = new("ET");
        public static Operator Not { get; } = new("NON");
        public static Operator Or { get; } = new("OU");
        public static Operator Xor { get; } = new("XOR");

        #endregion Logical

        #region Comparison

        public static Operator Equal { get; } = new("==");
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

        Punctuation(string code)
                : base(code, true, new StringTokenRule(code, StringComparison.Ordinal))
         => instances.Add(this);

        public static Punctuation Arrow { get; } = new("=>");
        public static Punctuation CloseBrace { get; } = new("}");
        public static Punctuation CloseBracket { get; } = new(")");
        public static Punctuation CloseSquareBracket { get; } = new("]");
        public static Punctuation Colon { get; } = new(":");
        public static Punctuation Comma { get; } = new(",");
        public static IReadOnlyCollection<Punctuation> Instances => instances;
        public static Punctuation OpenBrace { get; } = new("{");
        public static Punctuation OpenBracket { get; } = new("(");
        public static Punctuation OpenSquareBracket { get; } = new("[");
        public static Punctuation Semicolon { get; } = new(";");
    }

    internal abstract class Ruled<T>(string repr, bool isStatic, T rule)
                : TokenType(repr, isStatic), Ruled where T : TokenRule
    {
        public T Rule => rule;

        TokenRule Ruled.Rule => rule;

        public Option<Token> TryExtract(string code, int startIndex) => rule.TryExtract(this, code, startIndex);
    }

    internal sealed class Special : TokenType
    {
        Special(string repr) : base(repr, false)
        {
        }

        /// <summary>Special token that indicates the end of the token sequence.</summary>
        public static Special Eof { get; } = new("end of file");
    }

    internal sealed class Valued : Ruled<RegexTokenRule>
    {
        static readonly List<Valued> instances = [];

        Valued(string name, string pattern, RegexOptions options = RegexOptions.None)
                : base(name, false, new(pattern, options)) => instances.Add(this);

        public static Valued CommentMultiline { get; } = new("multiline comment", @"/\*(.*?)\*/", RegexOptions.Singleline);
        public static Valued CommentSingleline { get; } = new("singleline comment", "//(.*)$", RegexOptions.Multiline);
        public static Valued Identifier { get; } = new("identifier", @"([\p{L}_][\p{L}_0-9]*)");
        public static IReadOnlyCollection<Valued> Instances => instances;
        public static Valued LiteralCharacter { get; } = new("character literal", "'(.)'");
        public static Valued LiteralInteger { get; } = new("integer literal", @"(\d+)");
        public static Valued LiteralReal { get; } = new("real literal", @"(\d*\.\d+)");
        public static Valued LiteralString { get; } = new("string literal", @"""(.*?)""");
    }
}
