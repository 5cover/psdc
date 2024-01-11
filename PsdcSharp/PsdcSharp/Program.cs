using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Parsing;
using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

internal static class Program
{
    private static void Main()
    {
        string Input = File.ReadAllText("../../testPrograms/loop.psc");

        Tokenizer tokenizer = new(Input);
        List<Token> tokens = "Tokenizing".LogOperation(() => tokenizer.Tokenize().ToList());
        PrintMessages(tokenizer, Input);

        Parser parser = new(tokens);
        ParseResult<Node.Algorithm> abstractSyntaxTree = "Parsing".LogOperation(parser.Parse);
        PrintMessages(parser, Input);

        CodeGenerator codeGenerator = new CodeGeneratorC(abstractSyntaxTree);
        string generatedC = "Generating code".LogOperation(codeGenerator.Generate);
        PrintMessages(codeGenerator, Input);

        Console.Error.WriteLine("Generated C : ");
        Console.WriteLine(generatedC);
    }

    private static void PrintMessages(CompilationStep step, string input)
    {
        while (step.TryDequeueMessage(out Message? message)) {
            Position startPos = input.GetPositionAt(message.StartIndex);
            Position endPos = input.GetPositionAt(message.EndIndex);

            Position pos = startPos.Line == endPos.Line ? startPos : endPos;

            // if the error spans over multiple lines, take the last line

            ReadOnlySpan<char> faultyLine = input.Line(pos.Line);
            var msgColor = message.Type.GetConsoleColor();

            msgColor.DoInColor(() => Console.Error.Write($"[P{(int)message.Code:d4}]"));

            Console.Error.WriteLine($" {message.Type}: {message.Content(input)} ({pos})");

            // Part of line before error
            Console.Error.Write($"\t---> {faultyLine[..pos.Column].TrimStart()}"); // trim to remove indentation

            msgColor.SetColor();
            Console.Error.Write($"{faultyLine[pos.Column..endPos.Column]}");
            Console.ResetColor();

            // Part of line after error
            Console.Error.WriteLine($"{faultyLine[endPos.Column..].TrimEnd()}");
        }
    }
}
