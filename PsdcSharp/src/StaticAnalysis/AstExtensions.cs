using System.Diagnostics;
using System.Globalization;

using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.StaticAnalysis;

static partial class AstExtensions
{
    /// <summary>
    /// Wrap an expression in a binary operation.
    /// </summary>
    /// <param name="expr">The left operand of the binary expression.</param>
    /// <param name="operator">The operator of the binary expression.</param>
    /// <param name="value">The right operand of the binary expression.</param>
    /// <returns>An binary operation expression node with empty source tokens, or a simple literal expression if the operation could be eagerly evaluated, and the messages that occured during the computatio.</returns>
    public static (Expression expression, IEnumerable<Message> messages) Alter(this Expression expr, BinaryOperator @operator, int value)
    {
        var opBin = expr.MakeBinaryOperation(@operator, value);
        // Collapse literals
        if (expr is Expression.Literal.Integer intLit) {
            var operation = @operator.EvaluateOperation(intLit.EvaluatedValue, new Value.Integer(value));
            return operation.Value.ToLiteral().Match<Expression.Literal, (Expression, IEnumerable<Message>)>(
                lit => (lit, operation.Errors.Select(e => e.GetOperationMessage(opBin,
                    intLit.EvaluatedValue.Type, EvaluatedType.Integer.Instance))),
                () => (opBin, []));
        }
        // This feels like cheating, creatig AST nodes with placeholder source tokens during code generation
        // But this is the only way without massive abstraction
        // This will be improved if it ever becomes a problem.
        return (opBin, []);
    }

    private static Expression.BinaryOperation MakeBinaryOperation(this Expression expr, BinaryOperator @operator, int value)
     => new(SourceTokens.Empty, expr, @operator,
            new Expression.Literal.Integer(SourceTokens.Empty, value.ToString(CultureInfo.InvariantCulture)));

    public static Option<Expression.Literal> ToLiteral(this Value value) => value switch {
        Value.Boolean b => b.Value.Map(b => b
         ? (Expression.Literal)new Expression.Literal.True(SourceTokens.Empty)
         : new Expression.Literal.False(SourceTokens.Empty)),
        Value.Integer i => i.Value.Map(i => new Expression.Literal.Integer(SourceTokens.Empty, i)),
        Value.Real r => r.Value.Map(r => new Expression.Literal.Real(SourceTokens.Empty, r)),
        Value.String s => s.Value.Map(s => new Expression.Literal.String(SourceTokens.Empty, s)),
        Value.Unknown or Value.NonConst _ => Option.None<Expression.Literal>(),
        _ => throw value.ToUnmatchedException(),
    };

}
