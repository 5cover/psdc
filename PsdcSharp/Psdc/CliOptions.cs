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
    [Value(0, Required = true,
        HelpText = "Target language to compile to. More coming soon.",
        MetaValue = $"{Language.CliOption.C}")]
    public string TargetLanguage => targetLanguage;
    [Value(1, Required = true, Default = "-",
        HelpText = "Input file containing Pseudocode. When '-', read from standard input")]
    public string Input => input;
    [Option('o', "output", Default = "-",
        HelpText = "Output file. When '-' or unspecified, write to standard output")]
    public string Output => output;
    [Option('v', "verbose",
        HelpText = "Verbose output")]
    public bool Verbose => verbose;
    [Option("pedantic",
        HelpText = "Warn when using unofficial features. Guarantees correctness for tests.")]
    public bool Pedantic => pedantic;
    [Option("msg", Default = MessageStyle.Gnu,
        HelpText = "Style of the message list",
        MetaValue = "vscode/gnu")]
    public MessageStyle MsgStyle => msgStyle;
}
