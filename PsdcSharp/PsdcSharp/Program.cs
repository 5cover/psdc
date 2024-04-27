using System.Collections.Immutable;
using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Parsing;
using Scover.Psdc.StaticAnalysis;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 1) {
            throw new ArgumentException("Usage: <test_program_filename>");
        }

        string input = File.ReadAllText($"../../testPrograms/{args[0]}");

        PrintMessenger messenger = new(input);

        Tokenizer tokenizer = new(messenger, input);
        var tokens = "Tokenizing".LogOperation(() => tokenizer.Tokenize().ToImmutableArray());

        Parser parser = new(messenger, tokens);
        var ast = "Parsing".LogOperation(parser.Parse).Value;

        if (ast is null) {
            return;
        }

        StaticAnalyzer semanticAnalyzer = new(messenger, ast);
        var semanticAst = "Analyzing".LogOperation(semanticAnalyzer.AnalyzeSemantics);

        CodeGeneratorC codeGenerator = new(semanticAst);
        string cCode = "Generating code".LogOperation(codeGenerator.Generate);

        Console.Error.WriteLine("Generated C : ");
        Console.WriteLine(cCode);
    }

    private sealed class PrintMessenger(string input) : Messenger
    {
        private static TextWriter Stderr => Console.Error;

        public void Report(Message message)
        {
            var msgColor = message.Type.GetConsoleColor();
            msgColor.DoInColor(() => Stderr.Write($"[P{(int)message.Code:d4}] "));

            message.SourceCodeRange.Match(range => {
                Position start = input.GetPositionAt(range.Start);
                Position end = input.GetPositionAt(range.End);

                // If the error spans over multiple line, show only the last line.
                if (start.Line != end.Line) {
                    start = new(end.Line, 0);
                }

                Stderr.WriteLine($"{start}: {message.Type.ToString().ToLower()}: {message.Content(input)}");

                ReadOnlySpan<char> faultyLine = input.GetLine(start.Line);

                // Part of line before error
                Stderr.Write($"{GetErrorLinePrefix(start.Line)}{faultyLine[..start.Column]}");

                msgColor.SetColor();
                Stderr.Write($"{faultyLine[start.Column..end.Column]}");
                Console.ResetColor();

                // Part of line after error
                Stderr.WriteLine($"{faultyLine[end.Column..].TrimEnd()}");

                // Arrow below to indicate the precise location of the error even if colors aren't available
                Stderr.Write(GetErrorLinePrefix(new string(' ', (int)Math.Log10(start.Line) + 1)));
                var offset = Math.Max(faultyLine.GetLeadingWhitespaceCount(), start.Column);
                Stderr.WriteNTimes(offset, ' ');
                msgColor.DoInColor(() => Stderr.WriteNTimes(end.Column - offset, '^'));

                static string GetErrorLinePrefix(object lineNo) => $"    {lineNo} | ";
            },
            none: () => Stderr.WriteLine($"{message.Type}: {message.Content(input)}"));
            Stderr.WriteLine();
        }
    }
}
