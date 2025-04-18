using Scover.Psdc.Pseudocode;
using Scover.Psdc.Messages;

namespace Scover.Psdc.CodeGeneration.C;

sealed class KeywordTable : CodeGeneration.KeywordTable
{
    KeywordTable() { }
    public static KeywordTable Instance { get; } = new();

    public string Validate(Scope scope, Range location, string ident, Messenger msger)
    {
        if (keywords.Contains(ident)) {
            var newIdent = ident + "_";
            int identNo = 1;
            while (scope.HasSymbol(newIdent)) {
                newIdent = string.Create(Format.Code, $"{ident}_{identNo++}");
            }
            msger.Report(Message.WarningTargetLanguageReservedKeyword(
                location, Language.Name.C, ident, newIdent));
            return newIdent;
        }
        return ident;
    }

    static readonly HashSet<string> keywords = [
        "_Alignas",
        "_Alignof",
        "_Atomic",
        "_BitInt",
        "_Bool",
        "_Complex",
        "_Decimal128",
        "_Decimal32",
        "_Decimal64",
        "_Generic",
        "_Imaginary",
        "_Noreturn",
        "_Static_assert",
        "_Thread_local",
        "alignas",
        "alignof",
        "auto",
        "bool",
        "break",
        "case",
        "char",
        "const",
        "constexpr",
        "continue",
        "default",
        "do",
        "double",
        "else",
        "enum",
        "extern",
        "false",
        "float",
        "for",
        "goto",
        "if",
        "inline",
        "int",
        "long",
        "nullptr",
        "register",
        "restrict",
        "return",
        "short",
        "signed",
        "sizeof",
        "static_assert",
        "static",
        "struct",
        "switch",
        "thread_local",
        "true",
        "typedef",
        "typeof_unqual",
        "typeof",
        "union",
        "unsigned",
        "void",
        "volatile",
        "while",
    ];
}
