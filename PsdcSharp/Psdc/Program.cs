using static CommandLine.ParserResultExtensions;
using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;
using Scover.Psdc.Tokenization;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;

namespace Scover.Psdc;

static class Program
{
    const int ExitCompilationFailed = 1;

    static readonly TextWriter msgOutput = Console.Error;

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CliOptions))] // Needed for CommandLineOptions with AOT
    static int Main(string[] args) => new CommandLine.Parser(s => {
        s.HelpWriter = msgOutput;
        s.GetoptMode = true;
        s.CaseInsensitiveEnumValues = true;
    }).ParseArguments<CliOptions>(args).MapResult(static opt => {
        if (!CodeGenerator.TryGet(opt.TargetLanguage, out var codeGenerator)) {
            WriteError($"unknown language: '{opt.TargetLanguage}'");
            return SysExits.Usage;
        }

        bool outputIsRegFile = opt.Output != CliOptions.StdStreamPlaceholder;

        TextWriter output;
        try {
            output = outputIsRegFile
                ? new StreamWriter(opt.Output)
                : Console.Out;
        } catch (Exception e) when (e.IsFileSystemExogenous()) {
            WriteError($"coudln't open output file: {e.Message}");
            return SysExits.CantCreat;
        }

        try {
            string input;
            try {
                input = opt.Input == CliOptions.StdStreamPlaceholder
                    ? Console.In.ReadToEnd()
                    : File.ReadAllText(opt.Input);
            } catch (Exception e) when (e.IsFileSystemExogenous()) {
                WriteError($"couldn't read input: {e.Message}");
                return SysExits.NoInput;
            }

            PrintMessenger msger = new(
                msgOutput,
                opt.Input == CliOptions.StdStreamPlaceholder ? "<stdin>" : opt.Input,
                input,
                opt.MsgStyle,
                ImmutableDictionary.Create<MessageCode, bool>()
                    .Add(MessageCode.FeatureNotOfficial, opt.Pedantic));

            var tokens = "Tokenizing".LogOperation(opt.Verbose,
                () => Tokenizer.Tokenize(msger, input).ToArray());

            var ast = "Parsing".LogOperation(opt.Verbose,
                () => Parser.Parse(msger, tokens));

            if (ast.HasValue) {
                var sast = "Analyzing".LogOperation(opt.Verbose,
                    () => StaticAnalyzer.Analyze(msger, ast.Value));

                string cCode = "Generating code".LogOperation(opt.Verbose,
                    () => codeGenerator(msger, sast));

                output.Write(cCode);
            }

            msgOutput.WriteLine();

            msger.PrintMessageList();

            return Conclude(msger);
        } finally {
            if (outputIsRegFile) {
                output.Dispose();
            }
        }
    }, _ => SysExits.Usage);

    static void WriteError(string message)
     => msgOutput.WriteLine($"{Path.GetRelativePath(Environment.CurrentDirectory, Environment.ProcessPath ?? "psdc")}: error: {message}");

    static int Conclude(PrintMessenger msger)
    {
        msgOutput.Write("Compilation ");

        int exitCode;
        if (msger.GetMessageCount(MessageSeverity.Error) == 0) {
            exitCode = SysExits.Ok;
            new ConsoleColors(ConsoleColor.Green).DoInColor(() => msgOutput.Write("succeeded"));
        } else {
            exitCode = ExitCompilationFailed;
            new ConsoleColors(ConsoleColor.Red).DoInColor(() => msgOutput.Write("failed"));
        }

        msgOutput.WriteLine(string.Create(Format.Msg, $" ({Quantity(
        msger.GetMessageCount(MessageSeverity.Error), "error")}, {Quantity(
        msger.GetMessageCount(MessageSeverity.Warning), "warning")}, {Quantity(
        msger.GetMessageCount(MessageSeverity.Suggestion), "suggestion")})."));

        return exitCode;
    }

    static string Quantity(int amount, string singular, string plural)
     => string.Create(Format.Msg, $"{amount} {(amount == 1 ? singular : plural)}");

    static string Quantity(int amount, string singular)
     => Quantity(amount, singular, singular + "s");
}

