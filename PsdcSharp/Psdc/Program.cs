using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

static class Program
{
    const string StdinPlaceholder = "-";

    static int Main(string[] args)
    {
        const bool Debug = false;

        if (args.Length > 1) {
            WriteError("usage: INPUT_FILE");
            return SysExits.Usage;
        }

        string input;
        try {
            input = args.Length == 0 || args[0] == StdinPlaceholder
                ? Console.In.ReadToEnd()
                : File.ReadAllText(args[0]);
        } catch (Exception e) when (e.IsFileSystemExogenous()) {
            WriteError(e.Message);
            return SysExits.NoInput;
        }

        PrintMessenger messenger = new(Console.Error, input);

        var tokens = "Tokenizing".LogOperation(Debug,
            () => Tokenizer.Tokenize(messenger, input).ToArray());

        var ast = "Parsing".LogOperation(Debug,
            () => Parser.Parse(messenger, tokens));

        if (!ast.HasValue) {
            return SysExits.DataErr;
        }

        var sast = "Analyzing".LogOperation(Debug,
            () => StaticAnalyzer.Analyze(messenger, ast.Value));

        string cCode = "Generating code".LogOperation(Debug,
            () => CodeGenerator.GenerateC(messenger, sast));

        messenger.PrintConclusion();

        Console.Error.WriteLine("Generated C: ");
        Console.WriteLine(cCode);

        return SysExits.Ok;
    }

    static void WriteError(string message)
     => Console.Error.WriteLine($"usage: {Path.GetRelativePath(Environment.CurrentDirectory, Environment.ProcessPath ?? "psdc")}: {message}");
}
