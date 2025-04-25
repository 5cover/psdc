using System.Collections.Immutable;

using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

static partial class Parser
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

    static readonly IReadOnlyDictionary<TokenType, Parser<Node.Decl>> dispatchDecl = new Dictionary<TokenType, Parser<Node.Decl>>() {
        [TokenType.Hash] = CompilerDirective,
        [TokenType.Program] = MainProgram,
    };

    static ParseResult<Node.Decl> Decl(ParsingContext ctx)
    {
        var o = ParseOperation.Start(ctx.PushSubject("declaration"));
        if (!o.Switch(dispatchDecl, out var decl)) return o.Ko<Node.Decl>();
        return o.Ok(decl);
    }

    static ParseResult<Node.Decl> MainProgram(ParsingContext ctx)
    {
        var o = ParseOperation.Start(ctx.PushSubject("main program"));
        o.One(TokenType.Program);
        if (!o.One(TokenType.Ident, out var title)) return o.Ko<Node.Decl>();
        if (!o.One(Block, out var body)) return o.Ko<Node.Decl>();
        return o.Ok<Node.Decl>(new Node.Decl.MainProgram(o.Extent, new(title), body));
    }
}

