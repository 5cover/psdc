using Scover.Psdc.Lexing;

namespace Psdc.Tests;

public sealed class LexerTestsDataSources
{
    public static IEnumerable<Func<(string, IReadOnlyList<Token>)>> TokenSequence()
    {
        yield return () => (";;;", [
            new(new(0, 1), TokenType.Semi),
            new(new(1, 2), TokenType.Semi),
            new(new(2, 3), TokenType.Semi),
        ]);
    }

    public static IEnumerable<Func<string>> OneIdentifier()
    {
        yield return () => "foo";    // Starts with letter, simple
        yield return () => "_bar";   // Starts with underscore
        yield return () => "x1";     // Letter + digit
        yield return () => "π";      // Unicode letter
        yield return () => "名前";     // Japanese letters (valid `\p{L}`)
        yield return () => "_123";   // Underscore start, digits after
        yield return () => "übung";  // German 'ü' is a letter
        yield return () => "ñandú";  // Spanish ñ is valid
        yield return () => "变量2";    // Chinese identifier with digit
        yield return () => "a_b_c";  // Underscores allowed inside
        yield return () => "a1b2c3"; // Mix of letters and digits
        yield return () => "A";      // Single uppercase letter
        yield return () => "Z9";     // Letter + digit
        yield return () => "Λambda"; // Greek capital Lambda
    }
    public static IEnumerable<Func<string>> Empty()
    {
        yield return () => "";
        yield return () => " ";
        yield return () => "\n\r\t\v";
    }

    public static IEnumerable<Func<(long, string)>> OneLiteralInt()
    {
        yield return () => (5, "5");
        yield return () => (0, "0");
        yield return () => (0, "0000");
        yield return () => (5, "00005");
        yield return () => (5432, "00005432");
        yield return () => (543200, "0000543200");
    }

    public static IEnumerable<Func<(decimal, string)>> OneLiteralReal()
    {
        yield return () => (0, ".0");
        yield return () => (.1403m, ".1403");
        yield return () => (0, "0.0");
        yield return () => (0, "00.0");
        yield return () => (.1m, "0.1");
        yield return () => (.1m, "00.1");
        yield return () => (.17m, "0.17");
        yield return () => (456.17m, "456.17");
        yield return () => (456.17m, "0456.170");
    }

    public static IEnumerable<Func<(char, string)>> OneLiteralChar()
    {
        yield return () => ('A', "'A'");
        yield return () => ('A', "'\\x41'");   // hex
        yield return () => ('A', "'\\u0041'"); // unicode
        yield return () => ('A', "'\\101'");   // octal (101 = 65 = 'A')

        yield return () => ('Ω', "'\u03A9'");  // unicode char
        yield return () => ('Ω', "'\\u03A9'"); // unicode escape

        yield return () => ('❤', "'\u2764'");  // unicode char
        yield return () => ('❤', "'\\u2764'"); // unicode escape
        yield return () => ('❤', "'\\x2764'"); // hex escape

        yield return () => ('\'', "'\\''");   // standard escape
        yield return () => ('\'', "'\\x27'"); // hex
        yield return () => ('\'', "'\\047'"); // octal

        yield return () => ('"', "'\"'");    // actual character
        yield return () => ('"', "'\\\"'");  // standard escape
        yield return () => ('"', "'\\x22'"); // hex
        yield return () => ('"', "'\\042'"); // octal

        yield return () => ('\\', @"'\\'");   // standard escape
        yield return () => ('\\', "'\\x5c'"); // hex
        yield return () => ('\\', "'\\x5C'"); // hex
        yield return () => ('\\', "'\\134'"); // octal

        yield return () => ('\a', "'\\a'");   // escape character
        yield return () => ('\a', "'\\x07'"); // hex
        yield return () => ('\a', "'\\007'"); // octal

        yield return () => ('\b', "'\\b'");   // escape character
        yield return () => ('\b', "'\\x08'"); // hex
        yield return () => ('\b', "'\\010'"); // octal

        yield return () => ('\f', "'\\f'");   // escape character
        yield return () => ('\f', "'\\x0c'"); // hex
        yield return () => ('\f', "'\\x0C'"); // hex
        yield return () => ('\f', "'\\014'"); // octal

        yield return () => ('\n', "'\\n'");   // standard escape
        yield return () => ('\n', "'\\x0a'"); // hex
        yield return () => ('\n', "'\\x0A'"); // hex
        yield return () => ('\n', "'\\012'"); // octal

        yield return () => ('\r', "'\\r'");   // escape character
        yield return () => ('\r', "'\\x0d'"); // hex
        yield return () => ('\r', "'\\x0D'"); // hex
        yield return () => ('\r', "'\\015'"); // octal

        yield return () => ('\t', "'\\t'");   // escape character
        yield return () => ('\t', "'\\x09'"); // hex
        yield return () => ('\t', "'\\011'"); // octal

        yield return () => ('\v', "'\\v'");   // escape character
        yield return () => ('\v', "'\\x0b'"); // hex
        yield return () => ('\v', "'\\x0B'"); // hex
        yield return () => ('\v', "'\\013'"); // octal

        yield return () => ('\e', "'\\e'");   // escape character
        yield return () => ('\e', "'\\x1b'"); // hex
        yield return () => ('\e', "'\\x1B'"); // hex
        yield return () => ('\e', "'\\033'"); // octal

        yield return () => ('\xff', "'\\xff'"); // hex
        yield return () => ('\xff', "'\\xfF'"); // hex
        yield return () => ('\xff', "'\\xFf'"); // hex
        yield return () => ('\xff', "'\\xFF'"); // hex
    }

