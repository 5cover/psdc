using Scover.Psdc.Lexing;
using Scover.Psdc.Messages;

namespace Scover.Psdc.Tests;

sealed class LexerTestsDataSources
{
    public static IEnumerable<Func<(string, IReadOnlyList<Token>, IEnumerable<EvaluatedMessage>)>> TokenSequenceWithMessages()
    {
        yield return () => ("&", [], [new(new(0, 1), MessageCode.UnknownToken, "stray `&` in program", [])]);
        yield return () => ("&&", [], [new(new(0, 2), MessageCode.UnknownToken, "stray `&&` in program", [])]);
        yield return () => ("&&:&&", [new(new(2, 3), TokenType.Colon)], [
            new(new(0, 2), MessageCode.UnknownToken, "stray `&&` in program", []),
            new(new(3, 5), MessageCode.UnknownToken, "stray `&&` in program", []),
        ]);
        yield return () => ("&¤:&°&§``&&¤&&", [new(new(2, 3), TokenType.Colon)], [
            new(new(0, 2), MessageCode.UnknownToken, "stray `&¤` in program", []),
            new(new(3, 14), MessageCode.UnknownToken, "stray ```&°&§``&&¤&&``` in program", []),
        ]);
        yield return () => ("a\\ ", [new(new(0, 1), TokenType.Ident, "a")], [
            new(new(1, 2), MessageCode.UnknownToken, "stray `\\` in program", []),
        ]);
    }

