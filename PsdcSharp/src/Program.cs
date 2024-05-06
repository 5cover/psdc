using System.Collections.Immutable;
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
        if (args.Length != 1) {
            WriteError("usage: <test_program_filename>");
            return SysExits.Usage;
        }

        const bool Debug = false;

        string input;
        try {
            input = args[0] == StdinPlaceholder
                ? Console.In.ReadToEnd()
                : File.ReadAllText(args[0]);
        } catch (Exception e) when (e.IsFileSystemExogenous()) {
            WriteError(e.Message);
            return SysExits.NoInput;
        }

        Globals.Initialize(input);

        PrintMessenger messenger = new();

        var tokens = "Tokenizing".LogOperation(Debug,
            () => Tokenizer.Tokenize(messenger, input).ToImmutableArray());

        var ast = "Parsing".LogOperation(Debug,
            () => Parser.Parse(messenger, tokens).Value);

        if (ast is null) {
            return SysExits.DataErr;
        }

        var semanticAst = "Analyzing".LogOperation(Debug,
            () => StaticAnalyzer.AnalyzeSemantics(messenger, ast));

        string cCode = "Generating code".LogOperation(Debug,
            () => CodeGenerator.GenerateC(messenger, semanticAst));

        Console.Error.WriteLine("Generated C : ");
        Console.WriteLine(cCode);

        return SysExits.Ok;
    }

    static void WriteError(string message)
         => Console.Error.WriteLine($"psdc: {message}");

    sealed class PrintMessenger : Messenger
    {
        readonly Dictionary<MessageSeverity, int> _msgCountsBySeverity = [];
        static TextWriter Stderr => Console.Error;

        public void PrintConclusion()
         => Stderr.WriteLine($"""
            Compilation terminated
             ({_msgCountsBySeverity[MessageSeverity.Error]} errors,
             {_msgCountsBySeverity[MessageSeverity.Warning]} warnings,
             {_msgCountsBySeverity[MessageSeverity.Suggestion]} suggestions).
            """);

        public void Report(Message message)
        {
            if (!_msgCountsBySeverity.TryAdd(message.Severity, 1)) {
                ++_msgCountsBySeverity[message.Severity];
            }
            var msgColor = message.Severity.GetConsoleColor();
            msgColor.DoInColor(() => Stderr.Write($"[P{(int)message.Code:d4}] "));

            message.InputRange.Match(range => {
                Position start = Globals.Input.GetPositionAt(range.Start);
                Position end = Globals.Input.GetPositionAt(range.End);

                // If the error spans over multiple line, show only the last line.
                if (start.Line != end.Line) {
                    start = new(end.Line, 0);
                }

                Stderr.WriteLine($"{start}: {message.Severity.ToString().ToLower()}: {message.Content}");

                ReadOnlySpan<char> faultyLine = Globals.Input.GetLine(start.Line);

                // Part of line before error
                Stderr.Write($"{GetErrorLinePrefix(start.Line + 1)}{faultyLine[..start.Column]}");

                msgColor.SetColor();
                Stderr.Write($"{faultyLine[start.Column..end.Column]}");
                Console.ResetColor();

                // Part of line after error
                Stderr.WriteLine($"{faultyLine[end.Column..].TrimEnd()}");

                // Arrow below to indicate the precise location of the error even if colors aren't available
                Stderr.Write(GetErrorLinePrefix(new string(' ', start.Line.DigitCount())));
                var offset = Math.Max(faultyLine.GetLeadingWhitespaceCount(), start.Column);
                Stderr.WriteNTimes(offset, ' ');
                msgColor.DoInColor(() => Stderr.WriteNTimes(end.Column - offset, '^'));

                static string GetErrorLinePrefix(object lineNo) => $"    {lineNo} | ";
            },
            none: () => Stderr.WriteLine($"{message.Severity}: {message.Content}"));
            Stderr.WriteLine();
        }
    }
}
