using static CommandLine.ParserResultExtensions;

using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;
using Scover.Psdc.Lexing;

using System.Diagnostics.CodeAnalysis;

namespace Scover.Psdc;

static class Program
{
    static readonly TextWriter msgOutput = Console.Error;

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CliOptions))] // Needed for CommandLineOptions with AOT
    static int Main(string[] args) => new CommandLine.Parser(s => {
        s.HelpWriter = msgOutput;
        s.GetoptMode = true;
        s.CaseInsensitiveEnumValues = true;
    }).ParseArguments<CliOptions>(args).MapResult(static opt => {
        using var output = OpenOutput(opt);
        if (output is null) {
            return SysExit.CantCreat;
        }
        var input = ReadInput(opt);
        return input is null ? SysExit.NoInput : Compile(output, input, opt);
    }, _ => SysExit.Usage);

    static void WriteError(string message) =>
        msgOutput.WriteLine($"{Path.GetRelativePath(Environment.CurrentDirectory, Environment.ProcessPath ?? "psdc")}: error: {message}");

    static int Compile(TextWriter output, string input, CliOptions opt)
    {
        if (!CodeGenerator.TryGet(opt.TargetLanguage, out var codeGenerator)) {
            WriteError($"unknown language: '{opt.TargetLanguage}'");
            return SysExit.Usage;
        }

        FilterMessenger msger = new(code => opt.Pedantic || code is not MessageCode.UnofficialFeature);

        var tokens = "Tokenizing".LogOperation(opt.Verbose,
            () => Lexer.Lex(msger, input).ToArray());

        var ast = "Parsing".LogOperation(opt.Verbose,
            () => Parser.Parse(msger, tokens));

        if (ast.HasValue) {
            var sast = "Analyzing".LogOperation(opt.Verbose,
                () => StaticAnalyzer.Analyze(msger, input, ast.Value));

            string cCode = "Generating code".LogOperation(opt.Verbose,
                () => codeGenerator(msger, sast));

            output.Write(cCode);
        }

        msgOutput.WriteLine();

        MessagePrinter msgPrinter = opt.MsgStyle switch {
            MessageStyle.Gnu => CreateMessagePrinter(MessageTextPrinter.Style.Gnu),
            MessageStyle.VSCode => CreateMessagePrinter(MessageTextPrinter.Style.VSCode),
            MessageStyle.Json => new MessageJsonPrinter(msgOutput, input),
            _ => throw opt.MsgStyle.ToUnmatchedException(),
        };

        msgPrinter.PrintMessageList(msger.Messages);
        msgPrinter.Conclude(msger.GetMessageCount);
        return msger.GetMessageCount(MessageSeverity.Error) != 0 ? AppExit.FailedWithErrors
            : msger.GetMessageCount(MessageSeverity.Warning) != 0 ? AppExit.FailedWithWarnings
            : msger.GetMessageCount(MessageSeverity.Hint) != 0 ? AppExit.FailedWithHints
            : SysExit.Ok;

        MessageTextPrinter CreateMessagePrinter(MessageTextPrinter.Style style) => new(
            msgOutput,
            opt.Input == CliOptions.StdStreamPlaceholder ? "<stdin>" : opt.Input,
            input,
            style);
    }

    static TextWriter? OpenOutput(CliOptions opt)
    {
        try {
            return opt.Output != CliOptions.StdStreamPlaceholder
                ? new StreamWriter(opt.Output)
                : Console.Out;
        } catch (Exception e) when (e.IsFileSystemExogenous()) {
            WriteError($"couldn't open output file: {e.Message}");
            return null;
        }
    }

    static string? ReadInput(CliOptions opt)
    {
        try {
            return opt.Input == CliOptions.StdStreamPlaceholder
                ? Console.In.ReadToEnd()
                : File.ReadAllText(opt.Input);
        } catch (Exception e) when (e.IsFileSystemExogenous()) {
            WriteError($"couldn't read input: {e.Message}");
            return null;
        }
    }
}
