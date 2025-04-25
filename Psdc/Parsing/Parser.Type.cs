using Scover.Psdc.Lexing;

namespace Scover.Psdc.Parsing;

partial class Parser
{
    static readonly IReadOnlyDictionary<TokenType, Parser<Node.Type>> dispatchConsumeType = new Dictionary<TokenType, Parser<Node.Type>>() {
        [TokenType.Integer] = ctx => ParseResult.Ok<Node.Type>(ctx.Subject, 0, [], new Node.Type.Integer(new(ctx.Start - 1, ctx.Start)))
    };

    static ParseResult<Node.Type> Type(ParsingContext ctx)
    {
        var o = ParseOperation.Start(ctx.PushSubject("type"));
        if (!o.SwitchConsume(dispatchConsumeType, out var type)) return o.Ko<Node.Type>();
        return o.Ok(type);
    }

}
