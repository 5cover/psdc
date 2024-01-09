using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Parsing;
using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc;

internal static class Program
{
    private static void Main()
    {
        /*string Input = File.ReadAllText("test.psc");/**/
        const string Input = """
        programme VotreAge3000 c'est
        début
            age : entier;
            écrireÉcran("Quel âge avez-vous ? ");
            lireClavier(age);

            écrireÉcran("Vous avez ", age, " ans.");

            si age >= 18 alors
                écrireÉcran("Vous êtes majeur");
            sinonsi age == 16 alors
                écrireÉcran("C'est l'heure de se faire recenser!");
            sinon
                écrireÉcran("T'es un bébé toi!");
            finsi
        fin
        """;/**/

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
            Position position = input.GetPositionAt(message.StartIndex);

            int startColumn = position.Column;
            int endColumn = input.GetPositionAt(message.EndIndex).Column;

            ReadOnlySpan<char> faultyLine = input.Line(position.Line);
            (ConsoleColor? foreground, ConsoleColor? background) msgColor = message.Type.GetConsoleColor();

            msgColor.DoInColor(() => Console.Error.Write($"[P{(int)message.Code:d3}]"));

            Console.Error.WriteLine($" {message.Type}: {message.Contents} (at {position})");

            // Part of line before error
            Console.Error.Write($"\t---> {faultyLine[..startColumn].TrimStart()}"); // trim to remove indentation

            msgColor.SetColor();
            Console.Error.Write($"{faultyLine[startColumn..endColumn]}");
            Console.ResetColor();

            // Part of line after error
            Console.Error.WriteLine($"{faultyLine[endColumn..]}");
        }
    }
}
