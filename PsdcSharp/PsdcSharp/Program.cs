using System.Collections.Immutable;
using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Parsing;
using Scover.Psdc.SemanticAnalysis;
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

        Tokenizer tokenizer = new(input);
        var tokens = "Tokenizing".LogOperation(() => tokenizer.Tokenize().ToImmutableArray());
        PrintMessages(tokenizer, input);

        Parser parser = new(tokens);
        var ast = "Parsing".LogOperation(parser.Parse).Value;
        PrintMessages(parser, input);

        if (ast is null) {
            return;
        }

        SemanticAnalyzer semanticAnalyzer = new(ast);
        var semanticAst = "Analyzing semantics".LogOperation(semanticAnalyzer.AnalyzeSemantics);
        PrintMessages(semanticAnalyzer, input);

        CodeGeneratorC codeGenerator = new(semanticAst);
        string cCode = "Generating code".LogOperation(codeGenerator.Generate);
        PrintMessages(codeGenerator, input);

        Console.Error.WriteLine("Generated C : ");
        Console.WriteLine(cCode);
    }

    private static void PrintMessages(MessageProvider step, string input)
    {
        var stderr = Console.Error;

        while (step.TryDequeueMessage(out Message? message)) {
            var msgColor = message.Type.GetConsoleColor();
            msgColor.DoInColor(() => stderr.Write($"[P{(int)message.Code:d4}] "));

            message.SourceCodeRange.Match(range => {
                Position start = input.GetPositionAt(range.Start);
                Position end = input.GetPositionAt(range.End);

                // If the error spans over multiple line, show only the last line.
                if (start.Line != end.Line) {
                    start = new(end.Line, 0);
                }

                stderr.WriteLine($"{start}: {message.Type.ToString().ToLower()}: {message.Content(input)}");

                ReadOnlySpan<char> faultyLine = input.GetLine(start.Line);

                // Part of line before error
                stderr.Write($"{GetErrorLinePrefix(start.Line)}{faultyLine[..start.Column]}");

                msgColor.SetColor();
                stderr.Write($"{faultyLine[start.Column..end.Column]}");
                Console.ResetColor();

                // Part of line after error
                stderr.WriteLine($"{faultyLine[end.Column..].TrimEnd()}");

                // Arrow below to indicate the precise location of the error even if colors aren't available
                stderr.Write(GetErrorLinePrefix(new string(' ', (int)Math.Log10(start.Line) + 1)));
                var offset = Math.Max(faultyLine.GetLeadingWhitespaceCount(), start.Column);
                stderr.WriteNTimes(offset, ' ');
                msgColor.DoInColor(() => stderr.WriteNTimes(end.Column - start.Column, '^'));

                static string GetErrorLinePrefix(object lineNo) => $"    {lineNo} | ";
            },
            none: () => stderr.WriteLine($"{message.Type}: {message.Content(input)}"));
            stderr.WriteLine();
        }
    }
}