    public static IEnumerable<Func<(string, IReadOnlyList<Token>)>> TokenSequence()
    {
        yield return () => (";;;", [
            new(new(0, 1), TokenType.Semi),
            new(new(1, 2), TokenType.Semi),
            new(new(2, 3), TokenType.Semi),
        ]);
        yield return () => ("ac;dc", [
            new(new(0, 2), TokenType.Ident, "ac"),
            new(new(2, 3), TokenType.Semi),
            new(new(3, 5), TokenType.Ident, "dc"),
        ]);
        yield return () => (":==", [
            new(new(0, 2), TokenType.ColonEqual),
            new(new(2, 3), TokenType.Equal),
        ]);
        yield return () => ("procèdure procédure Procèdure PROCÈDURE", [
            new(new(0, 9), TokenType.Ident, "procèdure"),
            new(new(10, 19), TokenType.Procedure),
            new(new(20, 29), TokenType.Ident, "Procèdure"),
            new(new(30, 39), TokenType.Ident, "PROCÈDURE"),
        ]);

        // Literals
        yield return () => ("123 3.14 'a' \"Hello, World!\"", [
            new(new(0, 3), TokenType.LiteralInt, 123L),
            new(new(4, 8), TokenType.LiteralReal, 3.14m),
            new(new(9, 12), TokenType.LiteralChar, 'a'),
            new(new(13, 28), TokenType.LiteralString, "Hello, World!"),
        ]);

        // Control-flow and grouping with operators.
        // Positions computed approximately (adjust as needed):
        yield return () => ("si (a == b) alors écrire(a); fin_si", [
            new(new(0, 2), TokenType.If),           // "si"
            new(new(3, 4), TokenType.LParen),       // "("
            new(new(4, 5), TokenType.Ident, "a"),   // "a"
            new(new(6, 8), TokenType.Eq),           // "=="
            new(new(9, 10), TokenType.Ident, "b"),  // "b"
            new(new(10, 11), TokenType.RParen),     // ")"
            new(new(12, 17), TokenType.Then),       // "alors"
            new(new(18, 24), TokenType.Write),      // "écrire"
            new(new(24, 25), TokenType.LParen),     // "("
            new(new(25, 26), TokenType.Ident, "a"), // "a"
            new(new(26, 27), TokenType.RParen),     // ")"
            new(new(27, 28), TokenType.Semi),       // ";"
            new(new(29, 35), TokenType.EndIf),      // "fin_si"
        ]);

        // Grouping tokens sequence.
        yield return () => ("({[]})", [
            new(new(0, 1), TokenType.LParen),   // "("
            new(new(1, 2), TokenType.LBrace),   // "{"
            new(new(2, 3), TokenType.LBracket), // "["
            new(new(3, 4), TokenType.RBracket), // "]"
            new(new(4, 5), TokenType.RBrace),   // "}"
            new(new(5, 6), TokenType.RParen),   // ")"
        ]);

        // Expression with operators.
        yield return () => ("a+b*c-d/e", [
            new(new(0, 1), TokenType.Ident, "a"),
            new(new(1, 2), TokenType.Plus), // "+"
            new(new(2, 3), TokenType.Ident, "b"),
            new(new(3, 4), TokenType.Mul), // "*"
            new(new(4, 5), TokenType.Ident, "c"),
            new(new(5, 6), TokenType.Minus), // "-"
            new(new(6, 7), TokenType.Ident, "d"),
            new(new(7, 8), TokenType.Div), // "/"
            new(new(8, 9), TokenType.Ident, "e"),
        ]);

        // Complex assignment and operator mix.
        yield return () => ("a:=b+c*d-%f", [
            new(new(0, 1), TokenType.Ident, "a"),
            new(new(1, 3), TokenType.ColonEqual), // ":="
            new(new(3, 4), TokenType.Ident, "b"),
            new(new(4, 5), TokenType.Plus), // "+"
            new(new(5, 6), TokenType.Ident, "c"),
            new(new(6, 7), TokenType.Mul), // "*"
            new(new(7, 8), TokenType.Ident, "d"),
            new(new(8, 9), TokenType.Minus), // "-"
            new(new(9, 10), TokenType.Mod),  // "%"
            new(new(10, 11), TokenType.Ident, "f"),
        ]);

        yield return () => ("a\\  \nb", [new(new(0, 6), TokenType.Ident, "ab")]);
        yield return () => ("a\\  \nb\\\n", [new(new(0, 6), TokenType.Ident, "ab")]);
        yield return () => ("\\\na\\  \nb", [new(new(2, 8), TokenType.Ident, "ab")]);
        yield return () => ("\\\na\\  \nb\\\n", [new(new(2, 8), TokenType.Ident, "ab")]);
        yield return () => ("a\\  \n", [new(new(0, 1), TokenType.Ident, "a")]);
        yield return () => ("a\\\nb", [new(new(0, 4), TokenType.Ident, "ab")]);

        // Identifier with a line continuation inside it.
        yield return () => ("hé\\ \r  \nllo", [new(new(0, 11), TokenType.Ident, "héllo")]);

        // No space after the line continuation, tokens merge.
        yield return () => ("a\\\r\nb", [new(new(0, 5), TokenType.Ident, "ab")]);

        // Space after the line continuation causes token splitting.
        // After removing the sequence "\" and newline, the remaining space acts as a delimiter.
        yield return () => ("a\\\n b", [
            new(new(0, 1), TokenType.Ident, "a"),
            new(new(4, 5), TokenType.Ident, "b"),
        ]);

        // 4. String literal with a line continuation.
        // The backslash-newline sequence is removed.
        yield return () => ("\"Hello,\\\nWorld!\"", [
            new(new(0, 16), TokenType.LiteralString, "Hello,World!"),
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
        // Line continuations
        yield return () => (TokenType.Array, "tab\\\nleau");
        yield return () => (TokenType.Array, "t\\\nab\\\n\\\n\\\nleau");

        // Regular
        yield return () => (TokenType.Array, "TABLEAU");
        yield return () => (TokenType.Array, "Tableau");
        yield return () => (TokenType.Array, "TaBlEaU");
        yield return () => (TokenType.Array, "tableau");
        yield return () => (TokenType.Begin, "début");
        yield return () => (TokenType.Begin, "dÉbUt");
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
