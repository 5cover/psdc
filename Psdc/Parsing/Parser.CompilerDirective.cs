using System.Diagnostics;
using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

partial class Parser
{
    static readonly IReadOnlyDictionary<TokenType, Parser<Node.CompilerDirective>> dispatchConsumeCompilerDirective = new Dictionary<TokenType, Parser<Node.CompilerDirective>>() {
        [TokenType.HashAssert] = ctx => {
            var o = ParseOperation.Start(ctx.PushSubject("#assert"));
            if (!o.One(Expr, out var expr)) return o.Ko<Node.CompilerDirective>();
            var msg = o.TryOne(Expr);
            return o.Ok<Node.CompilerDirective>(new Node.CompilerDirective.Assert(o.Extent, expr, msg));
        },
        [TokenType.HashEval] = ctx => {
            var o = ParseOperation.Start(ctx.PushSubject("#eval"));
            if (!o.One([TokenType.EvalExpr, TokenType.Type], out var next)) return o.Ko<Node.CompilerDirective>();
            switch (next.Type) {
            case TokenType.EvalExpr: {
                if (!o.One(Expr, out var expr)) return o.Ko<Node.CompilerDirective>();
                return o.Ok<Node.CompilerDirective>(new Node.CompilerDirective.EvalExpr(o.Extent.OfStart(o.Extent.Start - 1), expr));
            }
            case TokenType.Type: {
                if (!o.One(Type, out var type)) return o.Ko<Node.CompilerDirective>();
                return o.Ok<Node.CompilerDirective>(new Node.CompilerDirective.EvalType(o.Extent.OfStart(o.Extent.Start - 1), type));
            }
            default: throw new UnreachableException();
            }
        },
    };

    static ParseResult<Node.CompilerDirective> CompilerDirective(ParsingContext ctx)
    {
        var o = ParseOperation.Start(ctx.PushSubject("compiler detective"));
        if (!o.SwitchConsume(dispatchConsumeCompilerDirective, out var cd)) return o.Ko<Node.CompilerDirective>();
        return o.Ok(cd);
    }

}