    public static IEnumerable<Func<(string, string)>> OneLiteralString()
    {
        yield return () => ("", "\"\"");
        yield return () => (" ", "\" \"");
        yield return () => ("hello world", "\"hello world\"");
        yield return () => ("Hello World", "\"Hello World\"");
        yield return () => ("变unicôde量2", "\"变unicôde量2\"");

        // Hex and Unicode escapes
        yield return () => ("\u0041", "\"\\u0041\"");             // A
        yield return () => ("\u03A9", "\"\\u03A9\"");             // Ω
        yield return () => ("f\U0001F600g", "\"f\\U0001F600g\""); // 😀
        yield return () => ("\x41", "\"\\x41\"");                 // A
        yield return () => ("\x7F", "\"\\x7F\"");                 // DEL
        yield return () => ("\x2764", "\"\\x2764\"");             // ❤
        yield return () => ("S", "\"\\123\"");                    // octal for 'S'

        // Edge cases
        yield return () => ("line1\nline2", "\"line1\\nline2\"");
        yield return () => ("Tabbed\ttext", "\"Tabbed\\ttext\"");
        yield return () => ("Quote: \"", "\"Quote: \\\"\"");
        yield return () => ("Backslash: \\", "\"Backslash: \\\\\"");
        yield return () => ("Emoji: 😀", "\"Emoji: 😀\"");
        yield return () => ("Emoji: 😀", "\"Emoji: \\uD83D\\uDE00\"");
        yield return () => ("Emoji: 😀", "\"Emoji: \\xD83D\\xDE00\"");
        yield return () => ("Emoji: 😀", "\"Emoji: \\xD83D\\uDE00\"");
        yield return () => ("Emoji: 😀", "\"Emoji: \\uD83D\\uDE00\"");
        yield return () => ("Emoji: 😀", "\"Emoji: \\U0000D83D\\U0000DE00\"");
        yield return () => ("中文\nTab\tEnd", "\"中文\\nTab\\tEnd\"");

        yield return () => ("A", "\"A\"");
        yield return () => ("A", "\"\\x41\"");   // hex
        yield return () => ("A", "\"\\u0041\""); // unicode
        yield return () => ("A", "\"\\101\"");   // octal (101 = 65 = 'A')

        yield return () => ("Ω", "\"\u03A9\"");  // unicode char
        yield return () => ("Ω", "\"\\u03A9\""); // unicode escape

        yield return () => ("❤", "\"\u2764\"");  // unicode char
        yield return () => ("❤", "\"\\u2764\""); // unicode escape
        yield return () => ("❤", "\"\\x2764\""); // hex escape

        yield return () => ("S", "\"S\"");     // actual character
        yield return () => ("S", "\"\\123\""); // octal

        yield return () => ("😀", "\"😀\"");          // actual character
        yield return () => ("😀", "\"\\U0001F600\""); // full unicode escape

        yield return () => ("'", "\"'\"");     // actual character
        yield return () => ("'", "\"\\'\"");   // standard escape
        yield return () => ("'", "\"\\x27\""); // hex
        yield return () => ("'", "\"\\047\""); // octal

        yield return () => ("\"", "\"\\\"\"");  // standard escape
        yield return () => ("\"", "\"\\x22\""); // hex
        yield return () => ("\"", "\"\\042\""); // octal

        yield return () => ("\\", "\"\\\\\"");  // standard escape
        yield return () => ("\\", "\"\\x5c\""); // hex
        yield return () => ("\\", "\"\\x5C\""); // hex
        yield return () => ("\\", "\"\\134\""); // octal

        yield return () => ("\a", "\"\\a\"");   // escape character
        yield return () => ("\a", "\"\\x07\""); // hex
        yield return () => ("\a", "\"\\007\""); // octal

        yield return () => ("\b", "\"\\b\"");   // escape character
        yield return () => ("\b", "\"\\x08\""); // hex
        yield return () => ("\b", "\"\\010\""); // octal

        yield return () => ("\f", "\"\\f\"");   // escape character
        yield return () => ("\f", "\"\\x0c\""); // hex
        yield return () => ("\f", "\"\\x0C\""); // hex
        yield return () => ("\f", "\"\\014\""); // octal

        yield return () => ("\n", "\"\\n\"");   // standard escape
        yield return () => ("\n", "\"\\x0a\""); // hex
        yield return () => ("\n", "\"\\x0A\""); // hex
        yield return () => ("\n", "\"\\012\""); // octal

        yield return () => ("\r", "\"\\r\"");   // escape character
        yield return () => ("\r", "\"\\x0d\""); // hex
        yield return () => ("\r", "\"\\x0D\""); // hex
        yield return () => ("\r", "\"\\015\""); // octal

        yield return () => ("\t", "\"\\t\"");   // escape character
        yield return () => ("\t", "\"\\x09\""); // hex
        yield return () => ("\t", "\"\\011\""); // octal

        yield return () => ("\v", "\"\\v\"");   // escape character
        yield return () => ("\v", "\"\\x0b\""); // hex
        yield return () => ("\v", "\"\\x0B\""); // hex
        yield return () => ("\v", "\"\\013\""); // octal

        yield return () => ("\e", "\"\\e\"");   // escape character
        yield return () => ("\e", "\"\\x1b\""); // hex
        yield return () => ("\e", "\"\\x1B\""); // hex
        yield return () => ("\e", "\"\\033\""); // octal

        yield return () => ("\xff", "\"\\xff\""); // hex
        yield return () => ("\xff", "\"\\xfF\""); // hex
        yield return () => ("\xff", "\"\\xFf\""); // hex
        yield return () => ("\xff", "\"\\xFF\""); // hex
    }

