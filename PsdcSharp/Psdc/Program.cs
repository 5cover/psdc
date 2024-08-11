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
         => Console.Error.WriteLine($"psdc: {message}");

    sealed class PrintMessenger(string input) : Messenger
    {
        public string Input => input;

        readonly DefaultDictionary<MessageSeverity, int> _msgCountsBySeverity = new(0);
        static TextWriter Stderr => Console.Error;

        const string LineNoMargin = "    ";
        const string Bar = " | ";
        const int MaxMultilineErrorLines = 10;

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

            var lineNoPadding = end.Line.DigitCount();

            Stderr.WriteLine($"{start}: {message.Severity.ToString().ToLower()}: {message.Content.Get(input)}");

            // If the error spans over only 1 line, show it with carets underneath
            if (start.Line == end.Line) {
                ReadOnlySpan<char> badLine = input.GetLine(end.Line);

                // Bad line
                StartLine(lineNoPadding, end.Line);
                {
                    Stderr.Write(badLine[..start.Column]); // keep indentation
                    msgColor.SetColor();
                    Stderr.Write(badLine[start.Column..end.Column]);
                    Console.ResetColor();
                    EndLine(badLine[end.Column..]);
                }

                // Caret line
                StartLine(lineNoPadding);
                {
                    var offset = Math.Max(badLine.GetLeadingWhitespaceCount(), start.Column);
                    Stderr.WriteNTimes(offset, ' ');
                    msgColor.DoInColor(() => Stderr.WriteNTimes(end.Column - offset, '^'));
                    Stderr.WriteLine();
                }
            }
            // Otherwise, show all the bad code
            else {
                StartLine(lineNoPadding, start.Line);
                {
                    ReadOnlySpan<char> badLine = input.GetLine(start.Line);
                    Stderr.Write(badLine[..start.Column]);
                    msgColor.SetColor();
                    EndLine(badLine[start.Column..]);
                    Console.ResetColor();
                }

                var badLines = Enumerable.Range(start.Line + 1, end.Line - start.Line - 1);
                foreach (var line in badLines.Take(MaxMultilineErrorLines - 2)) {
                    ReadOnlySpan<char> badLine = input.GetLine(line);
                    StartLine(lineNoPadding, line);
                    msgColor.SetColor();
                    EndLine(badLine);
                    Console.ResetColor();
                };

                badLines.FirstOrNone().Tap(l => {
                    StartLine(lineNoPadding, l);
                    EndLine($"({end.Line - start.Line + 1 - MaxMultilineErrorLines} more lines...)");
                });

                StartLine(lineNoPadding, end.Line);
                {
                    ReadOnlySpan<char> badLine = input.GetLine(end.Line);
                    msgColor.SetColor();
                    Stderr.Write(badLine[..end.Column]);
                    Console.ResetColor();
                    EndLine(badLine[end.Column..]);
                }
            }

            // Advice lines
            foreach (var advice in message.AdvicePieces) {
                StartLine(lineNoPadding);
                Stderr.WriteLine(advice);
            };

            Stderr.WriteLine();
        }

        static void EndLine(ReadOnlySpan<char> content) => Stderr.WriteLine(content.TrimEnd());

        static void StartLine(int padding, int line)
        {
            Stderr.Write(LineNoMargin);
            Stderr.Write((line + 1).ToString().PadLeft(padding));
            Stderr.Write(Bar);
        }

        static void StartLine(int padding)
        {
            Stderr.Write(LineNoMargin);
            Stderr.WriteNTimes(padding, ' ');
            Stderr.Write(Bar);
        }
    }
}
