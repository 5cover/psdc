using Scover.Psdc.Lexing;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.Tests;

sealed class ParserTests
{
    [Test]
    [MethodDataSource(nameof(Parse))]
    public async Task Parse(string input, Node.Algorithm expectedAst, IReadOnlyList<EvaluatedMessage> messages)
    {
        TestMessenger msger = new();
        var t = new Lexer(msger).Lex(input).ToArray();
        var ast = new Parser(msger, t).Parse();
        using var _ = Assert.Multiple();
        await Assert.That(msger.Messages.Select(m => m.Evaluate(input))).IsEquivalentTo(messages);
        await Assert.That(ast).IsEqualTo(expectedAst);
    }

    public static IEnumerable<(string, Node.Algorithm, IReadOnlyList<EvaluatedMessage>)> Parse()
    {
        yield return ("", new(new(0, 0), []), []);
    }
}
