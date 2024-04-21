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
        var ast = "Parsing".LogOperation(parser.Parse);
        PrintMessages(parser, input);

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
        while (step.TryDequeueMessage(out Message? message)) {
            var msgColor = message.Type.GetConsoleColor();
            msgColor.DoInColor(() => Console.Error.Write($"[P{(int)message.Code:d4}] "));

            message.SourceCodeSpan.Match((startIndex, endIndex) => {
                Position startPos = input.GetPositionAt(startIndex);
                Position endPos = input.GetPositionAt(endIndex);

                // if the error spans over multiple lines, take the last line
                Position errorPos = startPos.Line == endPos.Line ? startPos : endPos;
                Console.Error.WriteLine($"{errorPos}: {message.Type.ToString().ToLower()}: {message.Content(input)}");

                ReadOnlySpan<char> faultyLine = input.GetLine(errorPos.Line);

                // Part of line before error
                Console.Error.Write($"\t---> {faultyLine[..errorPos.Column].TrimStart()}"); // trim to remove indentation

                msgColor.SetColor();
                Console.Error.Write($"{faultyLine[errorPos.Column..endPos.Column]}");
                Console.ResetColor();

                // Part of line after error
                Console.Error.WriteLine($"{faultyLine[endPos.Column..].TrimEnd()}\n");
            }, none: () => {
                Console.Error.WriteLine($"{message.Type}: {message.Content(input)}");
            });
        }
    }
}
