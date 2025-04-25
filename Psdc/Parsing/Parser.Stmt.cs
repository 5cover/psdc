using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

partial class Parser
{
    static readonly IReadOnlyDictionary<TokenType, Parser<Node.Stmt>> dispatchStmt = new Dictionary<TokenType, Parser<Node.Stmt>>() {
        [TokenType.Begin] = Block,
    };

    static ParseResult<Node.Stmt> Stmt(ParsingContext ctx)
    {
        ParseResult<Node.Stmt> a = Block(ctx);
        var o = ParseOperation.Start(ctx.PushSubject("statement"));
        if (!o.Switch(dispatchStmt, out var stmt)) return o.Ko<Node.Stmt>();
        return o.Ok(stmt);
    }

    static ParseResult<Node.Stmt.Block> Block(ParsingContext ctx)
    {
        var o = ParseOperation.Start(ctx.PushSubject("block"));
        o.One(TokenType.Begin);
        o.ZeroOrMore(Stmt, TokenType.End, syncStmt, out var stmts);
        o.One(TokenType.End);
        return o.Ok<Node.Stmt.Block>(new (o.Extent, stmts));
    }
}
