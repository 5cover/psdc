using static CommandLine.ParserResultExtensions;
using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;
using Scover.Psdc.Tokenization;
using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc;

static class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CliOptions))] // Needed for CommandLineOptions with AOT
    static int Main(string[] args) => (int)new CommandLine.Parser(s => {
        s.HelpWriter = Console.Error;
        s.GetoptMode = true;
    }).ParseArguments<CliOptions>(args).MapResult(static opt => {
        if (!CodeGenerator.TryGet(opt.TargetLanguage, out var codeGenerator)) {
            WriteError($"unkown language: '{opt.TargetLanguage}'");
            return SysExits.Usage;
        }

        bool outputIsFile = opt.Output != "-";

        TextWriter output;
        try {
            output = outputIsFile
                ? new StreamWriter(opt.Output)
                : Console.Out;
        } catch (Exception e) when (e.IsFileSystemExogenous()) {
            WriteError($"coudln't open output file: {e.Message}");
            return SysExits.CantCreat;
        }

        try {
            string input;
            try {
                input = opt.Input == "-"
                    ? Console.In.ReadToEnd()
                    : File.ReadAllText(opt.Input);
            } catch (Exception e) when (e.IsFileSystemExogenous()) {
                WriteError($"couldn't read input: {e.Message}");
                return SysExits.NoInput;
            }

            PrintMessenger msger = new(Console.Error, input);

            var tokens = "Tokenizing".LogOperation(opt.Verbose,
                () => Tokenizer.Tokenize(msger, input).ToArray());

            var ast = "Parsing".LogOperation(opt.Verbose,
                () => Parser.Parse(msger, tokens));

            if (!ast.HasValue) {
                return SysExits.DataErr;
            }

            var sast = "Analyzing".LogOperation(opt.Verbose,
                () => StaticAnalyzer.Analyze(msger, ast.Value));

            string cCode = "Generating code".LogOperation(opt.Verbose,
                () => codeGenerator(msger, sast));

            output.Write(cCode);

            msger.PrintMessageList();

            return SysExits.Ok;
        } finally {
            if (outputIsFile) {
                output.Dispose();
            }
        }
    }, _ => SysExits.Usage);

    static void WriteError(string message)
     => Console.Error.WriteLine($"{Path.GetRelativePath(Environment.CurrentDirectory, Environment.ProcessPath ?? "psdc")}: error: {message}");
}
