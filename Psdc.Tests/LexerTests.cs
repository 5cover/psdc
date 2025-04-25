using System.Collections.Immutable;

using Scover.Psdc.Lexing;
using Scover.Psdc.Library;
using Scover.Psdc.Messages;

namespace Scover.Psdc.Tests;

readonly record struct EvaluatedMessage(FixedRange Location, MessageCode Code, string Content, ImmutableArray<string> AdvicePieces);

static class MessageExtensions
{
    public static EvaluatedMessage Evaluate(this Message msg, string input) => new(msg.Location, msg.Code, msg.Content.Get(input), msg.AdvicePieces.ToImmutableArray());
}

sealed class LexerTests
{
    [Test]
    [MethodDataSource<LexerTestsDataSources>(nameof(LexerTestsDataSources.TokenSequence))]
    public async Task TokenSequence(string input, IReadOnlyList<Token> tokens)
    {
        var t = await Lex(input, []);
        await Assert.That(t).HasCount().EqualTo(tokens.Count + 1);
        using var _ = Assert.Multiple();
        await AssertEofToken(input, t[^1]);
        await Assert.That(t[..^1]).IsEquivalentTo(tokens);
    }

    [Test]
    [MethodDataSource<LexerTestsDataSources>(nameof(LexerTestsDataSources.TokenSequenceWithMessages))]
    public async Task TokenSequenceWithMessages(string input, IReadOnlyList<Token> tokens, IEnumerable<EvaluatedMessage> messages)
    {
        var t = await Lex(input, messages);
        await Assert.That(t).HasCount().EqualTo(tokens.Count + 1);
        using var _ = Assert.Multiple();
        await AssertEofToken(input, t[^1]);
        await Assert.That(t[..^1]).IsEquivalentTo(tokens);
    }
    [Test]
    [MethodDataSource<LexerTestsDataSources>(nameof(LexerTestsDataSources.OneToken))]
    public async Task OneToken(TokenType type, string input)
    {
        var t = await Lex(input, []);
        await Assert.That(t).HasCount().EqualTo(2);
        using var _ = Assert.Multiple();
        await AssertEofToken(input, t[^1]);
        await Assert.That(t[0]).IsEqualTo(new(new(0, input.Length), type));
    }
    [Test]
    [MethodDataSource<LexerTestsDataSources>(nameof(LexerTestsDataSources.OneIdentifier))]
    public async Task OneIdentifier(string input)
    {
        var t = await Lex(input, []);
        await Assert.That(t).HasCount().EqualTo(2);
        using var _ = Assert.Multiple();
        await AssertEofToken(input, t[^1]);
        await Assert.That(t[0]).IsEqualTo(new(new(0, input.Length), TokenType.Ident, input));
    }

    [Test]
    [MethodDataSource<LexerTestsDataSources>(nameof(LexerTestsDataSources.OneLiteralString))]
    public async Task OneLiteralString(string value, string input)
    {
        var t = await Lex(input, []);
        await Assert.That(t).HasCount().EqualTo(2);
        using var _ = Assert.Multiple();
        await AssertEofToken(input, t[^1]);
        await Assert.That(t[0]).IsEqualTo(new(new(0, input.Length), TokenType.LiteralString, value));
    }

    [Test]
    [MethodDataSource<LexerTestsDataSources>(nameof(LexerTestsDataSources.OneLiteralChar))]
    public async Task OneLiteralChar(char value, string input)
    {
        var t = await Lex(input, []);
        await Assert.That(t).HasCount().EqualTo(2);
        using var _ = Assert.Multiple();
        await AssertEofToken(input, t[^1]);
        await Assert.That(t[0]).IsEqualTo(new(new(0, input.Length), TokenType.LiteralChar, value));
    }

    [Test]
    [MethodDataSource<LexerTestsDataSources>(nameof(LexerTestsDataSources.OneLiteralInt))]
    public async Task OneLiteralInt(long value, string input)
    {
        var t = await Lex(input, []);
        await Assert.That(t).HasCount().EqualTo(2);
        using var _ = Assert.Multiple();
        await AssertEofToken(input, t[^1]);
        await Assert.That(t[0]).IsEqualTo(new(new(0, input.Length), TokenType.LiteralInt, value));
    }

    [Test]
    [MethodDataSource<LexerTestsDataSources>(nameof(LexerTestsDataSources.OneLiteralReal))]
    public async Task OneLiteralReal(decimal value, string input)
    {
        var t = await Lex(input, []);
        await Assert.That(t).HasCount().EqualTo(2);
        using var _ = Assert.Multiple();
        await AssertEofToken(input, t[^1]);
        await Assert.That(t[0]).IsEqualTo(new(new(0, input.Length), TokenType.LiteralReal, value));
    }

    [Test]
    [MethodDataSource<LexerTestsDataSources>(nameof(LexerTestsDataSources.Empty))]
    public async Task Empty(string input)
    {
        var t = await Lex(input, []);
        await Assert.That(t).HasCount().EqualTo(1);
        using var _ = Assert.Multiple();
        await AssertEofToken(input, t[^1]);
    }

    static async Task<Token[]> Lex(string input, IEnumerable<EvaluatedMessage> messages)
    {
        TestMessenger msger = new();
        Lexer l = new(msger);
        var t = l.Lex(input).ToArray();
        await Assert.That(msger.Messages.Select(m => m.Evaluate(input))).IsEquivalentTo(messages);
        return t;
    }

    static async Task AssertEofToken(string input, Token token) => await Assert.That(token).IsEqualTo(new(new(input.Length, input.Length), TokenType.Eof));
}
