
using System.Text.RegularExpressions;

namespace Scover.Psdc.Tokenization;

internal abstract class TokenType
{
    private readonly string _repr;

    public override string ToString() => _repr;

    private TokenType(string repr, bool isStatic) => _repr = isStatic ? $"`{repr}`" : repr;

    internal interface Ruled
    {
        public TokenRule Rule { get; }
        public Option<Token> TryExtract(string input, int startIndex);
    }

    internal abstract class Ruled<T>(string repr, bool isStatic, T rule)
    : TokenType(repr, isStatic), Ruled where T : TokenRule
    {
        public Option<Token> TryExtract(string input, int startIndex) => rule.TryExtract(this, input, startIndex);
        public T Rule => rule;
        TokenRule Ruled.Rule => rule;
    }

    internal sealed class Named : Ruled<RegexTokenRule>
    {
        private Named(string name, string pattern, RegexOptions options = RegexOptions.None)
        : base(name, false, new(pattern, options))
        {
            _instances.Add(this);
        }

        private static List<Named> _instances = [];
        public static IReadOnlyCollection<Named> Instances => _instances;

        public static Named CommentMultiline { get; } = new("multiline comment", @"/\*(.*?)\*/", RegexOptions.Singleline);
        public static Named CommentSingleline { get; } = new("singleline comment", "//(.*)$", RegexOptions.Multiline);
        public static Named Identifier { get; } = new("identifier", @"([\p{L}_][\p{L}_0-9]*)");
        public static Named LiteralCharacter { get; } = new("character literal", "'(.)'");
        public static Named LiteralInteger { get; } = new("integer literal", @"(\d+)");
        public static Named LiteralReal { get; } = new("real literal", @"(\d*\.\d+)");
        public static Named LiteralString { get; } = new("string literal", @"""(.*?)""");
    }

    internal sealed class Symbol : Ruled<StringTokenRule>
    {
        private Symbol(string code)
        : base(code, true, new StringTokenRule(code, StringComparison.Ordinal))
        {

            _instances.Add(this);
        }

        private static List<Symbol> _instances = [];
        public static IReadOnlyCollection<Symbol> Instances => _instances;

        public static Symbol CloseBrace { get; } = new("}");
        public static Symbol CloseBracket { get; } = new(")");
        public static Symbol CloseSquareBracket { get; } = new("]");
        public static Symbol OpenBrace { get; } = new("{");
        public static Symbol OpenBracket { get; } = new("(");
        public static Symbol OpenSquareBracket { get; } = new("[");
        public static Symbol Arrow { get; } = new("=>");
        public static Symbol Colon { get; } = new(":");
        public static Symbol Comma { get; } = new(",");
        public static Symbol Semicolon { get; } = new(";");

        #region Operators

        public static Symbol OperatorAssignment { get; } = new(":=");
        public static Symbol OperatorMemberAccess { get; } = new(".");
        public static Symbol OperatorTypeAssignment { get; } = new("=");

        #region Arithmetic

        public static Symbol OperatorDivide { get; } = new("/");
        public static Symbol OperatorMinus { get; } = new("-");
        public static Symbol OperatorModulus { get; } = new("%");
        public static Symbol OperatorMultiply { get; } = new("*");
        public static Symbol OperatorPlus { get; } = new("+");

        #endregion Arithmetic

        #region Logical

        public static Symbol OperatorAnd { get; } = new("et");
        public static Symbol OperatorNot { get; } = new("!");
        public static Symbol OperatorOr { get; } = new("ou");

        #endregion Logical

        #region Comparison

        public static Symbol OperatorEqual { get; } = new("==");
        public static Symbol OperatorGreaterThan { get; } = new(">");
        public static Symbol OperatorGreaterThanOrEqual { get; } = new(">=");
        public static Symbol OperatorLessThan { get; } = new("<");
        public static Symbol OperatorLessThanOrEqual { get; } = new("<=");
        public static Symbol OperatorNotEqual { get; } = new("!=");

        #endregion Comparison

        #endregion Operators
    }

    internal sealed class Keyword : Ruled<WordTokenRule>
    {
        private Keyword(string word)
        : base(word, true, new WordTokenRule(word, StringComparison.Ordinal))
        {
            _instances.Add(this);
        }

        private static List<Keyword> _instances = [];
        public static IReadOnlyCollection<Keyword> Instances => _instances;

        public static Keyword Begin { get; } = new("début");
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

        public static Keyword Integer { get; } = new("entier");
        public static Keyword Real { get; } = new("réel");
        public static Keyword Character { get; } = new("caractère");
        public static Keyword String { get; } = new("chaîne");
        public static Keyword Boolean { get; } = new("booléen");

        #endregion Types

        #region Callables

        public static Keyword Delivers { get; } = new("délivre");
        public static Keyword Function { get; } = new("fonction");
        public static Keyword Procedure { get; } = new("procédure");
        public static Keyword Return { get; } = new("retourne");

        #endregion Callables

        #region Builtins

        public static Keyword LireClavier { get; } = new("lireClavier");
        public static Keyword EcrireEcran { get; } = new("écrireEcran");
        public static Keyword Assigner { get; } = new("assigner");
        public static Keyword OuvrirAjout { get; } = new("ouvrirAjout");
        public static Keyword OuvrirEcriture { get; } = new("ouvrirEcriture");
        public static Keyword OuvrirLecture { get; } = new("ouvrirLecture");
        public static Keyword Lire { get; } = new("lire");
        public static Keyword Ecrire { get; } = new("écrire");
        public static Keyword Fermer { get; } = new("fermer");
        public static Keyword Fdf { get; } = new("fdf");

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
        public static Keyword EntSortE { get; } = new("entE/sortE");
        public static Keyword EntF { get; } = new("entF");
        public static Keyword EntSortF { get; } = new("entF/sortF");
        public static Keyword SortE { get; } = new("sortE");
        public static Keyword SortF { get; } = new("sortF");

        #endregion Parameters
    }

    internal sealed class Special : TokenType
    {
        private Special(string repr) : base(repr, false)
        {
        }

        /// <summary>Special token that indicates the end of the token sequence.</summary>
        public static Special Eof { get; } = new("end of file");
    }

}