    public static IEnumerable<Func<(TokenType, string)>> OneToken()
    {
        yield return () => (TokenType.Array, "tableau");
        yield return () => (TokenType.Begin, "début");
        yield return () => (TokenType.Begin, "debut");
        yield return () => (TokenType.Boolean, "booléen");
        yield return () => (TokenType.Boolean, "booleen");
        yield return () => (TokenType.Character, "caractère");
        yield return () => (TokenType.Character, "caractere");
        yield return () => (TokenType.Constant, "constante");
        yield return () => (TokenType.Do, "faire");
        yield return () => (TokenType.Else, "sinon");
        yield return () => (TokenType.ElseIf, "sinon_si");
        yield return () => (TokenType.End, "fin");
        yield return () => (TokenType.EndFor, "fin_pour");
        yield return () => (TokenType.EndIf, "fin_si");
        yield return () => (TokenType.EndSwitch, "fin_selon");
        yield return () => (TokenType.EndWhile, "fin_tant_que");
        yield return () => (TokenType.False, "faux");
        yield return () => (TokenType.For, "pour");
        yield return () => (TokenType.Function, "fonction");
        yield return () => (TokenType.If, "si");
        yield return () => (TokenType.Integer, "entier");
        yield return () => (TokenType.Out, "sortie");
        yield return () => (TokenType.Procedure, "procédure");
        yield return () => (TokenType.Procedure, "procedure");
        yield return () => (TokenType.Program, "programme");
        yield return () => (TokenType.Read, "lire");
        yield return () => (TokenType.Real, "réel");
        yield return () => (TokenType.Real, "reel");
        yield return () => (TokenType.Return, "retourne");
        yield return () => (TokenType.Returns, "délivre");
        yield return () => (TokenType.Returns, "delivre");
        yield return () => (TokenType.String, "chaîne");
        yield return () => (TokenType.String, "chaine");
        yield return () => (TokenType.Structure, "structure");
        yield return () => (TokenType.Switch, "selon");
        yield return () => (TokenType.Then, "alors");
        yield return () => (TokenType.True, "vrai");
        yield return () => (TokenType.Trunc, "ent");
        yield return () => (TokenType.Type, "type");
        yield return () => (TokenType.When, "quand");
        yield return () => (TokenType.WhenOther, "quand_autre");
        yield return () => (TokenType.While, "tant_que");
        yield return () => (TokenType.Write, "écrire");
        yield return () => (TokenType.Write, "ecrire");
        yield return () => (TokenType.And, "et");
        yield return () => (TokenType.Not, "non");
        yield return () => (TokenType.Or, "or");
        yield return () => (TokenType.Xor, "xor");
        yield return () => (TokenType.LBrace, "{");
        yield return () => (TokenType.LBracket, "[");
        yield return () => (TokenType.LParen, "(");
        yield return () => (TokenType.RBrace, "}");
        yield return () => (TokenType.RBracket, "]");
        yield return () => (TokenType.RParen, ")");
        yield return () => (TokenType.Arrow, "=>");
        yield return () => (TokenType.Colon, ":");
        yield return () => (TokenType.ColonEqual, ":=");
        yield return () => (TokenType.Comma, ",");
        yield return () => (TokenType.Div, "/");
        yield return () => (TokenType.Dot, ".");
        yield return () => (TokenType.Semi, ";");
        yield return () => (TokenType.Eq, "==");
        yield return () => (TokenType.Equal, "=");
        yield return () => (TokenType.Gt, ">");
        yield return () => (TokenType.Ge, ">=");
        yield return () => (TokenType.Lt, "<");
        yield return () => (TokenType.Le, "<=");
        yield return () => (TokenType.Minus, "-");
        yield return () => (TokenType.Mod, "%");
        yield return () => (TokenType.Mul, "*");
        yield return () => (TokenType.Neq, "!=");
        yield return () => (TokenType.Plus, "+");
        yield return () => (TokenType.Hash, "#");
    }
}
