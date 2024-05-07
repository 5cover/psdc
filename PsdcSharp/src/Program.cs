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
        const bool Debug = false;

        if (args.Length > 1) {
            WriteError("usage: <test_program_filename>");
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

        PrintMessenger messenger = new(input);

        var tokens = "Tokenizing".LogOperation(Debug,
            () => Tokenizer.Tokenize(messenger, input).ToImmutableArray());

        var ast = "Parsing".LogOperation(Debug,
            () => Parser.Parse(messenger, tokens));

        if (!ast.HasValue) {
            return SysExits.DataErr;
        }

        var semanticAst = "Analyzing".LogOperation(Debug,
            () => StaticAnalyzer.Analyze(messenger, ast.Value));

        string cCode = "Generating code".LogOperation(Debug,
            () => CodeGenerator.GenerateC(messenger, semanticAst));

        messenger.PrintConclusion();

        Console.Error.WriteLine("Generated C : ");
        Console.WriteLine(cCode);

        return SysExits.Ok;
    }

    static void WriteError(string message)
         => Console.Error.WriteLine($"psdc: {message}");

    sealed class PrintMessenger(string input) : Messenger
    {
        public string Input => input;

        readonly DefaultDictionary<MessageSeverity, int> _msgCountsBySeverity = new(0);
        static TextWriter Stderr => Console.Error;

        public void PrintConclusion()
         => Stderr.WriteLine($"Compilation terminated ({_msgCountsBySeverity[MessageSeverity.Error]} errors, "
                           + $"{_msgCountsBySeverity[MessageSeverity.Warning]} warnings, "
                           + $"{_msgCountsBySeverity[MessageSeverity.Suggestion]} suggestions).");

        public void Report(Message message)
        {
            if (!_msgCountsBySeverity.TryAdd(message.Severity, 1)) {
                ++_msgCountsBySeverity[message.Severity];
            }
            var msgColor = message.Severity.GetConsoleColor();
            msgColor.DoInColor(() => Stderr.Write($"[P{(int)message.Code:d4}] "));

            Position start = input.GetPositionAt(message.InputRange.Start);
            Position end = input.GetPositionAt(message.InputRange.End);

            // If the error spans over multiple line, show only the last line.
            if (start.Line != end.Line) {
                start = new(end.Line, 0);
            }

            const string LineNoMargin = "    ";
            const string Bar = " | ";

            Stderr.WriteLine($"{start}: {message.Severity.ToString().ToLower()}: {message.Content.Get(input)}");

            ReadOnlySpan<char> faultyLine = input.GetLine(start.Line);

            // Faulty line
            WriteLineStart(withLineNumber: true);
            {
                Stderr.Write(faultyLine[..start.Column]); // keep indentation

                msgColor.SetColor();
                Stderr.Write($"{faultyLine[start.Column..end.Column]}");
                Console.ResetColor();

                Stderr.WriteLine($"{faultyLine[end.Column..].TrimEnd()}");
            }

            // Caret line
            WriteLineStart();
            {
                var offset = Math.Max(faultyLine.GetLeadingWhitespaceCount(), start.Column);
                Stderr.WriteNTimes(offset, ' ');
                msgColor.DoInColor(() => Stderr.WriteNTimes(end.Column - offset, '^'));
                Stderr.WriteLine();
            }

            // Advice line
            foreach (var advice in message.AdvicePieces) {
                WriteLineStart();
                Stderr.WriteLine(advice);
            };

            Stderr.WriteLine();

            void WriteLineStart(bool withLineNumber = false)
            {
                Stderr.Write(LineNoMargin);
                Stderr.Write(withLineNumber ? start.Line + 1 : new string(' ', start.Line.DigitCount()));
                Stderr.Write(Bar);
            }
        }
    }
}
