using System.Collections.Immutable;

using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

static class Parser
{
    // Token type -> whether to consume the token
    static readonly IReadOnlyDictionary<TokenType, bool> syncDecl = new Dictionary<TokenType, bool>() {
        [TokenType.Program] = false,
    };
    static readonly IReadOnlyDictionary<TokenType, bool> syncStmt = new Dictionary<TokenType, bool>() {
        [TokenType.Semi] = true,
    };

    public static ParseResult<Node.Algorithm> Parse(ImmutableArray<Token> tokens) => Algorithm(new(tokens, 0, []));

    static ParseResult<Node.Algorithm> Algorithm(ParsingContext ctx)
    {
        var o = ParseOperation.Start(ctx.PushSubject("algorithm"));
        o.ZeroOrMore(Decl, TokenType.Eof, syncDecl, out var decls);
        return o.Ok(new Node.Algorithm(decls));
    }

    static readonly IReadOnlyDictionary<TokenType, Parser<Node.Decl>> decl = new Dictionary<TokenType, Parser<Node.Decl>>() { };

    static ParseResult<Node.Decl> Decl(ParsingContext ctx)
    {
        var o = ParseOperation.Start(ctx.PushSubject("declaration"));
        if (o.Opt(TokenType.Hash)) {

        }
        if (o.Opt(TokenType.Program)) {

        }
        return o.Ko<Node.Decl>();
    }
}

