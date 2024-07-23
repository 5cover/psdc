using Scover.Psdc.Messages;

namespace Scover.Psdc.CodeGeneration.C;

sealed class KeywordTable : CodeGeneration.KeywordTable
{
    private KeywordTable() { }
    public static KeywordTable Instance { get; } = new();

    public string Validate(SourceTokens sourceTokens, string ident, Messenger msger)
    {
        if (keywords.Contains(ident)) {
            msger.Report(Message.ErrorTargetLanguageReservedKeyword(sourceTokens, ident, LanguageName.C));
        }
        return ident;
    }

    private static readonly HashSet<string> keywords = [
        "_Alignas", "_Alignof", "_Atomic", "_BitInt", "_Bool", "_Complex",
        "_Decimal128", "_Decimal32", "_Decimal64", "_Generic", "_Imaginary", "_Noreturn",
        "_Static_assert", "_Thread_local", "alignas", "alignof", "auto", "bool",
        "break", "case", "char", "const", "constexpr", "continue",
        "default", "do", "double", "else", "enum", "extern",
        "false", "float", "for", "goto", "if", "inline", "int",
        "long", "nullptr", "register", "restrict", "return", "short",
        "signed", "sizeof", "static_assert", "static", "struct", "switch",
        "thread_local", "true", "typedef", "typeof_unqual", "typeof", "union",
        "unsigned", "void", "volatile", "while",
    ];

}
