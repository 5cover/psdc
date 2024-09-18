using CommandLine;

namespace Scover.Psdc;

sealed class CliOptions(
    string targetLanguage,
    string input,
    string output,
    bool verbose,
    bool pedantic,
    MessageStyle msgStyle
)
{
    public const string StdStreamPlaceholder = "-";
    [Value(0, Required = true,
        HelpText = "Target language to compile to. More coming soon.",
        MetaValue = $"{Language.CliOption.C}")]
    public string TargetLanguage => targetLanguage;
    [Value(1, Default = StdStreamPlaceholder,
        HelpText = $"Input file containing Pseudocode. When '{StdStreamPlaceholder}', read from standard input.")]
    public string Input => input;
    [Option('o', "output", Default = StdStreamPlaceholder,
        HelpText = $"Output file. When '{StdStreamPlaceholder}' or unspecified, write to standard output.")]
    public string Output => output;
    [Option('v', "verbose",
        HelpText = "Verbose output")]
    public bool Verbose => verbose;
    [Option("pedantic",
        HelpText = "Warn when using unofficial features. Guarantees correctness for tests.")]
    public bool Pedantic => pedantic;
    [Option("msg", Default = MessageStyle.Gnu,
        HelpText = "Style of the message list",
        MetaValue = "vscode/gnu/json")]
    public MessageStyle MsgStyle => msgStyle;
}
